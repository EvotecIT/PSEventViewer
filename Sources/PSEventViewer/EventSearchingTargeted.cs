using PSEventViewer.Rules.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace PSEventViewer {

    public enum NamedEvents {
        ADComputerChangeDetailed,
        ADLdapBindingSummary,
        ADLdapBindingDetails,
        ADUserChange,
        ADUserStatus,
        ADUserChangeDetailed,
        ADUserLockouts,
        ADUserLogon,
        ADUserUnlocked,
        ADOrganizationalUnitChangeDetailed,
        ADOtherChangeDetailed,
    }

    public class EventSearchingTargeted : Settings {
        //public static EventObjectSlim BuildTargetEvents(EventObject eventObject) {
        //    switch (eventObject.LogName) {
        //        case "Security":
        //            switch (eventObject.Id) {
        //                case 4720:
        //                case 4738:
        //                    return new ADUserChange(eventObject); // Includes users added or modified in Active Directory
        //                case 4722:
        //                case 4725:
        //                //case 4767:
        //                case 4723:
        //                case 4724:
        //                case 4726:
        //                    return new ADUserStatus(eventObject);
        //                case 5136:
        //                case 5137:
        //                case 5139:
        //                case 5141:
        //                    if (eventObject.Data["ObjectClass"] == "user") {
        //                        return new ADUserChangeDetailed(eventObject);
        //                    } else if (eventObject.Data["ObjectClass"] == "computer") {
        //                        return new ADComputerChangeDetailed(eventObject);
        //                    } else if (eventObject.Data["ObjectClass"] == "organizationalUnit") {
        //                        return new ADOrganizationalUnitChangeDetailed(eventObject);
        //                    } else {
        //                        return new ADOtherChangeDetailed(eventObject);
        //                    }
        //                case 4740:
        //                    return new ADUserLockouts(eventObject);
        //                case 4624:
        //                    return new ADUserLogon(eventObject);
        //                case 4767:
        //                    // both support the same
        //                    //return new ADUserStatus(eventObject);
        //                    return new ADUserUnlocked(eventObject);
        //                default:
        //                    throw new ArgumentException("Invalid EventID for Security LogName");
        //            }
        //        case "Application":
        //            // Handle Application LogName here
        //            break;
        //        case "System":
        //            // Handle System LogName here
        //            break;
        //        case "Setup":
        //            // Handle Setup LogName here
        //            break;
        //        case "Directory Service":
        //            // Handle Directory Service LogName here
        //            break;
        //        default:
        //            throw new ArgumentException("Invalid LogName");
        //    }
        //    return null;
        //}

        public static EventObjectSlim BuildTargetEvents(EventObject eventObject, List<NamedEvents> typeEventsList) {
            // Check if the event ID and log name match any of the NamedEvents values
            foreach (var typeEvents in typeEventsList) {
                if (eventIdsMap.TryGetValue(typeEvents, out var eventInfo) &&
                    eventInfo.EventIds.Contains(eventObject.Id) &&
                    eventInfo.LogName == eventObject.LogName) {
                    // If they match, create the appropriate event object based on the NamedEvents value
                    switch (typeEvents) {
                        case NamedEvents.ADUserChange:
                            return new ADUserChange(eventObject);
                        case NamedEvents.ADUserStatus:
                            return new ADUserStatus(eventObject);
                        case NamedEvents.ADUserChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "user") {
                                return new ADUserChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADComputerChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "computer") {
                                return new ADComputerChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADOrganizationalUnitChangeDetailed:
                            if (eventObject.Data["ObjectClass"] == "organizationalUnit") {
                                return new ADOrganizationalUnitChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADOtherChangeDetailed:
                            if (eventObject.Data["ObjectClass"] != "user" && eventObject.Data["ObjectClass"] != "computer" && eventObject.Data["ObjectClass"] != "organizationalUnit") {
                                return new ADOtherChangeDetailed(eventObject);
                            }
                            break;
                        case NamedEvents.ADUserLockouts:
                            return new ADUserLockouts(eventObject);
                        case NamedEvents.ADUserLogon:
                            return new ADUserLogon(eventObject);
                        case NamedEvents.ADUserUnlocked:
                            return new ADUserUnlocked(eventObject);
                        case NamedEvents.ADLdapBindingSummary:
                            return new ADLdapBindingSummary(eventObject);
                        case NamedEvents.ADLdapBindingDetails:
                            return new ADLdapBindingDetails(eventObject);
                        default:
                            throw new ArgumentException($"Invalid NamedEvents value: {typeEvents}");
                    }
                }
            }

            // If no match is found, return null or throw an exception
            return null;
        }


        private static readonly Dictionary<NamedEvents, (List<int> EventIds, string LogName)> eventIdsMap = new Dictionary<NamedEvents, (List<int>, string)> {
            { NamedEvents.ADUserChange, (new List<int> { 4720, 4738 }, "Security") },
            { NamedEvents.ADUserStatus, (new List<int> { 4722, 4725, 4723, 4724, 4726 }, "Security") },
            { NamedEvents.ADUserChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADComputerChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADOrganizationalUnitChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADOtherChangeDetailed, (new List<int> { 5136, 5137, 5139, 5141 }, "Security") },
            { NamedEvents.ADUserLockouts, (new List<int> { 4740 }, "Security") },
            { NamedEvents.ADUserLogon, (new List<int> { 4624 }, "Security") },
            { NamedEvents.ADUserUnlocked, (new List<int> { 4767 }, "Security") },
            { NamedEvents.ADLdapBindingSummary, (new List<int> { 2887 }, "Directory Service") },
            { NamedEvents.ADLdapBindingDetails,(new List<int> { 2889 }, "Directory Service") }
        };

        public EventSearchingTargeted(InternalLogger internalLogger) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }


        //public static IEnumerable<EventObjectSlim> FindEvents(List<string> machineNames, NamedEvents typeEvents) {
        //    List<int> eventIds = new List<int> { 4720, 4738, 4722, 4725, 4767, 4723, 4724, 4726, 5136, 5139, 5141 };

        //    foreach (var foundEvent in EventSearching.QueryLogsParallel("Security", eventIds, machineNames)) {
        //        yield return BuildTargetEvents(foundEvent);
        //    }
        //}


        //public static IEnumerable<EventObjectSlim> FindEvents() {
        //    List<int> eventIds = new List<int> { 4720, 4738, 4722, 4725, 4767, 4723, 4724, 4726, 5136, 5139, 5141 };
        //    List<string> machineNames = new List<string> { "AD0", "AD1" };
        //    foreach (var foundEvent in EventSearching.QueryLogsParallel("Security", eventIds, machineNames)) {
        //        yield return BuildTargetEvents(foundEvent);
        //    }
        //}

        //public static async Task<List<EventObjectSlim>> FindEventsByNamedEvents(List<string> machineNames, List<NamedEvents> typeEventsList) {
        //    var eventInfoDict = new Dictionary<string, HashSet<int>>();
        //    var results = new ConcurrentBag<EventObjectSlim>(); // Thread-safe collection for storing results


        //    foreach (var typeEvents in typeEventsList) {
        //        if (!eventIdsMap.TryGetValue(typeEvents, out var eventInfo)) {
        //            throw new ArgumentException($"Invalid typeEvents value: {typeEvents}");
        //        }

        //        if (!eventInfoDict.TryGetValue(eventInfo.LogName, out var eventIds)) {
        //            eventIds = new HashSet<int>();
        //            eventInfoDict[eventInfo.LogName] = eventIds;
        //        }

        //        foreach (var eventId in eventInfo.EventIds) {
        //            eventIds.Add(eventId);
        //        }
        //    }

        //    CancellationTokenSource cts = new CancellationTokenSource();
        //    int maxDegreeOfParallelism = 8; // Adjust this value as needed
        //    SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        //    List<Task> tasks = new List<Task>();

        //    foreach (var kvp in eventInfoDict) {
        //        var logName = kvp.Key;
        //        var eventIds = kvp.Value.ToList();

        //        _logger.WriteDebug($"FindEventsByNamedEvents: {logName} {string.Join(", ", eventIds)}");

        //        foreach (var machineName in machineNames) {
        //            await semaphore.WaitAsync(cts.Token);
        //            tasks.Add(Task.Run(async () => {
        //                try {
        //                    if (cts.IsCancellationRequested) {
        //                        semaphore.Release();
        //                        return;
        //                    }

        //                    foreach (var foundEvent in EventSearching.QueryLog(logName, eventIds, machineName)) {
        //                        _logger.WriteDebug($"Found event: {foundEvent.Id} {foundEvent.LogName} {foundEvent.ComputerName}");
        //                        var targetEvent = BuildTargetEvents(foundEvent, typeEventsList);
        //                        if (targetEvent != null) {
        //                            results.Add(targetEvent);
        //                        }
        //                    }
        //                } finally {
        //                    semaphore.Release();
        //                }
        //            }, cts.Token));
        //        }
        //    }

        //    await Task.WhenAll(tasks);
        //    return results.ToList(); // Convert the ConcurrentBag to a List and return it
        //}

        public static IEnumerable<EventObjectSlim> FindEventsByNamedEvents(List<NamedEvents> typeEventsList, List<string> machineNames = null) {
            // Create a dictionary to store unique event IDs and log names
            var eventInfoDict = new Dictionary<string, HashSet<int>>();

            foreach (var typeEvents in typeEventsList) {
                // Look up the list of event IDs and the log name based on typeEvents
                if (!eventIdsMap.TryGetValue(typeEvents, out var eventInfo)) {
                    throw new ArgumentException($"Invalid typeEvents value: {typeEvents}");
                }

                // Add the event IDs to the dictionary
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

                //_logger.WriteDebug($"FindEventsByNamedEvents: {logName} {string.Join(", ", eventIds)}");

                foreach (var foundEvent in EventSearching.QueryLogsParallel(logName, eventIds, machineNames)) {
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
