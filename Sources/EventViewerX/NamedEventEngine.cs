using System.Globalization;
using System.Runtime.CompilerServices;

namespace EventViewerX;

/// <summary>
/// Projects registered named-event rules over the shared native query and batch engines.
/// </summary>
public static partial class NamedEventEngine {
    /// <summary>Streams named-event projections with bounded memory and ordered checkpoint observation.</summary>
    public static async IAsyncEnumerable<EventObjectSlim> ReadAsync(
        NamedEventQuery query,
        NamedEventsQueryExecutionInfo? executionInfo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {

        Validate(query);
        Dictionary<string, HashSet<int>> eventInfo =
            RestrictSources(
                EventObjectSlim.GetEventInfoForNamedEvents(
                    query.NamedEvents.ToList()),
                query.SourceLogName,
                query.SourceEventIds);
        if (eventInfo.Count == 0) {
            yield break;
        }

        NamedEventsQueryExecutionInfo info =
            executionInfo ??
            new NamedEventsQueryExecutionInfo();
        info.Reset(query.MaxCandidates);
        using var enricher = query.Enrichment == null
            ? null
            : new NamedEventEnricher(
                query.Enrichment);
        EventLogBatchQuery batch =
            CreateBatch(
                query,
                eventInfo,
                info);
        long emitted = 0;

        await foreach (NamedEventProjection projection in
                       ProjectCandidatesInOrderAsync(
                           EventLogEngine.ReadBatchAsync(
                               batch,
                               cancellationToken),
                           query.NamedEvents,
                           enricher,
                           info.TryRecordCandidate,
                           query.CandidateObserver,
                           cancellationToken)) {
            EventObjectSlim? target = projection.Target;
            if (target == null ||
                (query.ResultPredicate != null &&
                 !query.ResultPredicate(target))) {
                continue;
            }

            emitted++;
            info.EventsEmitted = emitted;
            yield return target;
            if (query.MaxEvents > 0 &&
                emitted >= query.MaxEvents) {
                yield break;
            }
        }
    }

    internal static Dictionary<string, HashSet<int>>
        RestrictSources(
            IReadOnlyDictionary<string, HashSet<int>> eventInfo,
            string? sourceLogName,
            IReadOnlyCollection<int>? sourceEventIds) {

        if (eventInfo == null) {
            throw new ArgumentNullException(
                nameof(eventInfo));
        }
        if (sourceEventIds != null &&
            sourceEventIds.Any(static eventId =>
                eventId <= 0)) {
            throw new ArgumentException(
                "Source event IDs must be positive.",
                nameof(sourceEventIds));
        }

        string? normalizedLogName =
            string.IsNullOrWhiteSpace(sourceLogName)
                ? null
                : sourceLogName!.Trim();
        HashSet<int>? allowedEventIds =
            sourceEventIds == null
                ? null
                : new HashSet<int>(
                    sourceEventIds);
        var restricted =
            new Dictionary<string, HashSet<int>>(
                StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, HashSet<int>> source in
                 eventInfo) {
            if (normalizedLogName != null &&
                !string.Equals(
                    source.Key,
                    normalizedLogName,
                    StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var eventIds = new HashSet<int>(
                source.Value);
            if (allowedEventIds != null) {
                eventIds.IntersectWith(
                    allowedEventIds);
            }
            if (eventIds.Count > 0) {
                restricted[source.Key] =
                    eventIds;
            }
        }
        return restricted;
    }

    private static EventLogBatchQuery CreateBatch(
        NamedEventQuery query,
        IReadOnlyDictionary<string, HashSet<int>> eventInfo,
        NamedEventsQueryExecutionInfo executionInfo) {

        (DateTime? startTime, DateTime? endTime) =
            EventTimeRange.Resolve(
                query.StartTime,
                query.EndTime,
                query.TimePeriod);
        string?[] targets = NormalizeTargets(
            query.MachineNames);
        var channelQueries =
            new List<EventLogChannelQuery>();
        foreach (string? target in targets) {
            foreach (KeyValuePair<string, HashSet<int>> source in
                     eventInfo) {
                long? checkpoint =
                    query.MinimumRecordIdExclusiveResolver?
                        .Invoke(
                            target,
                            source.Key);
                if (checkpoint < 0) {
                    throw new ArgumentOutOfRangeException(
                        nameof(query),
                        "Minimum event record IDs must be greater than or equal to zero.");
                }
                var filter = new EventFilter {
                    EventIds = source.Value
                        .OrderBy(static id => id)
                        .ToArray(),
                    StartTime = startTime,
                    EndTime = endTime,
                    MinimumRecordIdExclusive =
                        checkpoint
                };
                foreach (EventFilter partition in
                         EventFilterPartitioner.Partition(
                             filter)) {
                    channelQueries.Add(
                        new EventLogChannelQuery(
                            source.Key) {
                            MachineName = target,
                            Credential =
                                EventLogTarget.IsLocalMachine(
                                    target)
                                    ? null
                                    : query.Credential,
                            Authentication =
                                query.Authentication,
                            XPath =
                                EventFilterCompiler.BuildXPath(
                                    partition),
                            Oldest = query.Oldest,
                            ReadMode =
                                query.ReadMode,
                            IncludeBookmark =
                                query.IncludeBookmark,
                            MessageCulture =
                                query.MessageCulture,
                            FallbackMessageCulture =
                                query.FallbackMessageCulture,
                            RemoteConnectionTimeoutMilliseconds =
                                query.RemoteConnectionTimeoutMilliseconds,
                            RemoteReadTimeoutMilliseconds =
                                query.RemoteReadTimeoutMilliseconds,
                            BufferCapacity =
                                query.BufferCapacity
                        });
                }
            }
        }

        EventLogBatchQuery batch =
            EventLogBatchQuery.ForChannels(
                channelQueries);
        batch.MaxConcurrency =
            query.MaxConcurrency;
        batch.ContinueOnError =
            query.ContinueOnRemoteFailure;
        batch.FailureHandler =
            failure => HandleFailure(
                failure,
                executionInfo,
                eventInfo.Keys);
        return EventLogBatchConsolidator.Consolidate(
            batch);
    }

    private static void HandleFailure(
        EventLogQueryFailure failure,
        NamedEventsQueryExecutionInfo executionInfo,
        IEnumerable<string> sourceLogNames) {

        if (EventLogRemoteQueryFailureClassifier.TryClassify(
                failure.MachineName,
                failure.Exception,
                out EventLogRemoteQueryFailureKind kind)) {
            foreach (string sourceLogName in
                     sourceLogNames) {
                executionInfo.RecordTargetFailure(
                    new EventLogQueryTargetFailure(
                        failure.MachineName!,
                        sourceLogName,
                        kind,
                        failure.Exception.Message));
            }
            return;
        }
        throw failure.Exception;
    }

    private static string?[] NormalizeTargets(
        IReadOnlyList<string?>? machineNames) {

        IEnumerable<string?> candidates =
            machineNames == null ||
            machineNames.Count == 0
                ? new string?[] { null }
                : machineNames;
        return candidates
            .Select(static machine =>
                EventLogTarget.IsLocalMachine(machine)
                    ? null
                    : machine?.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void Validate(
        NamedEventQuery query) {

        if (query == null) {
            throw new ArgumentNullException(
                nameof(query));
        }
        if (query.MaxEvents < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Maximum events must be greater than or equal to zero.");
        }
        if (query.MaxCandidates < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Maximum candidates must be greater than or equal to zero.");
        }
        if (query.MaxConcurrency <= 0 ||
            query.MaxConcurrency >
            EventLogLimits.MaximumConcurrency) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                $"Maximum concurrency must be between 1 and {EventLogLimits.MaximumConcurrency}.");
        }
        if (query.BufferCapacity <= 0 ||
            query.BufferCapacity > 4096) {
            throw new ArgumentOutOfRangeException(
                nameof(query),
                "Buffer capacity must be between 1 and 4096.");
        }
        query.Enrichment?.Validate();
    }
}
