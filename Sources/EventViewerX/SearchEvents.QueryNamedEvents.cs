using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EventViewerX {
    /// <summary>
    /// Methods for querying events by predefined named event types.
    /// </summary>
    public partial class SearchEvents : Settings {
        /// <summary>
        /// Searches logs for events matching the provided named event types.
        /// </summary>
        /// <param name="typeEventsList">Event types to locate.</param>
        /// <param name="machineNames">Target machines to query.</param>
        /// <param name="startTime">Optional start time.</param>
        /// <param name="endTime">Optional end time.</param>
        /// <param name="timePeriod">Predefined time period.</param>
        /// <param name="maxThreads">Maximum parallel threads.</param>
        /// <param name="maxEvents">Global maximum number of matching rule results to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="maxEventsScanned">Global maximum number of candidate event records to evaluate before rule filtering.</param>
        /// <param name="executionInfo">Optional progress and failure object populated while the query is enumerated. Supplying it opts single remote sources into typed partial-result reporting.</param>
        /// <param name="minimumEventRecordIdExclusiveResolver">Optional per-machine/per-log native checkpoint resolver.</param>
        /// <param name="candidateObserver">Optional observer invoked for every globally merged candidate delivered for named-event projection.</param>
        /// <param name="oldest">Whether to enumerate candidates and select results from oldest to newest.</param>
        /// <param name="resultPredicate">Optional predicate applied to projected named-event results before enforcing <paramref name="maxEvents"/>.</param>
        /// <param name="sourceLogName">Optional exact log source filter applied before the candidate scan cap.</param>
        /// <param name="sourceEventIds">Optional event-ID source filter applied before the candidate scan cap.</param>
        /// <param name="enrichmentOptions">Optional post-projection enrichment. Enrichment completes before checkpoint observation so failures cannot skip records.</param>
        /// <returns>Asynchronous sequence of simplified events.</returns>
        public static async IAsyncEnumerable<EventObjectSlim> FindEventsByNamedEvents(
            List<NamedEvents> typeEventsList,
            List<string?>? machineNames = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            TimePeriod? timePeriod = null,
            int maxThreads = 8,
            int maxEvents = 0,
            [EnumeratorCancellation] CancellationToken cancellationToken = default,
            int maxEventsScanned = 0,
            NamedEventsQueryExecutionInfo? executionInfo = null,
            Func<string?, string, long?>? minimumEventRecordIdExclusiveResolver = null,
            Action<EventObject>? candidateObserver = null,
            bool oldest = false,
            Func<EventObjectSlim, bool>? resultPredicate = null,
            string? sourceLogName = null,
            IReadOnlyCollection<int>? sourceEventIds = null,
            NamedEventEnrichmentOptions? enrichmentOptions = null) {

            if (typeEventsList == null) {
                throw new ArgumentNullException(nameof(typeEventsList));
            }
            if (maxThreads <= 0 || maxThreads > MaximumParallelism) {
                throw new ArgumentOutOfRangeException(nameof(maxThreads), $"Maximum threads must be between 1 and {MaximumParallelism}.");
            }
            if (maxEvents < 0) {
                throw new ArgumentOutOfRangeException(nameof(maxEvents), "Maximum events must be greater than or equal to zero.");
            }
            if (maxEventsScanned < 0) {
                throw new ArgumentOutOfRangeException(nameof(maxEventsScanned), "Maximum scanned events must be greater than or equal to zero.");
            }
            enrichmentOptions?.Validate();

            Dictionary<string, HashSet<int>> eventInfo = RestrictNamedEventSources(
                EventObjectSlim.GetEventInfoForNamedEvents(typeEventsList),
                sourceLogName,
                sourceEventIds);
            NamedEventsQueryExecutionInfo queryInfo = executionInfo ?? new NamedEventsQueryExecutionInfo();
            Action<EventLogQueryTargetFailure>? targetFailureObserver = executionInfo == null
                ? null
                : queryInfo.RecordTargetFailure;
            queryInfo.Reset(maxEventsScanned);
            int emitted = 0;
            using var enricher = enrichmentOptions == null ? null : new NamedEventEnricher(enrichmentOptions);

            if (maxEventsScanned > 0) {
                int candidateLimit = maxEventsScanned == int.MaxValue ? int.MaxValue : maxEventsScanned + 1;
                await foreach (NamedEventProjection projection in ProjectNamedCandidatesInOrderAsync(
                                   QueryNamedPagedCandidatesAsync(
                                       eventInfo,
                                       machineNames,
                                       startTime,
                                       endTime,
                                       timePeriod,
                                       candidateLimit,
                                       maxThreads,
                                       cancellationToken,
                                       minimumEventRecordIdExclusiveResolver,
                                       oldest,
                                       targetFailureObserver),
                                   typeEventsList,
                                   enricher,
                                   queryInfo.TryRecordCandidate,
                                   candidateObserver,
                                   cancellationToken)) {
                    EventObjectSlim? targetEvent = projection.Target;
                    if (targetEvent == null || (resultPredicate != null && !resultPredicate(targetEvent))) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                    if (maxEvents > 0 && emitted >= maxEvents) {
                        yield break;
                    }
                }
                yield break;
            }

            if (oldest && minimumEventRecordIdExclusiveResolver != null) {
                await foreach (NamedEventProjection projection in ProjectNamedCandidatesInOrderAsync(
                                   QueryNamedPagedCandidatesAsync(
                                       eventInfo,
                                       machineNames,
                                       startTime,
                                       endTime,
                                       timePeriod,
                                       maxEvents: 0,
                                       maxThreads,
                                       cancellationToken,
                                       minimumEventRecordIdExclusiveResolver,
                                       oldest: true,
                                       targetFailureObserver),
                                   typeEventsList,
                                   enricher,
                                   queryInfo.TryRecordCandidate,
                                   candidateObserver,
                                   cancellationToken)) {
                    EventObjectSlim? targetEvent = projection.Target;
                    if (targetEvent == null || (resultPredicate != null && !resultPredicate(targetEvent))) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                    if (maxEvents > 0 && emitted >= maxEvents) {
                        yield break;
                    }
                }
                yield break;
            }

            if (maxEvents > 0) {
                await foreach (NamedEventProjection projection in ProjectNamedCandidatesInOrderAsync(
                                   QueryNamedPagedCandidatesAsync(
                                       eventInfo,
                                       machineNames,
                                       startTime,
                                       endTime,
                                       timePeriod,
                                       maxEvents,
                                       maxThreads,
                                       cancellationToken,
                                       minimumEventRecordIdExclusiveResolver,
                                       oldest,
                                       targetFailureObserver),
                                   typeEventsList,
                                   enricher,
                                   () => {
                                       queryInfo.EventsScanned++;
                                       return true;
                                   },
                                   candidateObserver,
                                   cancellationToken)) {
                    EventObjectSlim? targetEvent = projection.Target;
                    if (targetEvent == null || (resultPredicate != null && !resultPredicate(targetEvent))) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                    if (emitted >= maxEvents) {
                        yield break;
                    }
                }
                yield break;
            }

            // Unlimited queries retain streaming/backpressure and do not need a cross-log selection buffer.
            foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                await foreach (NamedEventProjection projection in ProjectNamedCandidatesInOrderAsync(
                                   QueryNamedEventCandidates(
                                       entry,
                                       machineNames,
                                       startTime,
                                       endTime,
                                       timePeriod,
                                       maxThreads,
                                       maxEvents: 0,
                                       cancellationToken,
                                       minimumEventRecordIdExclusiveResolver,
                                       oldest,
                                       targetFailureObserver),
                                   typeEventsList,
                                   enricher,
                                   queryInfo.TryRecordCandidate,
                                   candidateObserver,
                                   cancellationToken)) {
                    EventObjectSlim? targetEvent = projection.Target;
                    if (targetEvent == null || (resultPredicate != null && !resultPredicate(targetEvent))) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                }
            }
        }

        internal static Dictionary<string, HashSet<int>> RestrictNamedEventSources(
            IReadOnlyDictionary<string, HashSet<int>> eventInfo,
            string? sourceLogName,
            IReadOnlyCollection<int>? sourceEventIds) {

            if (eventInfo == null) {
                throw new ArgumentNullException(nameof(eventInfo));
            }
            if (sourceEventIds != null && sourceEventIds.Any(static eventId => eventId <= 0)) {
                throw new ArgumentException("Source event IDs must be positive.", nameof(sourceEventIds));
            }

            string? normalizedLogName = string.IsNullOrWhiteSpace(sourceLogName)
                ? null
                : sourceLogName!.Trim();
            HashSet<int>? allowedEventIds = sourceEventIds == null
                ? null
                : new HashSet<int>(sourceEventIds);
            var restricted = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, HashSet<int>> source in eventInfo) {
                if (normalizedLogName != null &&
                    !string.Equals(source.Key, normalizedLogName, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var eventIds = new HashSet<int>(source.Value);
                if (allowedEventIds != null) {
                    eventIds.IntersectWith(allowedEventIds);
                }
                if (eventIds.Count > 0) {
                    restricted[source.Key] = eventIds;
                }
            }
            return restricted;
        }

        private static IAsyncEnumerable<EventObject> QueryNamedEventCandidates(
            KeyValuePair<string, HashSet<int>> entry,
            List<string?>? machineNames,
            DateTime? startTime,
            DateTime? endTime,
            TimePeriod? timePeriod,
            int maxThreads,
            int maxEvents,
            CancellationToken cancellationToken,
            Func<string?, string, long?>? minimumEventRecordIdExclusiveResolver,
            bool oldest,
            Action<EventLogQueryTargetFailure>? targetFailureObserver) {

            return QueryLogsParallel(
                entry.Key,
                entry.Value.ToList(),
                machineNames,
                startTime: startTime,
                endTime: endTime,
                maxEvents: maxEvents,
                maxThreads: maxThreads,
                timePeriod: timePeriod,
                cancellationToken: cancellationToken,
                readMode: EventReadMode.Full,
                minimumEventRecordIdExclusiveResolver: minimumEventRecordIdExclusiveResolver == null
                    ? null
                    : machineName => minimumEventRecordIdExclusiveResolver(machineName, entry.Key),
                oldest: oldest,
                targetFailureObserver: targetFailureObserver);
        }

    }
}
