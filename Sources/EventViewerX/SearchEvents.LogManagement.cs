namespace EventViewerX;

using System.Diagnostics;

/// <summary>
/// Provides functionality for managing event logs.
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Creates a new event log with optional configuration.
    /// </summary>
    /// <param name="logName">The name of the log.</param>
    /// <param name="sourceName">The provider name.</param>
    /// <param name="machineName">Target machine.</param>
    /// <param name="maximumKilobytes">Maximum size in KB.</param>
    /// <param name="overflowAction">Overflow policy.</param>
    /// <param name="retentionDays">Retention in days for OverwriteOlder policy.</param>
    /// <returns><c>true</c> if creation succeeded.</returns>
    public static bool CreateLog(string logName, string? sourceName = null, string? machineName = null, int maximumKilobytes = 0, OverflowAction overflowAction = OverflowAction.OverwriteAsNeeded, int retentionDays = 7) {
        if (string.IsNullOrEmpty(sourceName)) {
            sourceName = logName;
        }

        try {
            bool exists = string.IsNullOrEmpty(machineName)
                ? EventLog.Exists(logName)
                : EventLog.Exists(logName, machineName);
            if (!exists) {
                var data = new EventSourceCreationData(sourceName, logName);
                if (!string.IsNullOrEmpty(machineName)) {
                    data.MachineName = machineName;
                }
                EventLog.CreateEventSource(data);
            } else if (!EventLog.SourceExists(sourceName, machineName)) {
                var data = new EventSourceCreationData(sourceName, logName);
                if (!string.IsNullOrEmpty(machineName)) {
                    data.MachineName = machineName;
                }
                EventLog.CreateEventSource(data);
            }

            using EventLog log = string.IsNullOrEmpty(machineName)
                ? new EventLog(logName)
                : new EventLog(logName, machineName);
            if (maximumKilobytes > 0) {
                log.MaximumKilobytes = maximumKilobytes;
            }
            log.ModifyOverflowPolicy(overflowAction, retentionDays);
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Removes an event log from the system.
    /// </summary>
    /// <param name="logName">Name of the log.</param>
    /// <param name="machineName">Target machine.</param>
    /// <returns><c>true</c> when log is removed.</returns>
    public static bool RemoveLog(string logName, string? machineName = null) {
        try {
            bool exists = string.IsNullOrEmpty(machineName)
                ? EventLog.Exists(logName)
                : EventLog.Exists(logName, machineName);
            if (!exists) {
                return false;
            }

            if (string.IsNullOrEmpty(machineName)) {
                EventLog.Delete(logName);
            } else {
                EventLog.Delete(logName, machineName);
            }

            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Determines whether an event log exists.
    /// </summary>
    /// <param name="logName">The log name.</param>
    /// <param name="machineName">Target machine.</param>
    /// <returns><c>true</c> when log exists.</returns>
    public static bool LogExists(string logName, string? machineName = null) {
        try {
            return string.IsNullOrEmpty(machineName)
                ? EventLog.Exists(logName)
                : EventLog.Exists(logName, machineName);
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Clears all entries from an event log and optionally sets retention days.
    /// </summary>
    /// <param name="logName">Log to clear.</param>
    /// <param name="machineName">Target machine.</param>
    /// <param name="retentionDays">Retention days when setting overwrite policy.</param>
    /// <returns><c>true</c> when clearing succeeds.</returns>
    public static bool ClearLog(string logName, string? machineName = null, int? retentionDays = null) {
        try {
            using EventLog log = string.IsNullOrEmpty(machineName)
                ? new EventLog(logName)
                : new EventLog(logName, machineName);
            log.Clear();
            if (retentionDays.HasValue) {
                log.ModifyOverflowPolicy(OverflowAction.OverwriteOlder, retentionDays.Value);
            }
            return true;
        } catch {
            return false;
        }
    }
}
