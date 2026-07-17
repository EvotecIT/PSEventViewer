using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
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
    /// Creates an event log reader within the caller-provided native-operation budget.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <param name="constructorTimeoutMs">Maximum time allowed for native reader construction. Zero disables the constructor timeout.</param>
    /// <returns>Initialized <see cref="EventLogReader"/>.</returns>
    private static EventLogReader CreateEventLogReader(EventLogQuery query, string? machineName, int constructorTimeoutMs = 0) {
        if (query == null) {
            throw new ArgumentNullException(nameof(query));
        }

        if (constructorTimeoutMs <= 0) {
            return new EventLogReader(query);
        }

        string target = string.IsNullOrWhiteSpace(machineName) ? "the local computer" : $"'{machineName}'";
        return ExecuteWithTimeout(
            () => new EventLogReader(query),
            constructorTimeoutMs,
            $"Timed out creating an Event Log reader for {target} after {constructorTimeoutMs} ms.",
            static reader => reader.Dispose());
    }

    private static T ExecuteWithTimeout<T>(Func<T> operation, int timeoutMs, string timeoutMessage, Action<T>? lateResultCleanup = null) {
        if (timeoutMs <= 0) {
            return operation();
        }

        Task<T> task = Task.Run(operation);
        Task completed = Task.WhenAny(task, Task.Delay(timeoutMs)).GetAwaiter().GetResult();
        if (completed == task || task.IsCompleted) {
            return task.GetAwaiter().GetResult();
        }

        _ = task.ContinueWith(
            completedTask => {
                if (completedTask.Status == TaskStatus.RanToCompletion) {
                    try {
                        lateResultCleanup?.Invoke(completedTask.Result);
                    } catch (Exception ex) {
                        _logger.WriteVerbose($"Late native-operation cleanup failed: {ex.Message}");
                    }
                } else if (completedTask.IsFaulted) {
                    _ = completedTask.Exception;
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        throw new TimeoutException(timeoutMessage);
    }

    private static EventRecord? ReadEventWithTimeout(EventLogReader reader, int timeoutMs, string operation) {
        if (timeoutMs <= 0) {
            return reader.ReadEvent();
        }

        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var stopwatch = Stopwatch.StartNew();
        EventRecord? record = reader.ReadEvent(timeout);
        if (record == null && stopwatch.Elapsed.Ticks >= timeout.Ticks * 9 / 10) {
            throw new TimeoutException($"{operation} exceeded its {timeoutMs} ms native read window.");
        }
        return record;
    }

    /// <summary>
    /// Core enumerable that streams events from a log using the supplied filters.
    /// </summary>
    /// <param name="logName">Log name to query.</param>
    /// <param name="eventIds">Optional list of specific event IDs to filter for.</param>
    /// <param name="machineName">Remote computer name; <c>null</c> targets local.</param>
    /// <param name="providerName">Optional name of the event provider to filter by.</param>
    /// <param name="keywords">Optional keywords to filter events by.</param>
    /// <param name="level">Optional event level to filter by (e.g., Error, Warning, Information).</param>
    /// <param name="startTime">Optional start time to filter events from.</param>
    /// <param name="endTime">Optional end time to filter events until.</param>
    /// <param name="userId">Optional user ID to filter events by.</param>
    /// <param name="maxEvents">Maximum number of events to return.</param>
    /// <param name="eventRecordId">Specific record IDs to include.</param>
    /// <param name="timePeriod">Relative time period filter.</param>
    /// <param name="cancellationToken">Cancellation token used while streaming events.</param>
    /// <param name="sessionTimeoutMs">Timeout for establishing sessions and reading events.</param>
    /// <param name="readMode">Amount of provider data to materialize for each event.</param>
    private static IEnumerable<EventObject> QueryLogEnumerable(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full) {
        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        if (eventIds != null && eventIds.Any(id => id <= 0)) {
            throw new ArgumentException("Event IDs must be positive.", nameof(eventIds));
        }

        if (eventRecordId != null && eventRecordId.Any(id => id <= 0)) {
            throw new ArgumentException("Event record IDs must be positive.", nameof(eventRecordId));
        }

        string queryString = BuildQueryString(
            logName,
            eventIds,
            providerName,
            keywords,
            level,
            startTime,
            endTime,
            userId ?? string.Empty,
            timePeriod: timePeriod,
            eventRecordIds: eventRecordId);

        _logger.WriteVerbose($"Querying log '{logName}' on '{machineName} with query: {queryString}");

        EventLogQuery query = new EventLogQuery(logName, PathType.LogName, queryString)
        {
            ReverseDirection = true,
            TolerateQueryErrors = false
        };
        int effectiveTimeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        foreach (var ev in QueryLogFromQuery(query, machineName, action: "QueryLog", logName, maxEvents, cancellationToken, effectiveTimeout, readMode)) {
            yield return ev;
        }
    }

    /// <summary>
    /// Queries a Windows event log by name using a caller-provided XPath expression.
    /// </summary>
    /// <remarks>
    /// This exists so callers (tools/hosts) can pass custom XPath without re-implementing the
    /// session warm-up / reader timeout / streaming logic.
    /// </remarks>
    /// <param name="logName">Log name (e.g., Security, System).</param>
    /// <param name="xpath">XPath filter (default: '*').</param>
    /// <param name="machineName">Remote computer name (null = local).</param>
    /// <param name="maxEvents">Maximum events to return (0 = all).</param>
    /// <param name="oldest">If true, read from oldest to newest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sessionTimeoutMs">Session open/read timeout (ms); null uses defaults.</param>
    /// <param name="readMode">Amount of provider data to materialize for each event.</param>
    public static IEnumerable<EventObject> QueryLogXPath(string logName, string? xpath = null, string? machineName = null, int maxEvents = 0, bool oldest = false, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full) {
        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        if (string.IsNullOrWhiteSpace(xpath)) {
            xpath = "*";
        }

        var query = new EventLogQuery(logName, PathType.LogName, xpath) {
            ReverseDirection = !oldest,
            TolerateQueryErrors = false
        };

        int effectiveTimeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        foreach (var ev in QueryLogFromQuery(query, machineName, action: "QueryLogXPath", logName, maxEvents, cancellationToken, effectiveTimeout, readMode)) {
            yield return ev;
        }
    }

    private static IEnumerable<EventObject> QueryLogFromQuery(EventLogQuery query, string? machineName, string action, string logName, int maxEvents, CancellationToken cancellationToken, int effectiveTimeout, EventReadMode readMode) {
        EventLogSessionOpenResult? sessionResult = null;
        if (!string.IsNullOrEmpty(machineName)) {
            int sessionBudget = effectiveTimeout > 0 ? effectiveTimeout : Settings.SessionTimeoutMs;
            sessionResult = CreateSessionResult(machineName, action, logName, sessionBudget);
            if (!sessionResult.Success || sessionResult.Session == null) {
                ThrowSessionFailure(sessionResult);
            }
            query.Session = sessionResult.Session;
        }

        string queriedMachine = string.IsNullOrEmpty(machineName) ? GetFQDN() : machineName!;
        try {
            using (var reader = CreateEventLogReader(query, machineName, effectiveTimeout)) {
                int eventCount = 0;
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    EventRecord? next = ReadEventWithTimeout(reader, effectiveTimeout, $"Reading '{logName}' on '{queriedMachine}'");
                    if (next == null) {
                        break;
                    }

                    EventObject eventObject = new EventObject(next, queriedMachine, readMode);
                    yield return eventObject;
                    eventCount++;
                    if (maxEvents > 0 && eventCount >= maxEvents) {
                        break;
                    }
                }
            }
        } finally {
            sessionResult?.Dispose();
        }
    }

    private static void ThrowSessionFailure(EventLogSessionOpenResult sessionResult) {
        string message = string.IsNullOrWhiteSpace(sessionResult.ErrorMessage)
            ? $"Event Log session to '{sessionResult.TargetHost}' could not be opened."
            : sessionResult.ErrorMessage;

        if (sessionResult.Status == EventLogSessionOpenStatus.AccessDenied) {
            throw new UnauthorizedAccessException(message);
        }
        if (sessionResult.Status == EventLogSessionOpenStatus.Timeout) {
            throw new TimeoutException(message);
        }
        throw new EventLogSessionException(sessionResult, message);
    }

    private static void ValidateQueryArguments(string logName, int maxEvents, int? sessionTimeoutMs) {
        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException("Log name cannot be null or empty.", nameof(logName));
        }
        if (maxEvents < 0) {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Maximum events must be greater than or equal to zero.");
        }
        if (sessionTimeoutMs < 0) {
            throw new ArgumentOutOfRangeException(nameof(sessionTimeoutMs), "Session timeout must be greater than or equal to zero when provided.");
        }
    }

    /// <summary>
    /// Queries a Windows event log by name with optional filters.
    /// </summary>
    /// <param name="logName">Log name (e.g., Security, System).</param>
    /// <param name="eventIds">Event IDs to include.</param>
    /// <param name="machineName">Remote computer name (null = local).</param>
    /// <param name="providerName">Provider name to include.</param>
    /// <param name="keywords">Keyword mask to include.</param>
    /// <param name="level">Event level to include.</param>
    /// <param name="startTime">Earliest event time.</param>
    /// <param name="endTime">Latest event time.</param>
    /// <param name="userId">User SID to include.</param>
    /// <param name="maxEvents">Maximum events to return (0 = all).</param>
    /// <param name="eventRecordId">Specific record IDs to include.</param>
    /// <param name="timePeriod">Relative time window (overrides start/end).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sessionTimeoutMs">Session open/read timeout (ms); null uses defaults.</param>
    /// <param name="readMode">Amount of provider data to materialize for each event.</param>
    /// <returns>Enumerable collection of matching events.</returns>
    public static IEnumerable<EventObject> QueryLog(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full) {
        return QueryLogsSequential(logName, eventIds, new List<string?> { machineName }, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs, readMode);
    }

    /// <summary>
    /// Queries a Windows event log by known-log enum with optional filters.
    /// </summary>
    public static IEnumerable<EventObject> QueryLog(KnownLog logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full) {
        return QueryLog(LogNameToString(logName), eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs, readMode);
    }

    /// <summary>
    /// Asynchronously queries a Windows event log by name with optional filters.
    /// </summary>
    /// <remarks>This compatibility API materializes every result. Prefer the streaming APIs for large logs.</remarks>
    [Obsolete("Use QueryLog for synchronous streaming or QueryLogsParallel for bounded asynchronous streaming.")]
    public static async Task<IEnumerable<EventObject>> QueryLogAsync(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full) {
        int timeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        return await Task.Run(() => QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, timeout, readMode).ToList().AsEnumerable(), cancellationToken);
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
    /// <param name="eventRecordIds">Optional event record identifiers combined with the other filters.</param>
    /// <returns>XML query string.</returns>
    private static string BuildQueryString(string logName, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, List<int>? tasks = null, List<int>? opcodes = null, TimePeriod? timePeriod = null, List<long>? eventRecordIds = null) {
        TimeSpan? lastPeriod = null;
        if (timePeriod.HasValue) {
            var times = TimeHelper.GetTimePeriod(timePeriod.Value);
            startTime = times.StartTime;
            endTime = times.EndTime;
            lastPeriod = times.LastPeriod;
            _logger.WriteVerbose($"Time period: {timePeriod}, time start: {startTime}, time end: {endTime}, lastPeriod: {lastPeriod}");
        }

        string escapedLogName = EscapeXmlValue(logName);
        StringBuilder queryString = new StringBuilder($"<QueryList><Query Id='0' Path='{escapedLogName}'><Select Path='{escapedLogName}'>*[System[");

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
            string literal = FormatXmlEncodedXPathStringLiteral(providerName!, nameof(providerName));
            AddCondition(queryString, $"Provider[@Name={literal}]");
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
                AddCondition(queryString, $"TimeCreated[@SystemTime&gt;='{FormatEventTimeUtc(startTime.Value)}' and @SystemTime&lt;='{FormatEventTimeUtc(endTime.Value)}']");
            } else if (startTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime&gt;='{FormatEventTimeUtc(startTime.Value)}']");
            } else if (endTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime&lt;='{FormatEventTimeUtc(endTime.Value)}']");
            }
        }

        // Add user ID to the query
        if (!string.IsNullOrEmpty(userId)) {
            if (!EventStructuredQueryFilterService.TryResolveUserId(userId!, out string? normalizedUserId)) {
                throw new ArgumentException("User identifier must be a valid SID or resolvable account name.", nameof(userId));
            }
            string literal = FormatXmlEncodedXPathStringLiteral(normalizedUserId!, nameof(userId));
            AddCondition(queryString, $"Security[@UserID={literal}]");
        }

        if (eventRecordIds != null) {
            var validRecordIds = eventRecordIds.Where(id => id > 0).Distinct().ToList();
            if (validRecordIds.Any()) {
                var recordConditions = validRecordIds.Select(id => $"(EventRecordID={id})");
                AddCondition(queryString, $"({string.Join(" or ", recordConditions)})");
            }
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

    private static string FormatEventTimeUtc(DateTime value) {
        DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
    }

    private static string LogNameToString(KnownLog logName) => logName switch {
        KnownLog.DirectoryService => "Directory Service",
        KnownLog.DNSServer => "DNS Server",
        KnownLog.WindowsPowerShell => "Windows PowerShell",
        _ => logName.ToString()
    };

}
