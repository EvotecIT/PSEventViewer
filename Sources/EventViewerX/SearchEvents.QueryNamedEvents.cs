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
            NamedEventsQueryExecutionInfo? executionInfo = null) {

            if (typeEventsList == null) {
                throw new ArgumentNullException(nameof(typeEventsList));
            }
            if (maxThreads <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxThreads), "Maximum threads must be positive.");
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

            // Query one channel at a time. QueryLogsParallel already owns bounded machine/filter parallelism;
            // adding a second producer layer here multiplies concurrency and defeats its backpressure.
            foreach (KeyValuePair<string, HashSet<int>> entry in eventInfo) {
                int remainingScanLimit = maxEventsScanned > 0
                    ? Math.Max(0, maxEventsScanned - (int)queryInfo.EventsScanned)
                    : 0;
                int queryLimit = remainingScanLimit > 0 && remainingScanLimit < int.MaxValue
                    ? remainingScanLimit + 1
                    : remainingScanLimit == 0 && maxEventsScanned > 0 ? 1 : remainingScanLimit;

                await foreach (EventObject foundEvent in QueryLogsParallel(
                                   entry.Key,
                                   entry.Value.ToList(),
                                   machineNames,
                                   startTime: startTime,
                                   endTime: endTime,
                                   maxEvents: queryLimit,
                                   maxThreads: maxThreads,
                                   timePeriod: timePeriod,
                                   cancellationToken: cancellationToken,
                                   readMode: EventReadMode.Full)) {
                    if (!queryInfo.TryRecordCandidate()) {
                        yield break;
                    }

                    EventObjectSlim? targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent == null) {
                        continue;
                    }

                    emitted++;
                    queryInfo.EventsEmitted = emitted;
                    yield return targetEvent;
                    if (maxEvents > 0 && emitted >= maxEvents) {
                        yield break;
                    }
                }
            }
        }
    }
}
