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
        /// <param name="executionInfo">Optional progress object populated while the query is enumerated.</param>
        /// <param name="minimumEventRecordIdExclusiveResolver">Optional per-machine/per-log native checkpoint resolver.</param>
        /// <param name="candidateObserver">Optional observer invoked for every native candidate before named-event projection.</param>
        /// <param name="oldest">Whether to enumerate candidates and select results from oldest to newest.</param>
        /// <param name="resultPredicate">Optional predicate applied to projected named-event results before enforcing <paramref name="maxEvents"/>.</param>
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
            Func<EventObjectSlim, bool>? resultPredicate = null) {

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

            Dictionary<string, HashSet<int>> eventInfo = EventObjectSlim.GetEventInfoForNamedEvents(typeEventsList);
            NamedEventsQueryExecutionInfo queryInfo = executionInfo ?? new NamedEventsQueryExecutionInfo();
            queryInfo.Reset(maxEventsScanned);
            int emitted = 0;

            if (maxEventsScanned > 0) {
                int candidateLimit = maxEventsScanned == int.MaxValue ? int.MaxValue : maxEventsScanned + 1;
                foreach (EventObject foundEvent in QueryNamedPagedCandidates(
                             eventInfo,
                             machineNames,
                             startTime,
                             endTime,
                             timePeriod,
                             candidateLimit,
                             cancellationToken,
                             minimumEventRecordIdExclusiveResolver,
                             oldest,
                             queryInfo.RecordTargetFailure)) {
                    if (!queryInfo.TryRecordCandidate()) {
                        yield break;
                    }

                    candidateObserver?.Invoke(foundEvent);
                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
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
                foreach (EventObject foundEvent in QueryNamedPagedCandidates(
                             eventInfo,
                             machineNames,
                             startTime,
                             endTime,
                             timePeriod,
                             maxEvents: 0,
                             cancellationToken,
                             minimumEventRecordIdExclusiveResolver,
                             oldest: true,
                             queryInfo.RecordTargetFailure)) {
                    queryInfo.TryRecordCandidate();
                    candidateObserver?.Invoke(foundEvent);

                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
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
                var projections = new Dictionary<EventObject, EventObjectSlim>();
                foreach (EventObject foundEvent in QueryNamedPagedCandidates(
                             eventInfo,
                             machineNames,
                             startTime,
                             endTime,
                             timePeriod,
                             maxEvents,
                             cancellationToken,
                             minimumEventRecordIdExclusiveResolver,
                             oldest,
                             queryInfo.RecordTargetFailure,
                             candidate => {
                                 queryInfo.EventsScanned++;
                                 candidateObserver?.Invoke(candidate);
                             },
                             candidate => {
                                 EventObjectSlim? projection = BuildTargetEvents(candidate, typeEventsList);
                                 if (projection == null || (resultPredicate != null && !resultPredicate(projection))) {
                                     return false;
                                 }
                                 projections[candidate] = projection;
                                 return true;
                             })) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!projections.TryGetValue(foundEvent, out EventObjectSlim? targetEvent)) {
                        continue;
                    }
                    projections.Remove(foundEvent);

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent!;
                }
                yield break;
            }

            // Unlimited queries retain streaming/backpressure and do not need a cross-log selection buffer.
            foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                await foreach (EventObject foundEvent in QueryNamedEventCandidates(
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
                                   queryInfo.RecordTargetFailure)) {
                    queryInfo.TryRecordCandidate();

                    candidateObserver?.Invoke(foundEvent);

                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent == null || (resultPredicate != null && !resultPredicate(targetEvent))) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                }
            }
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
