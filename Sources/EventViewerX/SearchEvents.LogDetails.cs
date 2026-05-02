using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventViewerX;

/// <summary>
/// Helper methods for retrieving event log configuration details.
/// </summary>
public partial class SearchEvents : Settings {
    private const int DiagnosticTimeoutGraceMs = 500;

    /// <summary>
    /// Returns details for a single log without enumerating all logs. Time‑bounded to avoid hangs on large/remote channels.
    /// </summary>
    public static EventLogDetails? GetLogDetails(string logName, string? machineName = null, int timeoutMs = 3000) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        return GetLogDetailsResult(logName, machineName, timeoutMs).Details;
    }

    /// <summary>
    /// Returns details for a single log with diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, string? machineName = null, int timeoutMs = 3000) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try {
            Task<EventLogDetailsResult> task = Task.Run(() => SafeGetResult(logName, machineName, timeoutMs));
            Task completed = Task.WhenAny(task, Task.Delay(OuterTimeoutMs(timeoutMs))).GetAwaiter().GetResult();
            return completed == task
                ? task.GetAwaiter().GetResult()
                : Failure(logName, machineName, EventLogDetailsStatus.Timeout, $"Timed out reading event log details after {timeoutMs} ms.", timeoutMs, "LogDetailsTimeout");
        } catch (Exception ex) {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) to avoid repeated handshakes per host.
    /// </summary>
    public static EventLogDetails? GetLogDetails(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        return GetLogDetailsResult(logName, session, timeoutMs, machineName).Details;
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) and returns diagnostic status when the log cannot be read.
    /// </summary>
    public static EventLogDetailsResult GetLogDetailsResult(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try
        {
            Task<EventLogDetailsResult> task = Task.Run(() => SafeGetResult(logName, session, timeoutMs, machineName));
            Task completed = Task.WhenAny(task, Task.Delay(OuterTimeoutMs(timeoutMs))).GetAwaiter().GetResult();
            return completed == task
                ? task.GetAwaiter().GetResult()
                : Failure(logName, machineName, EventLogDetailsStatus.Timeout, $"Timed out reading event log details after {timeoutMs} ms.", timeoutMs, "LogDetailsTimeout");
        }
        catch (Exception ex)
        {
            return Failure(logName, machineName, EventLogDetailsStatus.Error, ex.Message, timeoutMs, ex.GetType().Name);
        }
    }

    private static EventLogDetailsResult SafeGetResult(string logName, string? machineName, int timeoutMs) {
        EventLogSessionOpenResult? sessionResult = null;
        try {
            sessionResult = CreateSessionResult(machineName, "LogDetails", logName, timeoutMs);
            if (!sessionResult.Success || sessionResult.Session == null) {
                return Failure(
                    logName,
                    machineName,
                    EventLogDetailsStatus.SessionUnavailable,
                    string.IsNullOrWhiteSpace(sessionResult.ErrorMessage)
                        ? "Event log session could not be opened. Check host reachability, RPC/firewall access, Remote Event Log Management, and permissions."
                        : sessionResult.ErrorMessage,
                    timeoutMs,
                    string.IsNullOrWhiteSpace(sessionResult.ErrorType)
                        ? sessionResult.Status.ToString()
                        : sessionResult.ErrorType);
            }

            return SafeGetResult(logName, sessionResult.Session, timeoutMs, machineName);
        }
        finally {
            sessionResult?.Dispose();
        }
    }

    private static EventLogDetailsResult SafeGetResult(string logName, EventLogSession session, int timeoutMs, string? machineName)
    {
        try
        {
            EventLogConfiguration? logConfig = null;
            EventLogInformation? logInfoObj = null;
            string hostName = machineName ?? GetFQDN();

            try
            {
                logConfig = new EventLogConfiguration(logName, session);
            }
            catch (EventLogException ex)
            {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {hostName}: {ex.Message}");
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {hostName}: {ex.Message}");
                return Failure(logName, machineName, EventLogDetailsStatus.LogConfigurationUnavailable, ex.Message, timeoutMs, ex.GetType().Name);
            }

            try
            {
                logInfoObj = session.GetLogInformation(logName, PathType.LogName);
            }
            catch (Exception ex)
            {
                _logger.WriteVerbose($"Couldn't get log information for {logName} on {hostName}: {ex.Message}");
            }

            var details = new EventLogDetails(_logger, hostName, logConfig, logInfoObj);
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

    private static int OuterTimeoutMs(int timeoutMs) =>
        Math.Max(timeoutMs, 1) + DiagnosticTimeoutGraceMs;

    /// <summary>
    /// Enumerates event logs on the specified machine and returns their configuration details.
    /// </summary>
    /// <param name="listLog">Optional list of log name patterns (supports * and ?) to include.</param>
    /// <param name="machineName">Remote machine name; <c>null</c> targets the local computer.</param>
    /// <returns>An enumerable of <see cref="EventLogDetails"/> objects for each matching log.</returns>
    public static IEnumerable<EventLogDetails> DisplayEventLogs(string[]? listLog = null, string? machineName = null) {
        EventLogSession? session = null;
        bool ownsSession = false;
        try {
            if (!string.IsNullOrEmpty(machineName)) {
                session = CreateSession(machineName, "DisplayEventLogs", "*", DefaultSessionTimeoutMs);
                ownsSession = true;
            } else {
                session = new EventLogSession();
                machineName = GetFQDN();
                ownsSession = true;
            }
        } catch {
            session = null;
        }

        if (session == null) yield break;

        string[] logNames;
        var hostName = machineName ?? GetFQDN();
        try {
            var namesTask = Task.Run(() => session.GetLogNames());
            var completed = Task.WhenAny(namesTask, Task.Delay(DefaultSessionTimeoutMs)).GetAwaiter().GetResult();
            if (completed != namesTask) {
                _logger.WriteWarning($"DisplayEventLogs: timeout enumerating logs on {hostName} after {DefaultSessionTimeoutMs} ms");
                MarkHostUnreachable(hostName);
                yield break;
            }
            logNames = namesTask.GetAwaiter().GetResult().ToArray();
        } catch (Exception ex) {
            _logger.WriteWarning($"DisplayEventLogs: failed to enumerate logs on {hostName}: {ex.Message}");
            MarkHostUnreachable(hostName);
            yield break;
        }
        foreach (string logName in logNames) {
            if (listLog != null) {
                // Check if any log matches the pattern
                if (!listLog.Any(log => {
                    // Replace '*' with '.*' and '?' with '.' to convert the wildcard to a regex pattern
                    var regexPattern = "^" + Regex.Escape(log).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    return Regex.IsMatch(logName, regexPattern, RegexOptions.IgnoreCase);
                })) {
                    continue;
                }
            }
            EventLogConfiguration? logConfig;
            try {
                logConfig = new EventLogConfiguration(logName, session);
                // Rest of your code...
            } catch (EventLogException ex) {
                logConfig = null;
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {machineName}. Error: {ex.Message}");
            }
            EventLogInformation? logInfoObj;
            try {
                logInfoObj = session.GetLogInformation(logName, PathType.LogName);
            } catch (Exception ex) {
                logInfoObj = null;
                _logger.WriteWarning($"Couldn't get log information for {logName} on {machineName}. Error: {ex.Message}");
            }

            if (logInfoObj == null) {
                _logger.WriteVerbose($"Log information is not available for {logName} on {machineName}");
            }

            if (logConfig == null) {
                _logger.WriteWarning($"LogConfig is null for {logName} on {machineName}");
                continue;
            }
            EventLogDetails logDetails = new EventLogDetails(_logger, hostName, logConfig, logInfoObj);
            yield return logDetails;

        }

        if (ownsSession) {
            session.Dispose();
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
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string?> { null };
        }
        using BlockingCollection<EventLogDetails> logDetailsCollection = new();

        Task.Factory.StartNew(() => {
            Parallel.ForEach(machineNames, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cancellationToken }, machineName => {
                try {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.WriteVerbose("Querying event log settings on " + machineName);
                    foreach (EventLogDetails logDetails in DisplayEventLogs(listLog, machineName)) {
                        cancellationToken.ThrowIfCancellationRequested();
                        logDetailsCollection.Add(logDetails, cancellationToken);
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    _logger.WriteWarning("Error querying event log settings on " + machineName + ". Error: " + ex.Message);
                }
            });
            logDetailsCollection.CompleteAdding();
        }, cancellationToken);

        while (!logDetailsCollection.IsCompleted) {
            cancellationToken.ThrowIfCancellationRequested();
            while (logDetailsCollection.TryTake(out EventLogDetails? logDetails)) {
                if (logDetails != null) {
                    yield return logDetails;
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
