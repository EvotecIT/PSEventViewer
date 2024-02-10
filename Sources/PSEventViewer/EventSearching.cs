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
        /// Search for events in the event log
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
        /// <returns></returns>
        public static IEnumerable<EventObject> QueryLogNoErrorhandling(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0) {
            string queryString = BuildQueryString(eventIds, providerName, keywords, level, startTime, endTime, userId);

            _logger.WriteVerbose($"Querying log '{logName}' with query: {queryString} on {machineName}");

            EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
            if (machineName != null) {
                query.Session = new EventLogSession(machineName);
            }

            var queriedMachine = machineName ?? GetFQDN();

            using (EventLogReader reader = new EventLogReader(query)) {
                EventRecord record;
                int eventCount = 0;
                while ((record = reader.ReadEvent()) != null) {
                    using (record) {
                        EventObject eventObject = new EventObject(record, queriedMachine);
                        yield return eventObject;
                        eventCount++;
                        if (maxEvents > 0 && eventCount >= maxEvents) {
                            break;
                        }
                    }
                }
            }
        }

        private static EventLogReader CreateEventLogReader(EventLogQuery query, string machineName) {
            try {
                return new EventLogReader(query);
            } catch (EventLogException ex) {
                _logger.WriteWarning($"An error occurred on {machineName} while creating the event log reader: {ex.Message}");
                return null;
            }
        }

        public static string GetFQDN() {
            //string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            //string hostName = Dns.GetHostName();

            //domainName = "." + domainName;
            //if (!hostName.EndsWith(domainName)) {
            //    // if hostname does not already include domain name
            //    hostName += domainName;
            //}
            //return hostName; // return the fully qualified name
            //return Dns.GetHostEntry("LocalHost").HostName;
            return Dns.GetHostEntry("").HostName;
            //return System.Net.Dns.GetHostEntry(Environment.MachineName).HostName;
        }

        public static IEnumerable<EventObject> QueryLog(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0) {
            string queryString = BuildQueryString(eventIds, providerName, keywords, level, startTime, endTime, userId);

            _logger.WriteVerbose($"Querying log '{logName}' with query: {queryString}");

            EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
            if (machineName != null) {
                query.Session = new EventLogSession(machineName);
            }

            var queriedMachine = machineName ?? GetFQDN();
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



        private static string BuildQueryString(List<int> eventIds, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null) {
            StringBuilder queryString = new StringBuilder();

            // Add event IDs to the query
            queryString.AppendFormat("*[System[{0}]", string.Join(" or ", eventIds.Select(id => $"EventID={id}")));

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

        public static IEnumerable<EventObject> QueryLogsParallel(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4) {
            if (machineNames == null || !machineNames.Any()) {
                throw new ArgumentException("At least one machine name must be provided", nameof(machineNames));
            }

            _logger.WriteVerbose("Querying logs in parallel");

            var semaphore = new SemaphoreSlim(maxThreads);
            var results = new BlockingCollection<EventObject>();

            _logger.WriteVerbose("Creating tasks for each machine: " + string.Join(", ", machineNames));

            var tasks = machineNames.Select(machineName => Task.Run(async () => {
                _logger.WriteVerbose("Starting task for machine: " + machineName + " logName: " + logName + " event ids: " + eventIds);
                await semaphore.WaitAsync();
                try {
                    _logger.WriteVerbose("Starting task for machine: " + machineName + " logName: " + logName + " event ids: " + eventIds);
                    var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents);
                    foreach (var result in queryResults) {
                        results.Add(result);
                    }
                    _logger.WriteVerbose("Finished task for machine: " + machineName);
                } finally {
                    semaphore.Release();
                }
            })).ToList();

            Task.Factory.StartNew(() => {
                Task.WaitAll(tasks.ToArray());
                results.CompleteAdding();
            });

            return results.GetConsumingEnumerable();
        }
        public static IEnumerable<EventObject> QueryLogsParallelForEach(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 4) {
            if (machineNames == null || !machineNames.Any()) {
                throw new ArgumentException("At least one machine name must be provided", nameof(machineNames));
            }

            var results = new BlockingCollection<EventObject>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

            Task.Factory.StartNew(() => {
                Parallel.ForEach(machineNames, options, machineName => {
                    Console.WriteLine("Starting task for machine: " + machineName);
                    var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents);
                    foreach (var result in queryResults) {
                        results.Add(result);
                    }
                    Console.WriteLine("Finished task for machine: " + machineName);
                });
                results.CompleteAdding();
            });

            return results.GetConsumingEnumerable();
        }


        public ConcurrentQueue<EventObject> results = new ConcurrentQueue<EventObject>();

        public async Task QueryLogParallelTask(List<string> machineNames, string logName, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0) {
            _logger.WriteVerbose("Querying logs in parallel");
            var tasks = machineNames.Select(machineName => Task.Factory.StartNew(() => {
                try {
                    _logger.WriteVerbose("Starting task for machine: " + machineName);
                    int resultCount = 0;
                    foreach (var result in QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents)) {
                        results.Enqueue(result);
                        resultCount++;
                        _logger.WriteVerbose("Added result to queue: " + result.Id);
                    }
                    _logger.WriteVerbose($"Finished task for machine: {machineName}. Found {resultCount} results.");
                } catch (Exception ex) {
                    _logger.WriteWarning("Exception in task for machine: " + machineName + ". Exception: " + ex.ToString());
                    // Depending on your requirements, you might want to rethrow the exception or stop processing further machines.
                    // throw;
                }
            }, TaskCreationOptions.LongRunning));

            await Task.WhenAll(tasks);
            _logger.WriteVerbose("Finished querying logs in parallel");
        }

        public static void JustRunMe(EventSearching eventSearching, List<string> machineNames, string logName, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0) {
            var queryTask = eventSearching.QueryLogParallelTask(machineNames, logName, eventIds, providerName, keywords, level, startTime, endTime, userId, maxEvents);
            queryTask.GetAwaiter().GetResult();

            Console.WriteLine(eventSearching.results.Count);
        }
    }
}