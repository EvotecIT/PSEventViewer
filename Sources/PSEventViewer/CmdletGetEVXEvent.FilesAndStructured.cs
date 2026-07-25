using System.Collections;
using System.Net;

namespace PSEventViewer;

public sealed partial class CmdletGetEVXEvent {
    private readonly Dictionary<string, string[]>
        _offlineProvidersByPath =
            new(StringComparer.OrdinalIgnoreCase);

    private EventLogBatchQuery CreateFileBatch(
        IReadOnlyList<string> paths,
        EventFilter filter,
        EventFilter? suppress,
        string? rawXPath,
        bool allowManagedProviderFilter = true,
        bool allowRemoteBatchContext = false) {

        if (!allowRemoteBatchContext &&
            (Credential != null ||
            (MachineName != null && MachineName.Any(
                static machine => !string.IsNullOrWhiteSpace(machine))))) {
            throw new PSArgumentException(
                "Offline event log files are read locally and cannot be combined with MachineName or Credential.");
        }
        ValidateRawXPathCombination(rawXPath, filter);
        EventFilterCompiler
            .SplitNamedDataExclusions(
                filter,
                out EventFilter? selectFilter,
                out EventFilter? namedDataSuppression);
        filter = selectFilter!;
        IReadOnlyList<EventFilter>
            namedDataSuppressions =
                PartitionSuppressions(
                    namedDataSuppression);
        bool expandSelectProviders =
            ContainsWildcard(filter.ProviderNames);
        if (expandSelectProviders &&
            allowManagedProviderFilter) {
            string[] patterns = NormalizeRequiredValues(
                filter.ProviderNames!,
                nameof(ProviderName));
            _managedProviderPatterns = patterns
                .Select(pattern => new WildcardPattern(
                    pattern,
                    WildcardOptions.IgnoreCase |
                    WildcardOptions.CultureInvariant))
                .ToArray();
            filter = WithProviders(
                filter,
                Array.Empty<string>());
            expandSelectProviders = false;
        }
        bool expandSuppressProviders =
            suppress != null &&
            ContainsWildcard(suppress.ProviderNames);
        IReadOnlyList<EventFilter> suppressions =
            expandSuppressProviders
                ? Array.Empty<EventFilter>()
                : PartitionSuppressions(suppress);
        IReadOnlyList<EventFilter> filterChunks =
            expandSelectProviders
                ? Array.Empty<EventFilter>()
                : EventFilterPartitioner.Partition(filter);
        if (expandSelectProviders ||
            expandSuppressProviders ||
            namedDataSuppressions.Count > 0 ||
            suppressions.Count > 0 ||
            filterChunks.Count > 1) {
            var structured = new List<EventLogStructuredQuery>(
                checked(
                    paths.Count *
                    Math.Max(filterChunks.Count, 1)));
            foreach (string path in paths) {
                EventFilter pathFilter =
                    expandSelectProviders
                        ? ExpandOfflineProviderPatterns(
                            filter,
                            path)
                        : filter;
                IReadOnlyList<EventFilter> pathSuppressions =
                    expandSuppressProviders
                        ? PartitionSuppressions(
                            ExpandOfflineProviderPatterns(
                                suppress!,
                                path))
                        : suppressions;
                if (namedDataSuppressions.Count > 0) {
                    pathSuppressions = pathSuppressions
                        .Concat(namedDataSuppressions)
                        .ToArray();
                }
                EventFilter sourceFilter = CopyFilterWithCheckpoint(
                    pathFilter,
                    machineName: null,
                    path,
                    sourceIsFile: true);
                foreach (EventFilter chunk in
                         EventFilterPartitioner.Partition(
                             sourceFilter)) {
                    string queryXml =
                        EventFilterCompiler.BuildFileQueryXmlWithSuppressions(
                        new[] { path },
                        chunk,
                        pathSuppressions);
                    structured.Add(CreateStructuredQuery(
                        queryXml,
                        EventLogQuerySourceKind.File,
                        machineName: null));
                }
            }
            EventLogBatchQuery structuredBatch =
                EventLogBatchQuery.ForStructured(structured);
            structuredBatch =
                ConsolidateAndValidateBookmarkFanOut(
                    structuredBatch);
            ConfigureBatch(structuredBatch);
            return structuredBatch;
        }

        var files = new List<EventLogFileQuery>(paths.Count);
        foreach (string path in paths) {
            EventFilter sourceFilter = CopyFilterWithCheckpoint(
                filter,
                machineName: null,
                path,
                sourceIsFile: true);
            string xpath = string.IsNullOrWhiteSpace(rawXPath)
                ? EventFilterCompiler.BuildXPath(sourceFilter)
                : rawXPath!;
            files.Add(CreateFileQuery(path, xpath));
        }
        ValidateBookmarkFanOut(files.Count);
        EventLogBatchQuery batch = EventLogBatchQuery.ForFiles(files);
        ConfigureBatch(batch);
        return batch;
    }

