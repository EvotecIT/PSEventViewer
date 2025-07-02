using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Runtime.CompilerServices;
namespace EventViewerX {
    public partial class SearchEvents : Settings {

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