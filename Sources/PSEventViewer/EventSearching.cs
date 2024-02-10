using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSEventViewer {
    public class EventSearching : Settings {
        /// <summary>
        /// Initialize the EventSearching class with an internal logger
        /// </summary>
        /// <param name="internalLogger"></param>
        public EventSearching(InternalLogger internalLogger = null) {
            if (internalLogger != null) {
                _logger = internalLogger;
            }
        }

        /// <summary>
        /// Create an event log reader allowing for catching errors and logging them
        /// </summary>
        /// <param name="query"></param>
        /// <param name="machineName"></param>
        /// <returns></returns>
        private static EventLogReader CreateEventLogReader(EventLogQuery query, string machineName) {
            try {
                return new EventLogReader(query);
            } catch (EventLogException ex) {
                _logger.WriteWarning($"An error occurred on {machineName} while creating the event log reader: {ex.Message}");
                return null;
            } catch (UnauthorizedAccessException ex) {
                _logger.WriteWarning($"Insufficient permissions to read the event log on {machineName}: {ex.Message}");
                return null;
            } catch (Exception ex) {
                _logger.WriteWarning($"An error occurred on {machineName} while creating the event log reader: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the fully qualified domain name of the machine
        /// </summary>
        /// <returns></returns>
        public static string GetFQDN() {
            return Dns.GetHostEntry("").HostName;
        }


        /// <summary>
        /// Query a log for events
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="eventIds"></param>
        /// <param name="machineName"></param>
        /// <param name="providerName"></param>
        /// <param name="keywords"></param>
        /// <param name="level"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="userId"></param>
        /// <param name="maxEvents"></param>
        /// <param name="eventRecordId"></param>
        /// <returns></returns>
        public static IEnumerable<EventObject> QueryLog(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null) {
            string queryString;
            if (eventRecordId != null) {
                // If eventRecordId is provided, query the log for the specific event record ID
                queryString = BuildQueryString(eventRecordId);
            } else {
                // If eventRecordId is not provided, query the log for events based on the provided parameters
                queryString = BuildQueryString(eventIds, providerName, keywords, level, startTime, endTime, userId);
            }

            _logger.WriteVerbose($"Querying log '{logName}' on '{machineName} with query: {queryString}");

            EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
            if (machineName != null) {
                query.Session = new EventLogSession(machineName);
            }

            // If machineName is null, the query will be executed on the local machine
            // , but we still want to know the fully qualified domain name of the machine for logging purposes
            var queriedMachine = machineName ?? GetFQDN();

            // We need to keep record not disposed to be able to access it after the using block
            // Maybe there's a better way to do this
            EventRecord record;
            using (EventLogReader reader = CreateEventLogReader(query, machineName)) {
                if (reader != null) {
                    int eventCount = 0;
                    while ((record = reader.ReadEvent()) != null) {
                        // using (record) {
                        EventObject eventObject = new EventObject(record, queriedMachine);
                        yield return eventObject;
                        eventCount++;
                        if (maxEvents > 0 && eventCount >= maxEvents) {
                            break;
                        }
                        // }
                    }
                }
            }
        }

        /// <summary>
        /// Build a query string for querying a log for a specific event record ID or IDs
        /// </summary>
        /// <param name="eventRecordIds"></param>
        /// <returns></returns>
        private static string BuildQueryString(List<long> eventRecordIds) {
            if (eventRecordIds != null && eventRecordIds.Any()) {
                return $"*[System[{string.Join(" or ", eventRecordIds.Select(id => $"EventRecordID={id}"))}]]";
            } else {
                return "*";
            }
        }


        /// <summary>
        /// Build a query string for querying a log for events based on the provided parameters
        /// </summary>
        /// <param name="eventIds"></param>
        /// <param name="providerName"></param>
        /// <param name="keywords"></param>
        /// <param name="level"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private static string BuildQueryString(List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null) {
            StringBuilder queryString = new StringBuilder();

            // Add event IDs to the query
            if (eventIds != null && eventIds.Any()) {
                queryString.AppendFormat("*[System[{0}]", string.Join(" or ", eventIds.Select(id => $"EventID={id}")));
            } else {
                queryString.Append("*[System[");
            }

            // Add provider name to the query
            if (!string.IsNullOrEmpty(providerName)) {
                queryString.AppendFormat(" and Provider[@Name='{0}']", providerName);
            }

            // Add keywords to the query
            if (keywords.HasValue) {
                queryString.AppendFormat(" and Keywords={0}", (long)keywords.Value);
            }

            // Add level to the query
            if (level.HasValue) {
                queryString.AppendFormat(" and Level={0}", (int)level.Value);
            }

            // Add time range to the query
            if (startTime.HasValue && endTime.HasValue) {
                queryString.AppendFormat(" and TimeCreated[@SystemTime>='{0:O}' and @SystemTime<='{1:O}']", startTime.Value, endTime.Value);
            }

            // Add user ID to the query
            if (!string.IsNullOrEmpty(userId)) {
                queryString.AppendFormat(" and Security[@UserID='{0}']", userId);
            }

            queryString.Append("]");

            return queryString.ToString();
        }

        public static IEnumerable<EventObject> QueryLogsParallel(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4, List<long> eventRecordId = null) {
            if (machineNames == null || !machineNames.Any()) {
                machineNames = new List<string> { null };
            }
            var semaphore = new SemaphoreSlim(maxThreads);
            var results = new BlockingCollection<EventObject>();

            _logger.WriteVerbose("Creating tasks for each machine: " + string.Join(", ", machineNames));

            var tasks = new List<Task>();
            foreach (var machineName in machineNames) {
                if (eventIds != null) {
                    var eventIdsChunks = eventIds.Select((x, i) => new { Index = i, Value = x })
                        .GroupBy(x => x.Index / 22)
                        .Select(x => x.Select(v => v.Value).ToList())
                        .ToList();

                    foreach (var chunk in eventIdsChunks) {
                        tasks.Add(CreateTask(machineName, logName, chunk, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results));
                    }
                }

                if (eventRecordId != null) {
                    var eventRecordIdChunks = eventRecordId.Select((x, i) => new { Index = i, Value = x })
                        .GroupBy(x => x.Index / 22)
                        .Select(x => x.Select(v => v.Value).ToList())
                        .ToList();

                    foreach (var chunk in eventRecordIdChunks) {
                        tasks.Add(CreateTask(machineName, logName, null, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, chunk));
                    }
                }
            }

            Task.Factory.StartNew(() => {
                Task.WaitAll(tasks.ToArray());
                results.CompleteAdding();
            });

            return results.GetConsumingEnumerable();
        }

        private static Task CreateTask(string machineName, string logName, List<int> eventIds, string providerName, Keywords? keywords, Level? level, DateTime? startTime, DateTime? endTime, string userId, int maxEvents, SemaphoreSlim semaphore, BlockingCollection<EventObject> results, List<long> eventRecordId = null) {
            return Task.Run(async () => {
                _logger.WriteVerbose($"Querying log on machine: {machineName}, logName: {logName}, event ids: " + string.Join(", ", eventIds ?? new List<int>()));
                await semaphore.WaitAsync();
                try {
                    var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId);
                    foreach (var result in queryResults) {
                        results.Add(result);
                    }
                    _logger.WriteVerbose("Querying log on machine: " + machineName + " completed.");
                } finally {
                    semaphore.Release();
                }
            });
        }

        public static IEnumerable<EventObject> QueryLogsParallelForEach(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4, List<long> eventRecordId = null) {
            if (machineNames == null || !machineNames.Any()) {
                throw new ArgumentException("At least one machine name must be provided", nameof(machineNames));
            }

            var results = new BlockingCollection<EventObject>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

            Task.Factory.StartNew(() => {
                Parallel.ForEach(machineNames, options, machineName => {
                    _logger.WriteVerbose("Starting task for machine: " + machineName);
                    var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId);
                    foreach (var result in queryResults) {
                        results.Add(result);
                    }
                    _logger.WriteVerbose("Finished task for machine: " + machineName);
                });
                results.CompleteAdding();
            });

            return results.GetConsumingEnumerable();
        }
    }
}