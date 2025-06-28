using System.Diagnostics.Eventing.Reader;
using System.IO;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>
    /// Initialize the EventSearching class with an internal logger
    /// </summary>
    /// <param name="internalLogger">The internal logger.</param>
    public SearchEvents(InternalLogger internalLogger = null) {
        if (internalLogger != null) {
            _logger = internalLogger;
        }
    }

    /// <summary>
    /// Create an event log reader allowing for catching errors and logging them
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="machineName">Name of the machine.</param>
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
    private static string GetFQDN() {
        return Dns.GetHostEntry("").HostName;
    }

    /// <summary>
    /// Queries events from a Windows event log file (.evtx) with optional filtering criteria.
    /// </summary>
    /// <param name="filePath">The file path to the Windows event log file (.evtx) to query.</param>
    /// <param name="eventIds">Optional list of specific event IDs to filter for.</param>
    /// <param name="providerName">Optional name of the event provider to filter by.</param>
    /// <param name="keywords">Optional keywords to filter events by.</param>
    /// <param name="level">Optional event level to filter by (e.g., Error, Warning, Information).</param>
    /// <param name="startTime">Optional start time to filter events from.</param>
    /// <param name="endTime">Optional end time to filter events until.</param>
    /// <param name="userId">Optional user ID to filter events by.</param>
    /// <param name="maxEvents">Maximum number of events to return. 0 means no limit.</param>
    /// <param name="eventRecordId">Optional list of specific event record IDs to retrieve.</param>
    /// <param name="timePeriod">Optional predefined time period to filter events by.</param>
    /// <param name="oldest">If true, returns events in chronological order (oldest first). If false, returns newest first.</param>
    /// <param name="namedDataFilter">Optional hashtable containing named data filters to include events.</param>
    /// <param name="namedDataExcludeFilter">Optional hashtable containing named data filters to exclude events.</param>
    /// <returns>An enumerable collection of EventObject instances representing the filtered events from the log file.</returns>
    public static IEnumerable<EventObject> QueryLogFile(string filePath, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null, bool oldest = false, System.Collections.Hashtable namedDataFilter = null, System.Collections.Hashtable namedDataExcludeFilter = null) {

        string absolutePath = Path.GetFullPath(filePath);

        if (!File.Exists(absolutePath)) {
            throw new FileNotFoundException($"The log file '{absolutePath}' does not exist.", absolutePath);
        }

        // Check if we have any filters that require an XML query
        bool hasFilters = namedDataFilter != null || namedDataExcludeFilter != null || eventIds != null ||
                         providerName != null || keywords != null || level != null || startTime != null ||
                         endTime != null || userId != null || eventRecordId != null;

        EventLogQuery query;

        if (hasFilters) {
            // Build complex XML query with filters
            var namedDataFilterArray = namedDataFilter != null ? new[] { namedDataFilter } : null;
            var namedDataExcludeFilterArray = namedDataExcludeFilter != null ? new[] { namedDataExcludeFilter } : null;
            var idArray = eventIds?.Select(i => i.ToString()).ToArray();
            var eventRecordIdArray = eventRecordId?.Select(i => i.ToString()).ToArray();
            var providerNameArray = !string.IsNullOrEmpty(providerName) ? new[] { providerName } : null;
            var keywordsArray = keywords != null ? new[] { (long)keywords.Value } : null;
            var levelArray = level != null ? new[] { level.ToString() } : null;
            var userIdArray = !string.IsNullOrEmpty(userId) ? new[] { userId } : null;

            string queryString = BuildWinEventFilter(
                id: idArray,
                eventRecordId: eventRecordIdArray,
                startTime: startTime,
                endTime: endTime,
                providerName: providerNameArray,
                keywords: keywordsArray,
                level: levelArray,
                userId: userIdArray,
                namedDataFilter: namedDataFilterArray,
                namedDataExcludeFilter: namedDataExcludeFilterArray,
                path: absolutePath,
                xpathOnly: false
            );

            _logger.WriteVerbose($"Querying file '{filePath}' with complex query: {queryString}");

            // Complex query with filters - use XML with null path
            query = new EventLogQuery(null, PathType.LogName, queryString) {
                ReverseDirection = !oldest
            };
        } else {
            // Simple query without filters - use XML with wildcard filter
            string queryString = BuildWinEventFilter(
                path: absolutePath,
                xpathOnly: false
            );

            _logger.WriteVerbose($"Querying file '{filePath}' with simple query: {queryString}");

            query = new EventLogQuery(null, PathType.LogName, queryString) {
                ReverseDirection = !oldest
            };
        }

        // We need to keep record not disposed to be able to access it after the using block
        // Maybe there's a better way to do this
        EventRecord record;
        using (EventLogReader reader = CreateEventLogReader(query, filePath)) {
            if (reader != null) {
                int eventCount = 0;
                while ((record = reader.ReadEvent()) != null) {
                    // using (record) {
                    EventObject eventObject = new EventObject(record, filePath);
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
    /// Queries the log.
    /// </summary>
    /// <param name="logName">Name of the log.</param>
    /// <param name="eventIds">The event ids.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <param name="providerName">Name of the provider.</param>
    /// <param name="keywords">The keywords.</param>
    /// <param name="level">The level.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="maxEvents">The maximum events.</param>
    /// <param name="eventRecordId">The event record identifier.</param>
    /// <param name="timePeriod">The time period.</param>
    /// <returns></returns>
    public static IEnumerable<EventObject> QueryLog(string logName, List<int> eventIds = null, string machineName = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, List<long> eventRecordId = null, TimePeriod? timePeriod = null) {
        string queryString;
        if (eventRecordId != null) {
            // If eventRecordId is provided, query the log for the specific event record ID
            queryString = BuildQueryString(eventRecordId);
        } else {
            // If eventRecordId is not provided, query the log for events based on the provided parameters
            queryString = BuildQueryString(logName, eventIds, providerName, keywords, level, startTime, endTime, userId, timePeriod: timePeriod);
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
    /// <param name="logName">Name of the log.</param>
    /// <param name="eventIds">The event ids.</param>
    /// <param name="providerName">Name of the provider.</param>
    /// <param name="keywords">The keywords.</param>
    /// <param name="level">The level.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="tasks">The tasks.</param>
    /// <param name="opcodes">The opcodes.</param>
    /// <param name="timePeriod">The time period.</param>
    /// <returns></returns>
    private static string BuildQueryString(string logName, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, List<int> tasks = null, List<int> opcodes = null, TimePeriod? timePeriod = null) {
        TimeSpan? lastPeriod = null;
        if (timePeriod.HasValue) {
            var times = TimeHelper.GetTimePeriod(timePeriod.Value);
            startTime = times.StartTime;
            endTime = times.EndTime;
            lastPeriod = times.LastPeriod;
            _logger.WriteVerbose("Time period: " + timePeriod + ", time start: " + startTime + ", time end: " + endTime, " lastPeriod: " + lastPeriod);
        }

        StringBuilder queryString = new StringBuilder($"<QueryList><Query Id='0' Path='{logName}'><Select Path='{logName}'>*[System[");

        // Add event IDs to the query
        if (eventIds != null && eventIds.Any()) {
            AddCondition(queryString, "(" + string.Join(" or ", eventIds.Select(id => $"EventID={id}")) + ")");
        }

        // Add provider name to the query
        if (!string.IsNullOrEmpty(providerName)) {
            AddCondition(queryString, $"Provider[@Name='{providerName}']");
        }

        // Add keywords to the query
        if (keywords.HasValue) {
            AddCondition(queryString, $"Keywords={(long)keywords.Value}");
        }

        // Add level to the query
        if (level.HasValue) {
            AddCondition(queryString, $"Level={(int)level.Value}");
        }

        // Add tasks to the query
        if (tasks != null && tasks.Any()) {
            AddCondition(queryString, "(" + string.Join(" or ", tasks.Select(task => $"Task={task}")) + ")");
        }

        // Add opcodes to the query
        if (opcodes != null && opcodes.Any()) {
            AddCondition(queryString, "(" + string.Join(" or ", opcodes.Select(opcode => $"Opcode={opcode}")) + ")");
        }

        if (lastPeriod != null) {
            AddCondition(queryString, $"TimeCreated[timediff(@SystemTime) &lt;= {lastPeriod.Value.TotalMilliseconds}]");
        } else {
            // Add time range to the query
            if (startTime.HasValue && endTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime&gt;='{startTime.Value:s}Z' and @SystemTime&lt;='{endTime.Value:s}Z']");
            } else if (startTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime&gt;='{startTime.Value:s}Z']");
            } else if (endTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime&lt;='{endTime.Value:s}Z']");
            }
        }

        // Add user ID to the query
        if (!string.IsNullOrEmpty(userId)) {
            AddCondition(queryString, $"Security[@UserID='{userId}']");
        }

        // Check if any conditions were added to the query
        if (queryString.ToString().EndsWith("[System[")) {
            // If no conditions were added, return a query that selects all events
            queryString.Append("*");
        }

        queryString.Append("]]</Select></Query></QueryList>");

        return queryString.ToString();
    }

    private static void AddCondition(StringBuilder queryString, string condition) {
        if (!queryString.ToString().EndsWith("[System[")) {
            queryString.Append(" and ");
        }
        queryString.Append(condition);
    }

    public static IEnumerable<EventObject> QueryLogsParallel(string logName, List<int> eventIds = null, List<string> machineNames = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, int maxEvents = 0, int maxThreads = 8, List<long> eventRecordId = null, TimePeriod? timePeriod = null) {
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string> { null };
            _logger.WriteVerbose("No machine names provided, querying the local machine.");
        } else {
            _logger.WriteVerbose("Machine names provided. Creating tasks for each machine on the list: " + string.Join(", ", machineNames));
        }
        var semaphore = new SemaphoreSlim(maxThreads);
        var results = new BlockingCollection<EventObject>();

        var tasks = new List<Task>();
        foreach (var machineName in machineNames) {
            if (eventIds != null) {
                var eventIdsChunks = eventIds.Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / 22)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var chunk in eventIdsChunks) {
                    tasks.Add(CreateTask(machineName, logName, chunk, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, timePeriod: timePeriod));
                }
            } else if (eventRecordId != null) {
                var eventRecordIdChunks = eventRecordId.Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / 22)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var chunk in eventRecordIdChunks) {
                    tasks.Add(CreateTask(machineName, logName, null, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, chunk, timePeriod: timePeriod));
                }
            } else {
                // event ids are null, so we don't need to chunk them
                tasks.Add(CreateTask(machineName, logName, eventIds, providerName, keywords, level, startTime, endTime, userId, maxEvents, semaphore, results, timePeriod: timePeriod));
            }
        }

        Task.Factory.StartNew(() => {
            Task.WaitAll(tasks.ToArray());
            results.CompleteAdding();
        });

        return results.GetConsumingEnumerable();
    }

    private static Task CreateTask(string machineName, string logName, List<int> eventIds, string providerName, Keywords? keywords, Level? level, DateTime? startTime, DateTime? endTime, string userId, int maxEvents, SemaphoreSlim semaphore, BlockingCollection<EventObject> results, List<long> eventRecordId = null, TimePeriod? timePeriod = null) {
        return Task.Run(async () => {
            _logger.WriteVerbose($"Querying log on machine: {machineName}, logName: {logName}, event ids: " + string.Join(", ", eventIds ?? new List<int>()));
            await semaphore.WaitAsync();
            try {
                var queryResults = QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod);
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