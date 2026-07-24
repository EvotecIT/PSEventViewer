using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using EventViewerX.Native;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>Maximum number of abandonment-safe native calls that may own dedicated timeout threads.</summary>
    internal const int MaximumConcurrentTimedNativeOperations =
        BoundedNativeOperation.MaximumConcurrentOperations;

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

    /// <summary>Runs a native operation within the shared dedicated-thread and timeout budget.</summary>
    internal static T ExecuteWithTimeout<T>(Func<T> operation, int timeoutMs, string timeoutMessage, Action<T>? lateResultCleanup = null) {
        return BoundedNativeOperation.Execute(
            operation,
            timeoutMs,
            timeoutMessage,
            lateResultCleanup);
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

    private static CancellationTokenRegistration RegisterReaderCancellation(EventLogReader reader, CancellationToken cancellationToken) {
        return cancellationToken.Register(
            static state => {
                try {
                    ((EventLogReader)state!).CancelReading();
                } catch (ObjectDisposedException) {
                } catch (EventLogException) {
                } catch (InvalidOperationException) {
                }
            },
            reader);
    }

    private static EventRecord? ReadEventWithCancellation(
        EventLogReader reader,
        int timeoutMs,
        string operation,
        CancellationToken cancellationToken) {

        cancellationToken.ThrowIfCancellationRequested();
        try {
            EventRecord? record = ReadEventWithTimeout(reader, timeoutMs, operation);
            cancellationToken.ThrowIfCancellationRequested();
            return record;
        } catch (EventLogException ex) when (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException("Event Log reading was cancelled.", ex, cancellationToken);
        } catch (InvalidOperationException ex) when (cancellationToken.IsCancellationRequested) {
            throw new OperationCanceledException("Event Log reading was cancelled.", ex, cancellationToken);
        }
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
    /// <param name="minimumEventRecordIdExclusive">Optional native record-ID lower bound.</param>
    /// <param name="maximumEventRecordIdExclusive">Optional native record-ID upper bound.</param>
    /// <param name="oldest">Whether to enumerate matching records from oldest to newest.</param>
    /// <param name="messageCulture">Culture requested for provider messages and display names.</param>
    private static IEnumerable<EventObject> QueryLogEnumerable(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, long? maximumEventRecordIdExclusive = null, bool oldest = false, CultureInfo? messageCulture = null) {
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
            eventRecordIds: eventRecordId,
            minimumEventRecordIdExclusive: minimumEventRecordIdExclusive,
            maximumEventRecordIdExclusive: maximumEventRecordIdExclusive);

        _logger.WriteVerbose($"Querying log '{logName}' on '{machineName} with query: {queryString}");

        int effectiveTimeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        foreach (var ev in QueryLogFromQuery(
                     queryString,
                     oldest,
                     machineName,
                     logName,
                     maxEvents,
                     cancellationToken,
                     effectiveTimeout,
                     readMode,
                     messageCulture)) {
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
    /// <param name="messageCulture">Culture requested for provider messages and display names.</param>
    public static IEnumerable<EventObject> QueryLogXPath(string logName, string? xpath = null, string? machineName = null, int maxEvents = 0, bool oldest = false, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full, CultureInfo? messageCulture = null) {
        ValidateQueryArguments(logName, maxEvents, sessionTimeoutMs);
        string effectiveXPath = string.IsNullOrWhiteSpace(xpath) ? "*" : xpath!;

        int effectiveTimeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        foreach (var ev in QueryLogFromQuery(
                     effectiveXPath,
                     oldest,
                     machineName,
                     logName,
                     maxEvents,
                     cancellationToken,
                     effectiveTimeout,
                     readMode,
                     messageCulture)) {
            yield return ev;
        }
    }

    private static IEnumerable<EventObject> QueryLogFromQuery(
        string xpath,
        bool oldest,
        string? machineName,
        string logName,
        int maxEvents,
        CancellationToken cancellationToken,
        int effectiveTimeout,
        EventReadMode readMode,
        CultureInfo? messageCulture) {

        var query = new EventLogChannelQuery(logName) {
            XPath = xpath,
            Oldest = oldest,
            MachineName = machineName,
            MaxEvents = maxEvents,
            ReadMode = readMode,
            MessageCulture = messageCulture,
            RemoteConnectionTimeoutMilliseconds = effectiveTimeout > 0
                ? effectiveTimeout
                : Settings.SessionTimeoutMs,
            RemoteReadTimeoutMilliseconds = effectiveTimeout,
            RpcEndpointPort = Settings.RpcProbePort
        };
        foreach (EventObject eventObject in EventLogEngine.ReadChannel(
                     query,
                     cancellationToken)) {
            yield return eventObject;
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
    /// <param name="minimumEventRecordIdExclusive">Optional native record-ID lower bound.</param>
    /// <param name="oldest">Whether to enumerate matching records from oldest to newest.</param>
    /// <param name="messageCulture">Culture requested for provider messages and display names.</param>
    /// <returns>Enumerable collection of matching events.</returns>
    public static IEnumerable<EventObject> QueryLog(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, bool oldest = false, CultureInfo? messageCulture = null) {
        Func<string?, long?>? resolver = minimumEventRecordIdExclusive.HasValue ? _ => minimumEventRecordIdExclusive : null;
        return QueryLogsSequential(logName, eventIds, new List<string?> { machineName }, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs, readMode, resolver, oldest: oldest, messageCulture: messageCulture);
    }

    /// <summary>
    /// Queries a Windows event log by known-log enum with optional filters.
    /// </summary>
    public static IEnumerable<EventObject> QueryLog(KnownLog logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, bool oldest = false, CultureInfo? messageCulture = null) {
        return QueryLog(LogNameToString(logName), eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs, readMode, minimumEventRecordIdExclusive, oldest, messageCulture);
    }

    /// <summary>
    /// Asynchronously queries a Windows event log by name with optional filters.
    /// </summary>
    /// <remarks>This compatibility API materializes every result. Prefer the streaming APIs for large logs.</remarks>
    [Obsolete("Use QueryLog for synchronous streaming or QueryLogsParallel for bounded asynchronous streaming.")]
    public static async Task<IEnumerable<EventObject>> QueryLogAsync(string logName, List<int>? eventIds = null, string? machineName = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, int maxEvents = 0, List<long>? eventRecordId = null, TimePeriod? timePeriod = null, CancellationToken cancellationToken = default, int? sessionTimeoutMs = null, EventReadMode readMode = EventReadMode.Full, long? minimumEventRecordIdExclusive = null, bool oldest = false, CultureInfo? messageCulture = null) {
        int timeout = sessionTimeoutMs ?? Settings.QuerySessionTimeoutMs;
        return await Task.Run(() => QueryLog(logName, eventIds, machineName, providerName, keywords, level, startTime, endTime, userId, maxEvents, eventRecordId, timePeriod, cancellationToken, timeout, readMode, minimumEventRecordIdExclusive, oldest, messageCulture).ToList().AsEnumerable(), cancellationToken);
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
    /// <param name="minimumEventRecordIdExclusive">Optional native record-ID lower bound.</param>
    /// <param name="maximumEventRecordIdExclusive">Optional native record-ID upper bound.</param>
    /// <returns>XML query string.</returns>
    private static string BuildQueryString(string logName, List<int>? eventIds = null, string? providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string? userId = null, List<int>? tasks = null, List<int>? opcodes = null, TimePeriod? timePeriod = null, List<long>? eventRecordIds = null, long? minimumEventRecordIdExclusive = null, long? maximumEventRecordIdExclusive = null) {
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

        if (minimumEventRecordIdExclusive.HasValue) {
            if (minimumEventRecordIdExclusive.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(minimumEventRecordIdExclusive), "Minimum event record ID must be greater than or equal to zero.");
            }
            AddCondition(queryString, $"EventRecordID&gt;{minimumEventRecordIdExclusive.Value.ToString(CultureInfo.InvariantCulture)}");
        }
        if (maximumEventRecordIdExclusive.HasValue) {
            if (maximumEventRecordIdExclusive.Value < 0) {
                throw new ArgumentOutOfRangeException(nameof(maximumEventRecordIdExclusive), "Maximum event record ID must be greater than or equal to zero.");
            }
            AddCondition(queryString, $"EventRecordID&lt;{maximumEventRecordIdExclusive.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        // Check if any conditions were added to the query
        if (queryString.ToString().EndsWith("[System[", StringComparison.Ordinal)) {
            // If no conditions were added, return a query that selects all events
            queryString.Append("*");
        }

        queryString.Append("]]</Select></Query></QueryList>");

        return queryString.ToString();
    }

    private static void AddCondition(StringBuilder queryString, string condition) {
        if (!queryString.ToString().EndsWith("[System[", StringComparison.Ordinal)) {
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
