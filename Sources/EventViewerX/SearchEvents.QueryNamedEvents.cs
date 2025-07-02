using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Runtime.CompilerServices;
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
        /// <param name="maxEvents">Maximum events to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Asynchronous sequence of simplified events.</returns>
        public static async IAsyncEnumerable<EventObjectSlim> FindEventsByNamedEvents(List<NamedEvents> typeEventsList, List<string> machineNames = null, DateTime? startTime = null, DateTime? endTime = null, TimePeriod? timePeriod = null, int maxThreads = 8, int maxEvents = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            // Use the new reflection-based system to get event info
            var eventInfoDict = EventObjectSlim.GetEventInfoForNamedEvents(typeEventsList);

            // Query each log name with the corresponding event IDs
            foreach (var kvp in eventInfoDict) {
                var logName = kvp.Key;
                var eventIds = kvp.Value.ToList();

                await foreach (var foundEvent in SearchEvents.QueryLogsParallel(logName, eventIds, machineNames, startTime: startTime, endTime: endTime, timePeriod: timePeriod, maxThreads: maxThreads, maxEvents: maxEvents, cancellationToken: cancellationToken)) {
                    _logger.WriteDebug($"Found event: {foundEvent.Id} {foundEvent.LogName} {foundEvent.ComputerName}");
                    var targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent != null) {
                        yield return targetEvent;
                    }
                }
            }
        }
    }
}