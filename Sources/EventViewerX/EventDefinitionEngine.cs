using System.Reflection;
using System.Runtime.CompilerServices;

namespace EventViewerX;

/// <summary>Executes validated declarative definitions through the shared native batch engine.</summary>
public static class EventDefinitionEngine {
    private static readonly Dictionary<string, PropertyInfo> MetadataProperties = typeof(EventObject)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
        .ToDictionary(static property => property.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Streams custom projections.</summary>
    public static IAsyncEnumerable<CustomEventRecord> ReadAsync(EventDefinitionQuery query,
        CancellationToken cancellationToken = default) {
        return ReadSnapshotAsync(CreateSnapshot(query), null, cancellationToken);
    }

    /// <summary>Streams custom projections and reports progress and isolated remote failures.</summary>
    public static IAsyncEnumerable<CustomEventRecord> ReadAsync(EventDefinitionQuery query,
        EventDefinitionQueryExecutionInfo executionInfo,
        CancellationToken cancellationToken = default) {
        if (executionInfo == null) {
            throw new ArgumentNullException(nameof(executionInfo));
        }
        return ReadSnapshotAsync(CreateSnapshot(query), executionInfo, cancellationToken);
    }

    private static async IAsyncEnumerable<CustomEventRecord> ReadSnapshotAsync(EventDefinitionQuery query,
        EventDefinitionQueryExecutionInfo? executionInfo,
        [EnumeratorCancellation] CancellationToken cancellationToken) {
        EventDefinitionQueryExecutionInfo info = executionInfo ?? new EventDefinitionQueryExecutionInfo();
        info.Reset();
        (DateTime? start, DateTime? end) = EventTimeRange.Resolve(query.StartTime, query.EndTime, query.TimePeriod);
        EventLogBatchQuery batch = query.Paths != null && query.Paths.Count > 0
            ? CreateFileBatch(query, start, end)
            : CreateChannelBatch(query, start, end);
        batch.MaxEvents = 0;
        batch.MaxConcurrency = query.MaxConcurrency;
        batch.ContinueOnError = query.ContinueOnRemoteFailure;
        batch.FailureHandler = failure => HandleFailure(failure, info);
        long emitted = 0;
        await foreach (EventObject source in EventLogEngine.ReadBatchAsync(batch, cancellationToken)) {
            if (query.MaxCandidates > 0 && info.EventsScanned >= query.MaxCandidates) {
                info.ScanLimitReached = true;
                yield break;
            }
            info.EventsScanned++;
            var record = new CustomEventRecord(query.Definition, source, Project(query.Definition, source));
            query.CandidateObserver?.Invoke(source);
            if (query.ResultPredicate != null && !query.ResultPredicate(record)) {
                continue;
            }
            emitted++;
            info.EventsEmitted = emitted;
            yield return record;
            if (query.MaxEvents > 0 && emitted >= query.MaxEvents) {
                yield break;
            }
        }
    }

    private static EventLogBatchQuery CreateChannelBatch(
        EventDefinitionQuery query,
        DateTime? start,
        DateTime? end) {

        string?[] targets = NormalizeTargets(query.MachineNames);
        var sources = new List<EventLogChannelQuery>();
        foreach (string? target in targets) {
            foreach (EventDefinitionSource source in query.Definition.Sources) {
                foreach ((string xpath, string logName) in CreateSourceFilters(
                             query,
                             source,
                             target,
                             sourceIsFile: false,
                             start,
                             end,
                             useOriginalChannel: !string.IsNullOrWhiteSpace(query.CollectorLogName))) {
                    sources.Add(new EventLogChannelQuery(logName) {
                        MachineName = target,
                        Credential = EventLogTarget.IsLocalMachine(target) ? null : query.Credential,
                        Authentication = query.Authentication,
                        XPath = xpath,
                        Oldest = query.Oldest,
                        ReadMode = query.ReadMode,
                        IncludeBookmark = query.IncludeBookmark,
                        MessageCulture = query.MessageCulture,
                        FallbackMessageCulture = query.FallbackMessageCulture,
                        RemoteConnectionTimeoutMilliseconds = query.RemoteConnectionTimeoutMilliseconds,
                        RemoteReadTimeoutMilliseconds = query.RemoteReadTimeoutMilliseconds,
                        BufferCapacity = query.BufferCapacity
                    });
                }
            }
        }
        return EventLogBatchConsolidator.Consolidate(EventLogBatchQuery.ForChannels(sources));
    }

    private static EventLogBatchQuery CreateFileBatch(
        EventDefinitionQuery query,
        DateTime? start,
        DateTime? end) {

        var files = new List<EventLogFileQuery>();
        foreach (string path in query.Paths!) {
            string fullPath = Path.GetFullPath(path);
            foreach (EventDefinitionSource source in query.Definition.Sources) {
                foreach ((string xpath, _) in CreateSourceFilters(
                             query,
                             source,
                             fullPath,
                             sourceIsFile: true,
                             start,
                             end,
                             useOriginalChannel: true)) {
                    files.Add(new EventLogFileQuery(fullPath) {
                        XPath = xpath,
                        Oldest = query.Oldest,
                        ReadMode = query.ReadMode,
                        IncludeBookmark = query.IncludeBookmark,
                        MessageCulture = query.MessageCulture,
                        FallbackMessageCulture = query.FallbackMessageCulture
                    });
                }
            }
        }
        return EventLogBatchConsolidator.Consolidate(EventLogBatchQuery.ForFiles(files));
    }

    private static IEnumerable<(string XPath, string LogName)> CreateSourceFilters(
        EventDefinitionQuery query,
        EventDefinitionSource source,
        string? machineName,
        bool sourceIsFile,
        DateTime? start,
        DateTime? end,
        bool useOriginalChannel) {

        var filter = new EventFilter {
            EventIds = source.EventIds.ToArray(),
            ProviderNames = source.ProviderNames.ToArray(),
            RecordIds = query.RecordIds?.ToArray(),
            StartTime = start,
            EndTime = end,
            MinimumRecordIdExclusive = query.MinimumRecordIdExclusiveResolver?.Invoke(
                machineName,
                sourceIsFile
                    ? machineName!
                    : string.IsNullOrWhiteSpace(query.CollectorLogName) ? source.LogName : query.CollectorLogName!)
        };
        foreach (EventFilter partition in EventFilterPartitioner.Partition(filter)) {
            string xpath = EventFilterCompiler.BuildXPath(partition);
            string logName = source.LogName;
            if (useOriginalChannel) {
                xpath = EventTypeEngine.AddOriginalChannelPredicate(xpath, source.LogName);
                if (!string.IsNullOrWhiteSpace(query.CollectorLogName)) {
                    logName = query.CollectorLogName!;
                }
            }
            yield return (xpath, logName);
        }
    }

    /// <summary>Projects one previously read event through a custom definition.</summary>
    public static CustomEventRecord CreateRecord(EventDefinition definition, EventObject source) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }
        definition.Validate();
        return new CustomEventRecord(definition, source, Project(definition, source));
    }

    internal static IReadOnlyDictionary<string, object?> Project(EventDefinition definition, EventObject source) {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (EventDefinitionField field in definition.Fields) {
            object? value = field.Source switch {
                EventFieldSource.Data => source.Data.TryGetValue(field.SourceName, out string? data) ? data : field.DefaultValue,
                EventFieldSource.MessageData => source.MessageData.TryGetValue(field.SourceName, out string? messageData) ? messageData : field.DefaultValue,
                EventFieldSource.Metadata => MetadataProperties.TryGetValue(field.SourceName, out PropertyInfo? property) ? property.GetValue(source) : field.DefaultValue,
                EventFieldSource.Message => source.Message,
                EventFieldSource.Constant => field.SourceName,
                _ => field.DefaultValue
            };
            result[field.Name] = value;
        }
        return result;
    }

    internal static EventDefinitionQuery CreateSnapshot(EventDefinitionQuery query) {
        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }
        query.Definition.Validate();
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Maximum events must be non-negative.");
        }
        if (query.MaxCandidates < 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Maximum candidates must be non-negative.");
        }
        if (query.MaxConcurrency < 1 || query.MaxConcurrency > EventLogLimits.MaximumConcurrency) {
            throw new ArgumentOutOfRangeException(nameof(query), $"Maximum concurrency must be between 1 and {EventLogLimits.MaximumConcurrency}.");
        }
        if (query.RemoteConnectionTimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Remote connection timeout must be positive.");
        }
        if (query.RemoteReadTimeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(query), "Remote read timeout must be non-negative.");
        }
        if (query.BufferCapacity is < 1 or > 4096) {
            throw new ArgumentOutOfRangeException(nameof(query), "Buffer capacity must be between 1 and 4096.");
        }
        if (!Enum.IsDefined(typeof(EventLogAuthentication), query.Authentication)) {
            throw new ArgumentOutOfRangeException(nameof(query), "The remote authentication value is not supported.");
        }
        string?[] targets = NormalizeTargets(query.MachineNames);
        bool hasPaths = query.Paths != null && query.Paths.Count > 0;
        if (hasPaths && query.Paths!.Any(static path => string.IsNullOrWhiteSpace(path))) {
            throw new ArgumentException("Offline paths cannot contain empty values.", nameof(query));
        }
        if (hasPaths &&
            (query.MachineNames != null && query.MachineNames.Count > 0 ||
             !string.IsNullOrWhiteSpace(query.CollectorLogName) ||
             query.Credential != null)) {
            throw new ArgumentException(
                "Offline paths cannot be combined with remote targets, collectors, or credentials.",
                nameof(query));
        }
        if (query.Credential != null && targets.Any(EventLogTarget.IsLocalMachine)) {
            throw new ArgumentException("Credential can only be used when every definition target is remote.", nameof(query));
        }
        EventDefinition definition = CopyDefinition(query.Definition);
        return new EventDefinitionQuery(definition) {
            Paths = query.Paths?.ToArray(),
            MachineNames = targets,
            CollectorLogName = string.IsNullOrWhiteSpace(query.CollectorLogName) ? null : query.CollectorLogName!.Trim(),
            StartTime = query.StartTime,
            EndTime = query.EndTime,
            TimePeriod = query.TimePeriod,
            RecordIds = query.RecordIds?.ToArray(),
            MaxEvents = query.MaxEvents,
            MaxCandidates = query.MaxCandidates,
            MaxConcurrency = query.MaxConcurrency,
            Oldest = query.Oldest,
            ReadMode = query.ReadMode,
            IncludeBookmark = query.IncludeBookmark,
            Credential = query.Credential,
            Authentication = query.Authentication,
            RemoteConnectionTimeoutMilliseconds = query.RemoteConnectionTimeoutMilliseconds,
            RemoteReadTimeoutMilliseconds = query.RemoteReadTimeoutMilliseconds,
            BufferCapacity = query.BufferCapacity,
            MessageCulture = query.MessageCulture,
            FallbackMessageCulture = query.FallbackMessageCulture,
            ResultPredicate = query.ResultPredicate,
            MinimumRecordIdExclusiveResolver = query.MinimumRecordIdExclusiveResolver,
            CandidateObserver = query.CandidateObserver,
            ContinueOnRemoteFailure = query.ContinueOnRemoteFailure
        };
    }

    private static EventDefinition CopyDefinition(EventDefinition definition) => new() {
        Name = definition.Name.Trim(),
        DisplayName = definition.DisplayName?.Trim() ?? string.Empty,
        Description = definition.Description?.Trim() ?? string.Empty,
        Category = definition.Category?.Trim() ?? string.Empty,
        Sources = definition.Sources.Select(static source => new EventDefinitionSource {
            LogName = source.LogName.Trim(),
            EventIds = source.EventIds.Distinct().OrderBy(static id => id).ToArray(),
            ProviderNames = source.ProviderNames.Select(static provider => provider.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static provider => provider, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        }).ToArray(),
        Fields = definition.Fields.Select(static field => new EventDefinitionField {
            Name = field.Name.Trim(),
            Source = field.Source,
            SourceName = field.SourceName?.Trim() ?? string.Empty,
            DefaultValue = field.DefaultValue
        }).ToArray()
    };

    private static string?[] NormalizeTargets(IReadOnlyList<string?>? machineNames) {
        IEnumerable<string?> candidates = machineNames == null || machineNames.Count == 0
            ? new string?[] { null }
            : machineNames;
        return candidates.Select(static machine => EventLogTarget.IsLocalMachine(machine) ? null : machine?.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void HandleFailure(EventLogQueryFailure failure, EventDefinitionQueryExecutionInfo executionInfo) {
        if (EventLogRemoteQueryFailureClassifier.TryClassify(failure.MachineName, failure.Exception,
                out EventLogRemoteQueryFailureKind kind)) {
            executionInfo.RecordTargetFailure(new EventLogQueryTargetFailure(
                failure.MachineName!, failure.Source, kind, failure.Exception.Message));
            return;
        }
        throw failure.Exception;
    }
}
