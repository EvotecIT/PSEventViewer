namespace EventViewerX;

using System.Diagnostics;

/// <summary>
/// Provides functionality for managing event logs.
/// </summary>
public partial class SearchEvents : Settings {
    // CreateLog moved to SearchEvents.LogManagement.CreateLog.cs

    /// <summary>
    /// Removes an event log from the system.
    /// </summary>
    /// <param name="logName">Name of the log.</param>
    /// <param name="machineName">Target machine.</param>
    /// <returns><c>true</c> when log is removed.</returns>
    public static bool RemoveLog(string logName, string machineName = null) {
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
    public static bool LogExists(string logName, string machineName = null) {
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
    public static bool ClearLog(string logName, string machineName = null, int? retentionDays = null) {
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
