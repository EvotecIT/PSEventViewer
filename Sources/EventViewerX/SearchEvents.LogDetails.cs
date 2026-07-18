using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventViewerX;

/// <summary>
/// Helper methods for retrieving event log configuration details.
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Returns details for a single log without enumerating all logs.
    /// </summary>
    public static EventLogDetails? GetLogDetails(string logName, string? machineName = null, int timeoutMs = 3000, bool includeEventTimes = false) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        return GetLogDetailsResult(logName, machineName, timeoutMs, includeEventTimes).Details;
    }

    /// <summary>
    /// Returns details for a single log with diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, string? machineName = null, int timeoutMs = 3000, bool includeEventTimes = false) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try {
            return SafeGetResult(logName, machineName, timeoutMs, includeEventTimes);
        } catch (Exception ex) {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) to avoid repeated handshakes per host.
    /// </summary>
    public static EventLogDetails? GetLogDetails(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null, bool includeEventTimes = false)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        return GetLogDetailsResult(logName, session, timeoutMs, machineName, includeEventTimes).Details;
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) and returns diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null, bool includeEventTimes = false)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try
        {
            return SafeGetResult(logName, session, timeoutMs, machineName, includeEventTimes);
        }
        catch (Exception ex)
        {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
    }

    private static EventLogDetailsResult SafeGetResult(string logName, string? machineName, int timeoutMs, bool includeEventTimes) {
        EventLogSessionOpenResult? sessionResult = null;
        try {
            sessionResult = CreateSessionResult(machineName, "LogDetails", logName, timeoutMs);
            if (!sessionResult.Success || sessionResult.Session == null) {
                return Failure(
                    logName,
                    machineName,
                    MapSessionFailureStatus(sessionResult.Status),
                    string.IsNullOrWhiteSpace(sessionResult.ErrorMessage)
                        ? "Event log session could not be opened. Check host reachability, RPC/firewall access, Remote Event Log Management, and permissions."
                        : sessionResult.ErrorMessage,
                    timeoutMs,
                    string.IsNullOrWhiteSpace(sessionResult.ErrorType)
                        ? sessionResult.Status.ToString()
                        : sessionResult.ErrorType);
            }

            return SafeGetResult(logName, sessionResult.Session, timeoutMs, machineName, includeEventTimes);
        }
        finally {
            sessionResult?.Dispose();
        }
    }

    private static EventLogDetailsResult SafeGetResult(string logName, EventLogSession session, int timeoutMs, string? machineName, bool includeEventTimes)
    {
        EventLogConfiguration? logConfig = null;
        try
        {
            EventLogInformation? logInfoObj = null;
            string hostName = machineName ?? GetFQDN();

            try {
                logConfig = ExecuteWithTimeout(
                    () => new EventLogConfiguration(logName, session),
                    timeoutMs,
                    $"Timed out reading configuration for '{logName}' on '{hostName}' after {timeoutMs} ms.",
                    static configuration => configuration.Dispose());
            } catch (TimeoutException ex) {
                _logger.WriteWarning(ex.Message);
                return Failure(logName, machineName, EventLogDetailsStatus.Timeout, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (EventLogException ex) {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {hostName}: {ex.Message}");
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (Exception ex) {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {hostName}: {ex.Message}");
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            }

            try {
                logInfoObj = ExecuteWithTimeout(
                    () => session.GetLogInformation(logName, PathType.LogName),
                    timeoutMs,
                    $"Timed out reading runtime information for '{logName}' on '{hostName}' after {timeoutMs} ms.");
            } catch (TimeoutException ex) {
                _logger.WriteWarning(ex.Message);
                return Failure(logName, machineName, EventLogDetailsStatus.Timeout, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (Exception ex) {
                _logger.WriteVerbose($"Couldn't get log information for {logName} on {hostName}: {ex.Message}");
            }

            var details = new EventLogDetails(_logger, hostName, logConfig, logInfoObj);
            if (includeEventTimes) {
                ReadEventTimes(logName, session, timeoutMs, details);
            }
            if (logInfoObj == null)
            {
                return new EventLogDetailsResult
                {
                    LogName = logName,
                    MachineName = hostName,
                    Status = EventLogDetailsStatus.LogInformationUnavailable,
                    Details = details,
                    ErrorMessage = "Event log configuration was collected, but runtime log information was unavailable.",
                    TimeoutMs = timeoutMs
                };
            }

            return new EventLogDetailsResult
            {
                LogName = logName,
                MachineName = hostName,
                Status = EventLogDetailsStatus.Success,
                Details = details,
                TimeoutMs = timeoutMs
            };
        }
        catch (Exception ex)
        {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
        finally
        {
            logConfig?.Dispose();
        }
    }

    private static void ReadEventTimes(string logName, EventLogSession session, int timeoutMs, EventLogDetails details) {
        try {
            var oldestQuery = new EventLogQuery(logName, PathType.LogName) { Session = session };
            using (EventLogReader oldestReader = CreateEventLogReader(oldestQuery, details.MachineName, timeoutMs)) {
                using EventRecord? oldest = ReadEventWithTimeout(oldestReader, timeoutMs, $"Reading the oldest event from '{logName}' on '{details.MachineName}'");
                details.OldestEvent = oldest?.TimeCreated;
            }

            var newestQuery = new EventLogQuery(logName, PathType.LogName) {
                Session = session,
                ReverseDirection = true
            };
            using (EventLogReader newestReader = CreateEventLogReader(newestQuery, details.MachineName, timeoutMs)) {
                using EventRecord? newest = ReadEventWithTimeout(newestReader, timeoutMs, $"Reading the newest event from '{logName}' on '{details.MachineName}'");
                details.NewestEvent = newest?.TimeCreated;
            }
        } catch (Exception ex) {
            _logger.WriteVerbose($"Couldn't read oldest/newest event times for {logName} on {details.MachineName}: {ex.Message}");
        }
    }

    private static EventLogDetailsResult Failure(
        string logName,
        string? machineName,
        EventLogDetailsStatus status,
        string errorMessage,
        int timeoutMs,
        string? errorType = null) =>
        new()
        {
            LogName = logName,
            MachineName = machineName,
            Status = status,
            ErrorMessage = errorMessage,
            ErrorType = errorType ?? string.Empty,
            TimeoutMs = timeoutMs
        };

    /// <summary>
    /// Enumerates event logs and returns a result for every requested or matched channel, including failures.
    /// </summary>
    /// <param name="listLog">Optional log names or wildcard patterns.</param>
    /// <param name="machineName">Remote machine name; <c>null</c> targets the local computer.</param>
    /// <param name="timeoutMs">Per-operation timeout in milliseconds for session, configuration, information, enumeration, and optional event-time reads.</param>
    /// <param name="includeEventTimes">When true, reads the oldest and newest event timestamps for each log.</param>
    /// <returns>Diagnostic result for every requested or matched channel.</returns>
    public static IEnumerable<EventLogDetailsResult> DisplayEventLogResults(string[]? listLog = null, string? machineName = null, int timeoutMs = 3000, bool includeEventTimes = false) {
        if (timeoutMs <= 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive.");
        }

        string hostName = string.IsNullOrWhiteSpace(machineName) ? GetFQDN() : machineName!;
        EventLogSessionOpenResult sessionResult = CreateSessionResult(machineName, "DisplayEventLogResults", "*", timeoutMs);
        if (!sessionResult.Success || sessionResult.Session == null) {
            yield return Failure("*", hostName, MapSessionFailureStatus(sessionResult.Status), sessionResult.ErrorMessage, timeoutMs, sessionResult.ErrorType);
            sessionResult.Dispose();
            yield break;
        }

        EventLogSession activeSession = sessionResult.Session;
        try {
            bool hasOnlyExactNames = listLog != null && listLog.Length > 0 &&
                                     listLog.All(name => name.IndexOf('*') < 0 && name.IndexOf('?') < 0);
            if (hasOnlyExactNames) {
                foreach (string exactName in listLog!.Distinct(StringComparer.OrdinalIgnoreCase)) {
                    yield return SafeGetResult(exactName, activeSession, timeoutMs, hostName, includeEventTimes);
                }
                yield break;
            }

            string[] logNames;
            EventLogDetailsResult? enumerationFailure = null;
            try {
                logNames = ExecuteWithTimeout(
                    () => activeSession.GetLogNames().ToArray(),
                    timeoutMs,
                    $"Timed out enumerating event logs on '{hostName}' after {timeoutMs} ms.");
            } catch (TimeoutException ex) {
                logNames = Array.Empty<string>();
                enumerationFailure = Failure("*", hostName, EventLogDetailsStatus.Timeout, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (Exception ex) {
                logNames = Array.Empty<string>();
                enumerationFailure = Failure("*", hostName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
            }
            if (enumerationFailure != null) {
                yield return enumerationFailure;
                yield break;
            }

            Regex[]? filters = listLog?.Select(pattern => new Regex(
                "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();
            foreach (string logName in logNames) {
                if (filters != null && !filters.Any(filter => filter.IsMatch(logName))) {
                    continue;
                }
                yield return SafeGetResult(logName, activeSession, timeoutMs, hostName, includeEventTimes);
            }
        } finally {
            sessionResult.Dispose();
        }
    }

    private static EventLogDetailsStatus MapSessionFailureStatus(EventLogSessionOpenStatus status) {
        return status switch {
            EventLogSessionOpenStatus.AccessDenied => EventLogDetailsStatus.AccessDenied,
            EventLogSessionOpenStatus.Timeout => EventLogDetailsStatus.Timeout,
            EventLogSessionOpenStatus.NegativeCache => EventLogDetailsStatus.HostUnavailable,
            EventLogSessionOpenStatus.RpcUnavailable => EventLogDetailsStatus.HostUnavailable,
            _ => EventLogDetailsStatus.SessionUnavailable
        };
    }

    /// <summary>
    /// Enumerates event logs on the specified machine and returns available configuration details.
    /// </summary>
    /// <param name="listLog">Optional list of log name patterns (supports * and ?) to include.</param>
    /// <param name="machineName">Remote machine name; <c>null</c> targets the local computer.</param>
    /// <returns>Available details for matching channels.</returns>
    public static IEnumerable<EventLogDetails> DisplayEventLogs(string[]? listLog = null, string? machineName = null) {
        foreach (EventLogDetailsResult result in DisplayEventLogResults(listLog, machineName, DefaultSessionTimeoutMs)) {
            if (result.Details != null) {
                yield return result.Details;
            } else {
                _logger.WriteWarning($"Couldn't read event log details for {result.LogName} on {result.MachineName}: {result.ErrorMessage}");
            }
        }
    }

    /// <summary>
    /// Retrieves typed event log detail results from multiple machines through one bounded parallel pipeline.
    /// </summary>
    public static IEnumerable<EventLogDetailsResult> DisplayEventLogResultsParallel(
        string[]? listLog = null,
        List<string?>? machineNames = null,
        int maxDegreeOfParallelism = 8,
        int timeoutMs = 3000,
        bool includeEventTimes = false,
        CancellationToken cancellationToken = default) {

        if (maxDegreeOfParallelism <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Maximum degree of parallelism must be positive.");
        }
        if (maxDegreeOfParallelism > MaximumParallelism) {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"Maximum degree of parallelism cannot exceed {MaximumParallelism}.");
        }
        if (timeoutMs <= 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive.");
        }

        List<string?> targets = machineNames == null || machineNames.Count == 0
            ? new List<string?> { null }
            : machineNames;
        using var pipelineCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var results = new BlockingCollection<EventLogDetailsResult>(Math.Max(16, maxDegreeOfParallelism * 4));

        Task worker = Task.Factory.StartNew(() => {
            try {
                Parallel.ForEach(
                    targets,
                    new ParallelOptions {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism,
                        CancellationToken = pipelineCancellation.Token
                    },
                    machineName => {
                        foreach (EventLogDetailsResult result in DisplayEventLogResults(listLog, machineName, timeoutMs, includeEventTimes)) {
                            results.Add(result, pipelineCancellation.Token);
                        }
                    });
            } finally {
                results.CompleteAdding();
            }
        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        try {
            foreach (EventLogDetailsResult result in results.GetConsumingEnumerable(cancellationToken)) {
                yield return result;
            }

            worker.GetAwaiter().GetResult();
        } finally {
            pipelineCancellation.Cancel();
            try {
                worker.GetAwaiter().GetResult();
            } catch (OperationCanceledException) when (pipelineCancellation.IsCancellationRequested) {
            }
        }
    }

    /// <summary>
    /// Retrieves event log details from multiple machines in parallel with cancellation support.
    /// </summary>
    /// <param name="listLog">Optional list of log name patterns (supports * and ?).</param>
    /// <param name="machineNames">List of remote machines to query; <c>null</c> or empty defaults to the local host.</param>
    /// <param name="maxDegreeOfParallelism">Maximum concurrent queries.</param>
    /// <param name="cancellationToken">Cancellation token to abort enumeration.</param>
    /// <returns>An enumerable of <see cref="EventLogDetails"/> objects streamed as they are collected.</returns>
    public static IEnumerable<EventLogDetails> DisplayEventLogsParallel(string[]? listLog = null, List<string?>? machineNames = null, int maxDegreeOfParallelism = 8, CancellationToken cancellationToken = default) {
        foreach (EventLogDetailsResult result in DisplayEventLogResultsParallel(
                     listLog,
                     machineNames,
                     maxDegreeOfParallelism,
                     Settings.SessionTimeoutMs,
                     includeEventTimes: false,
                     cancellationToken)) {
            if (result.Details != null) {
                yield return result.Details;
            } else {
                _logger.WriteWarning($"Couldn't read event log details for {result.LogName} on {result.MachineName}: {result.ErrorMessage}");
            }
        }
    }
}
