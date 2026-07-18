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

        EventLogDetailsResult result = GetLogDetailsResult(logName, machineName, timeoutMs, includeEventTimes);
        WriteLogDetailsWarningIfNeeded(result);
        return result.Details;
    }

    /// <summary>
    /// Returns details for a single log with diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, string? machineName = null, int timeoutMs = 3000, bool includeEventTimes = false) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));
        if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive.");

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

        EventLogDetailsResult result = GetLogDetailsResult(logName, session, timeoutMs, machineName, includeEventTimes);
        WriteLogDetailsWarningIfNeeded(result);
        return result.Details;
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) and returns diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null, bool includeEventTimes = false) {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));
        if (timeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be positive.");

        try {
            return SafeGetResult(logName, session, timeoutMs, machineName, includeEventTimes);
        } catch (Exception ex) {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
    }

    private static EventLogDetailsResult SafeGetResult(string logName, string? machineName, int timeoutMs, bool includeEventTimes) {
        EventLogSessionOpenResult? sessionResult = null;
        try {
            sessionResult = CreateSessionResult(machineName, "LogDetails", logName, timeoutMs, emitDiagnostics: false);
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
                return Failure(logName, machineName, EventLogDetailsStatus.Timeout, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (UnauthorizedAccessException ex) {
                return Failure(logName, machineName, EventLogDetailsStatus.AccessDenied, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (EventLogException ex) {
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            } catch (Exception ex) {
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            }

            Exception? logInformationFailure = null;
            try {
                logInfoObj = ExecuteWithTimeout(
                    () => session.GetLogInformation(logName, PathType.LogName),
                    timeoutMs,
                    $"Timed out reading runtime information for '{logName}' on '{hostName}' after {timeoutMs} ms.");
            } catch (TimeoutException ex) {
                logInformationFailure = ex;
            } catch (UnauthorizedAccessException ex) {
                logInformationFailure = ex;
            } catch (Exception ex) {
                logInformationFailure = ex;
            }

            var details = new EventLogDetails(_logger, hostName, logConfig, logInfoObj);
            Exception? eventTimeFailure = includeEventTimes
                ? ReadEventTimes(logName, session, timeoutMs, details)
                : null;
            var result = new EventLogDetailsResult {
                LogName = logName,
                MachineName = hostName,
                Status = EventLogDetailsStatus.Success,
                Details = details,
                TimeoutMs = timeoutMs
            };
            foreach (EventLogDetailsDiagnostic diagnostic in details.Diagnostics) {
                AppendResultDiagnostic(result, diagnostic.Status, diagnostic.Message, diagnostic.ErrorType);
            }
            if (logInfoObj == null) {
                AppendResultDiagnostic(
                    result,
                    MapLogInformationFailureStatus(logInformationFailure),
                    CreateLogInformationFailureMessage(logInformationFailure),
                    logInformationFailure?.GetType().Name ?? string.Empty);
            }
            if (eventTimeFailure != null) {
                ApplyEventTimeFailure(result, eventTimeFailure);
            }
            return result;
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

    private static Exception? ReadEventTimes(string logName, EventLogSession session, int timeoutMs, EventLogDetails details) {
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
            return null;
        } catch (Exception ex) {
            return ex;
        }
    }

    internal static void ApplyEventTimeFailure(EventLogDetailsResult result, Exception failure) {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (failure == null) throw new ArgumentNullException(nameof(failure));

        string message = $"Oldest/newest event times could not be read: {failure.Message}";
        AppendResultDiagnostic(result, MapEventTimeFailureStatus(failure), message, failure.GetType().Name);
    }

    internal static void AppendResultDiagnostic(
        EventLogDetailsResult result,
        EventLogDetailsStatus status,
        string message,
        string errorType) {

        if (result == null) throw new ArgumentNullException(nameof(result));
        result.Status = MergeDiagnosticStatus(result.Status, status);
        if (!string.IsNullOrWhiteSpace(message)) {
            result.ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? message
                : $"{result.ErrorMessage} {message}";
        }
        if (string.IsNullOrWhiteSpace(errorType)) {
            return;
        }
        if (string.IsNullOrWhiteSpace(result.ErrorType)) {
            result.ErrorType = errorType;
        } else if (!result.ErrorType.Split(';').Contains(errorType, StringComparer.Ordinal)) {
            result.ErrorType = $"{result.ErrorType};{errorType}";
        }
    }

    internal static EventLogDetailsStatus MergeDiagnosticStatus(
        EventLogDetailsStatus current,
        EventLogDetailsStatus candidate) {

        return GetDetailsStatusPriority(candidate) > GetDetailsStatusPriority(current)
            ? candidate
            : current;
    }

    private static int GetDetailsStatusPriority(EventLogDetailsStatus status) {
        return status switch {
            EventLogDetailsStatus.Success => -1,
            EventLogDetailsStatus.Error => 1,
            EventLogDetailsStatus.AccessDenied or
            EventLogDetailsStatus.Timeout or
            EventLogDetailsStatus.HostUnavailable or
            EventLogDetailsStatus.SessionUnavailable => 2,
            _ => 0
        };
    }

    internal static EventLogDetailsStatus MapEventTimeFailureStatus(Exception failure) {
        if (failure == null) throw new ArgumentNullException(nameof(failure));

        return failure switch {
            EventLogSessionException sessionException => MapSessionFailureStatus(sessionException.Status),
            UnauthorizedAccessException => EventLogDetailsStatus.AccessDenied,
            TimeoutException => EventLogDetailsStatus.Timeout,
            _ => EventLogDetailsStatus.EventTimesUnavailable
        };
    }

    internal static EventLogDetailsStatus MapLogInformationFailureStatus(Exception? failure) {
        return failure switch {
            EventLogSessionException sessionException => MapSessionFailureStatus(sessionException.Status),
            UnauthorizedAccessException => EventLogDetailsStatus.AccessDenied,
            TimeoutException => EventLogDetailsStatus.Timeout,
            _ => EventLogDetailsStatus.LogInformationUnavailable
        };
    }

    private static string CreateLogInformationFailureMessage(Exception? failure) {
        const string summary = "Event log configuration was collected, but runtime log information was unavailable.";
        return failure == null || string.IsNullOrWhiteSpace(failure.Message)
            ? summary
            : $"{summary} {failure.Message}";
    }

    internal static void WriteLogDetailsWarningIfNeeded(EventLogDetailsResult result) {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (result.HasDiagnosticFailure) {
            _logger.WriteWarning(result.DiagnosticMessage);
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

        string[]? exactNames = listLog != null && listLog.Length > 0 &&
                               listLog.All(name => name.IndexOf('*') < 0 && name.IndexOf('?') < 0)
            ? listLog.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : null;
        string hostName = string.IsNullOrWhiteSpace(machineName) ? GetFQDN() : machineName!;
        EventLogSessionOpenResult sessionResult = CreateSessionResult(machineName, "DisplayEventLogResults", "*", timeoutMs, emitDiagnostics: false);
        if (!sessionResult.Success || sessionResult.Session == null) {
            string[] failedLogNames = exactNames ?? new[] { "*" };
            foreach (string failedLogName in failedLogNames) {
                yield return Failure(failedLogName, hostName, MapSessionFailureStatus(sessionResult.Status), sessionResult.ErrorMessage, timeoutMs, sessionResult.ErrorType);
            }
            sessionResult.Dispose();
            yield break;
        }

        EventLogSession activeSession = sessionResult.Session;
        try {
            if (exactNames != null) {
                foreach (string exactName in exactNames) {
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

    internal static EventLogDetailsStatus MapSessionFailureStatus(EventLogSessionOpenStatus status) {
        return status switch {
            EventLogSessionOpenStatus.AccessDenied => EventLogDetailsStatus.AccessDenied,
            EventLogSessionOpenStatus.Timeout => EventLogDetailsStatus.Timeout,
            EventLogSessionOpenStatus.NegativeCache => EventLogDetailsStatus.HostUnavailable,
            EventLogSessionOpenStatus.RpcUnavailable => EventLogDetailsStatus.HostUnavailable,
            EventLogSessionOpenStatus.EventLogSessionUnavailable => EventLogDetailsStatus.HostUnavailable,
            EventLogSessionOpenStatus.Error => EventLogDetailsStatus.Error,
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
            WriteLogDetailsWarningIfNeeded(result);
            if (result.Details != null) {
                yield return result.Details;
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
            WriteLogDetailsWarningIfNeeded(result);
            if (result.Details != null) {
                yield return result.Details;
            }
        }
    }
}
