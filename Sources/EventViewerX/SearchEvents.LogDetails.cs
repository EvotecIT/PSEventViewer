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
    /// Returns details for a single log without enumerating all logs. Time‑bounded to avoid hangs on large/remote channels.
    /// </summary>
    public static EventLogDetails GetLogDetails(string logName, string machineName = null, int timeoutMs = 3000) {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try {
            var task = Task.Run(() => SafeGet(logName, machineName, timeoutMs));
            var completed = Task.WhenAny(task, Task.Delay(timeoutMs)).GetAwaiter().GetResult();
            return completed == task ? task.GetAwaiter().GetResult() : null;
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Reuses an existing EventLogSession (caller owns it) to avoid repeated handshakes per host.
    /// </summary>
    public static EventLogDetails GetLogDetails(string logName, EventLogSession session, int timeoutMs = 3000, string? machineName = null)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));

        try
        {
            var task = Task.Run(() => SafeGet(logName, session, timeoutMs, machineName));
            var completed = Task.WhenAny(task, Task.Delay(timeoutMs)).GetAwaiter().GetResult();
            return completed == task ? task.GetAwaiter().GetResult() : null;
        }
        catch
        {
            return null;
        }
    }

    private static EventLogDetails SafeGet(string logName, string machineName, int timeoutMs) {
        EventLogSession session = null;
        try {
            session = CreateSession(machineName, "LogDetails", logName, timeoutMs);
            if (session == null) return null;

            EventLogConfiguration logConfig = null;
            EventLogInformation logInfoObj = null;

            try {
                logConfig = new EventLogConfiguration(logName, session);
            } catch (EventLogException ex) {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {machineName ?? GetFQDN()}: {ex.Message}");
            }

            try {
                logInfoObj = session.GetLogInformation(logName, PathType.LogName);
            } catch (Exception ex) {
                _logger.WriteVerbose($"Couldn't get log information for {logName} on {machineName ?? GetFQDN()}: {ex.Message}");
            }

            if (logConfig == null) return null;
            return new EventLogDetails(_logger, machineName ?? GetFQDN(), logConfig, logInfoObj);
        }
        finally {
            session?.Dispose();
        }
    }

    private static EventLogDetails SafeGet(string logName, EventLogSession session, int timeoutMs, string? machineName)
    {
        try
        {
            EventLogConfiguration logConfig = null;
            EventLogInformation logInfoObj = null;

            try
            {
                logConfig = new EventLogConfiguration(logName, session);
            }
            catch (EventLogException ex)
            {
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {(machineName ?? GetFQDN())}: {ex.Message}");
            }

            try
            {
                logInfoObj = session.GetLogInformation(logName, PathType.LogName);
            }
            catch (Exception ex)
            {
                _logger.WriteVerbose($"Couldn't get log information for {logName} on {(machineName ?? GetFQDN())}: {ex.Message}");
            }

            if (logConfig == null) return null;
            return new EventLogDetails(_logger, machineName ?? GetFQDN(), logConfig, logInfoObj);
        }
        finally
        {
            // session lifetime owned by caller
        }
    }

    public static IEnumerable<EventLogDetails> DisplayEventLogs(string[] listLog = null, string machineName = null) {
        EventLogSession session = null;
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
        try {
            var namesTask = Task.Run(() => session.GetLogNames());
            var completed = Task.WhenAny(namesTask, Task.Delay(DefaultSessionTimeoutMs)).GetAwaiter().GetResult();
            if (completed != namesTask) {
                _logger.WriteWarning($"DisplayEventLogs: timeout enumerating logs on {machineName} after {DefaultSessionTimeoutMs} ms");
                MarkHostUnreachable(machineName);
                yield break;
            }
            logNames = namesTask.GetAwaiter().GetResult().ToArray();
        } catch (Exception ex) {
            _logger.WriteWarning($"DisplayEventLogs: failed to enumerate logs on {machineName}: {ex.Message}");
            MarkHostUnreachable(machineName);
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
            EventLogConfiguration logConfig;
            try {
                logConfig = new EventLogConfiguration(logName, session);
                // Rest of your code...
            } catch (EventLogException ex) {
                logConfig = null;
                _logger.WriteWarning($"Couldn't create EventLogConfiguration for {logName} on {machineName}. Error: {ex.Message}");
            }
            EventLogInformation logInfoObj;
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
            EventLogDetails logDetails = new EventLogDetails(_logger, machineName, logConfig, logInfoObj);
            yield return logDetails;

        }

        if (ownsSession) {
            session.Dispose();
        }
    }

    public static IEnumerable<EventLogDetails> DisplayEventLogsParallel(string[] listLog = null, List<string> machineNames = null, int maxDegreeOfParallelism = 8, CancellationToken cancellationToken = default) {
        if (machineNames == null || !machineNames.Any()) {
            machineNames = new List<string> { null };
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
            EventLogDetails logDetails;
            while (logDetailsCollection.TryTake(out logDetails)) {
                yield return logDetails;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
