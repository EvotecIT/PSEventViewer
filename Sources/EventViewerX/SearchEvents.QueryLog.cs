using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>
    /// Initialize the EventSearching class with an internal logger
    /// </summary>
    /// <param name="internalLogger">The internal logger.</param>
    public SearchEvents(InternalLogger? internalLogger = null) {
        if (internalLogger != null) {
            _logger = internalLogger;
        }
    }

    /// <summary>
    /// Create an event log reader allowing for catching errors and logging them
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <returns>Initialized <see cref="EventLogReader"/> or null when failed.</returns>
    private static EventLogReader? CreateEventLogReader(EventLogQuery query, string? machineName) {
        string targetMachine = string.IsNullOrEmpty(machineName) ? GetFQDN() : machineName!;
        if (query == null) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: Query cannot be null.");
            return null;
        }

        try {
            return new EventLogReader(query);
        } catch (EventLogException ex) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: {ex.Message}");
            return null;
        } catch (UnauthorizedAccessException ex) {
            _logger.WriteWarning($"Insufficient permissions to read the event log on {targetMachine}: {ex.Message}");
            return null!;
        } catch (Exception ex) {
            _logger.WriteWarning($"An error occurred on {targetMachine} while creating the event log reader: {ex.Message}");
            return null;
        }
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
    private static IEnumerable<EventObject> QueryLogEnumerable(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        if (eventIds != null && eventIds.Any(id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }

        if (eventRecordId != null && eventRecordId.Any(id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordId));
        }

        string queryString = eventRecordId != null
            ? BuildQueryString(eventRecordId)
            : BuildQueryString(logName, eventIds, providerName, keywords, level, startTime, endTime, userId ?? string.Empty, timePeriod: timePeriod);

        _logger.WriteVerbose($"Querying log '{logName}' on '{machineName} with query: {queryString}");

        EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString);
        if (!string.IsNullOrEmpty(machineName)) {
            query.Session = new EventLogSession(machineName);
        }

        string queriedMachine = string.IsNullOrEmpty(machineName) ? GetFQDN() : machineName!;

        EventRecord record;
        using (EventLogReader? reader = CreateEventLogReader(query, machineName)) {
            if (reader != null) {
                int eventCount = 0;
                while (!cancellationToken.IsCancellationRequested && (record = reader.ReadEvent()) != null) {
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
    /// <returns>Enumerable collection of matching events.</returns>
    public static IEnumerable<EventObject> QueryLog(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLogEnumerable(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken);
    }

    public static IEnumerable<EventObject> QueryLog(KnownLog logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return QueryLog(LogNameToString(logName), eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken);
    }

    public static async Task<IEnumerable<EventObject>> QueryLogAsync(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default) {
        return await Task.Run(() => QueryLogEnumerable(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken).ToList().AsEnumerable(), cancellationToken);
    }

    /// <summary>
    /// Build a query string for querying a log for a specific event record ID or IDs
    /// </summary>
    /// <param name="eventRecordIds">Event record identifiers.</param>
    /// <returns>XML query string.</returns>
    private static string BuildQueryString(List<long> eventRecordIds) {
        if (eventRecordIds != null) {
            var validIds = eventRecordIds.Where(id => id > 0).ToList();
            if (validIds.Any()) {
                return $"*[System[{string.Join(" or ", validIds.Select(id => $"EventRecordID={id}"))}]]";
            }
        }

        return "*";
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
    /// <returns>XML query string.</returns>
    private static string BuildQueryString(string logName, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, List<int>? tasks = null, List<int>? opcodes = null, TimePeriod? timePeriod = null) {
        TimeSpan? lastPeriod = null;
        if (timePeriod.HasValue) {
            var times = TimeHelper.GetTimePeriod(timePeriod.Value);
            startTime = times.StartTime;
            endTime = times.EndTime;
            lastPeriod = times.LastPeriod;
            _logger.WriteVerbose($"Time period: {timePeriod}, time start: {startTime}, time end: {endTime}, lastPeriod: {lastPeriod}");
        }

        StringBuilder queryString = new StringBuilder($"<QueryList><Query Id='0' Path='{logName}'><Select Path='{logName}'>*[System[");

        // Add event IDs to the query
        if (eventIds != null) {
            var validIds = eventIds.Where(id => id > 0).Distinct().ToList();
            if (validIds.Any()) {
                var idConditions = validIds.Select(id => $"(EventID={id})");
                AddCondition(queryString, $"({string.Join(" or ", idConditions)})");
            }
        }

        // Add provider name to the query
        if (!string.IsNullOrEmpty(providerName)) {
            var escaped = EscapeXPathValue(providerName!);
            AddCondition(queryString, $"Provider[@Name='{escaped}']");
        }

        // Add keywords to the query
        if (keywords.HasValue) {
            AddCondition(queryString, $"band(Keywords,{(long)keywords.Value})");
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

    private static string LogNameToString(KnownLog logName) => logName switch {
        KnownLog.DirectoryService => "Directory Service",
        KnownLog.DNSServer => "DNS Server",
        KnownLog.WindowsPowerShell => "Windows PowerShell",
        _ => logName.ToString()
    };

}