    private EventFilter ExpandOfflineProviderPatterns(
            EventFilter filter,
            string path) {

        string[] patterns = NormalizeRequiredValues(
            filter.ProviderNames ??
            Array.Empty<string>(),
            nameof(ProviderName));
        WildcardPattern[] wildcards = patterns
            .Select(pattern => new WildcardPattern(
                pattern,
                WildcardOptions.IgnoreCase |
                WildcardOptions.CultureInvariant))
            .ToArray();
        string[] providers = GetOfflineProviders(path)
            .Where(provider =>
                wildcards.Any(pattern =>
                    pattern.IsMatch(provider)))
            .ToArray();
        return WithProviders(
            filter,
            providers.Length > 0
                ? providers
                : new[] {
                    "__EventViewerX_No_Matching_Provider__"
                });
    }

    private string[] GetOfflineProviders(string path) {
        string fullPath =
            System.IO.Path.GetFullPath(path);
        if (_offlineProvidersByPath.TryGetValue(
                fullPath,
                out string[]? providers)) {
            return providers;
        }
        providers = EventLogEngine.ReadFile(
                new EventLogFileQuery(fullPath) {
                    Oldest = true,
                    ReadMode = EventReadMode.Metadata
                })
            .Select(static eventObject =>
                eventObject.ProviderName)
            .Where(static provider =>
                !string.IsNullOrWhiteSpace(provider))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                static provider => provider,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _offlineProvidersByPath.Add(
            fullPath,
            providers);
        return providers;
    }

    private static IReadOnlyList<EventFilter> PartitionSuppressions(
        EventFilter? filter) {

        return filter?.HasAny == true
            ? EventFilterPartitioner.Partition(filter)
            : Array.Empty<EventFilter>();
    }

    private EventLogBatchQuery CreateStructuredBatch(
        string queryXml,
        EventLogQuerySourceKind sourceKind) {

        if (string.IsNullOrWhiteSpace(queryXml)) {
            throw new PSArgumentException(
                "FilterXml cannot be null, empty, or whitespace.");
        }
        var sourceProbe =
            new EventLogStructuredQuery(queryXml) {
                SourceKind = sourceKind
            };
        bool usesFiles = sourceProbe.ResolveSourceKinds()
            .Contains(EventLogQuerySourceKind.File);
        if (usesFiles &&
            (Credential != null ||
             (MachineName != null &&
              MachineName.Any(static machine =>
                  !string.IsNullOrWhiteSpace(machine))))) {
            throw new PSArgumentException(
                "A file-based FilterXml is evaluated locally and cannot be combined with MachineName or Credential.");
        }
        IReadOnlyList<string?> machines =
            usesFiles
                ? new string?[] { null }
                : EventLogTarget.NormalizeMachineNames(MachineName);
        ValidateRemoteCredentialTargets(machines);
        var structured = new List<EventLogStructuredQuery>(machines.Count);
        foreach (string? machine in machines) {
            structured.Add(CreateStructuredQuery(
                queryXml,
                sourceKind,
                machine));
        }
        int independentSourceCount = checked(
            structured.Count *
            sourceProbe.GetIndependentSourceCount());
        ValidateBookmarkFanOut(independentSourceCount);
        EventLogBatchQuery batch =
            EventLogBatchQuery.ForStructured(structured);
        ConfigureBatch(batch);
        return batch;
    }

