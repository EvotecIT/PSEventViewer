using System;
using System.Collections.Generic;
using System.Linq;

namespace EventViewerX {
    public partial class SearchEvents : Settings {

        public static IEnumerable<EventObjectSlim> FindEventsByNamedEvents(List<NamedEvents> typeEventsList, List<string> machineNames = null, DateTime? startTime = null, DateTime? endTime = null, TimePeriod? timePeriod = null, int maxThreads = 8, int maxEvents = 0) {
            // Create a dictionary to store unique event IDs and log names
            var eventInfoDict = new Dictionary<string, HashSet<int>>();

            foreach (var typeEvents in typeEventsList) {
                if (!eventDefinitions.TryGetValue(typeEvents, out var eventInfo)) {
                    throw new ArgumentException($"Invalid typeEvents value: {typeEvents}");
                }
                if (!eventInfoDict.TryGetValue(eventInfo.LogName, out var eventIds)) {
                    eventIds = new HashSet<int>();
                    eventInfoDict[eventInfo.LogName] = eventIds;
                }

                foreach (var eventId in eventInfo.EventIds) {
                    eventIds.Add(eventId);
                }
            }

            // Query each log name with the corresponding event IDs
            foreach (var kvp in eventInfoDict) {
                var logName = kvp.Key;
                var eventIds = kvp.Value.ToList();

                foreach (var foundEvent in SearchEvents.QueryLogsParallel(logName, eventIds, machineNames, startTime: startTime, endTime: endTime, timePeriod: timePeriod, maxThreads: maxThreads, maxEvents: maxEvents)) {
                    _logger.WriteDebug($"Found event: {foundEvent.Id} {foundEvent.LogName} {foundEvent.ComputerName}");
                    // yield return BuildTargetEvents(foundEvent, typeEventsList);
                    var targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
                    if (targetEvent != null) {
                        yield return targetEvent;
                    }
                }
            }
        }
    }
}