    private EventLogChannelQuery CreateChannelQuery(
        string logName,
        string? machineName,
        string xpath) {

        var query = new EventLogChannelQuery(logName) {
            MachineName = machineName,
            Credential = Credential?.GetNetworkCredential(),
            Authentication = Authentication,
            XPath = xpath,
            Oldest = EffectiveOldest,
            ReadMode = ReadMode,
            MessageCulture = MessageCulture,
            FallbackMessageCulture = FallbackMessageCulture,
            MaxEvents = GetNativeCandidateLimit(),
            IncludeBookmark = IncludeBookmark,
            RemoteReadTimeoutMilliseconds =
                EffectiveRemoteReadTimeoutMilliseconds,
            BufferCapacity = BufferCapacity > 0 ? BufferCapacity : 64,
            BookmarkXml = BookmarkXml,
            BookmarkOffset = BookmarkOffset,
            StrictBookmark = !IgnoreStaleBookmark
        };
        query.RemoteConnectionTimeoutMilliseconds =
            EffectiveRemoteConnectionTimeoutMilliseconds;
        return query;
    }

    private EventLogFileQuery CreateFileQuery(
        string path,
        string xpath) {

        return new EventLogFileQuery(path) {
            XPath = xpath,
            Oldest = EffectiveOldest,
            ReadMode = ReadMode,
            MessageCulture = MessageCulture,
            FallbackMessageCulture = FallbackMessageCulture,
            MaxEvents = GetNativeCandidateLimit(),
            IncludeBookmark = IncludeBookmark,
            BookmarkXml = BookmarkXml,
            BookmarkOffset = BookmarkOffset,
            StrictBookmark = !IgnoreStaleBookmark
        };
    }

    private EventLogStructuredQuery CreateStructuredQuery(
        string queryXml,
        EventLogQuerySourceKind sourceKind,
        string? machineName) {

        bool fileSource =
            sourceKind == EventLogQuerySourceKind.File;
        var query = new EventLogStructuredQuery(queryXml) {
            SourceKind = sourceKind,
            MachineName = machineName,
            Credential = fileSource
                ? null
                : Credential?.GetNetworkCredential(),
            Authentication = fileSource
                ? EventLogAuthentication.Default
                : Authentication,
            Oldest = EffectiveOldest,
            ReadMode = ReadMode,
            MessageCulture = MessageCulture,
            FallbackMessageCulture = FallbackMessageCulture,
            MaxEvents = GetNativeCandidateLimit(),
            IncludeBookmark = IncludeBookmark,
            RemoteReadTimeoutMilliseconds =
                EffectiveRemoteReadTimeoutMilliseconds,
            BufferCapacity = BufferCapacity > 0 ? BufferCapacity : 64,
            BookmarkXml = BookmarkXml,
            BookmarkOffset = BookmarkOffset,
            StrictBookmark = !IgnoreStaleBookmark,
            TolerateQueryErrors = TolerateQueryErrors,
            FailureHandler = failure => WriteError(new ErrorRecord(
                failure.Exception,
                "EVXStructuredQueryPathFailed",
                ErrorCategory.ReadError,
                string.IsNullOrWhiteSpace(failure.MachineName)
                    ? failure.Source
                    : $"{failure.Source} on {failure.MachineName}"))
        };
        query.RemoteConnectionTimeoutMilliseconds =
            EffectiveRemoteConnectionTimeoutMilliseconds;
        return query;
    }

}
