namespace EventViewerX;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

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
    /// <param name="sourceLogName">Optional log name used for source creation checks.</param>
    /// <returns><c>true</c> if creation succeeded.</returns>
    public static bool CreateLog(string logName, string? sourceName = null, string? machineName = null, int maximumKilobytes = 0, OverflowAction overflowAction = OverflowAction.OverwriteAsNeeded, int retentionDays = 7, string? sourceLogName = null) {
        if (string.IsNullOrEmpty(sourceName)) {
            sourceName = logName;
        }

        if (string.IsNullOrEmpty(sourceLogName)) {
            sourceLogName = logName;
        }
        sourceName ??= logName;
        sourceLogName ??= logName;

        try {
            bool logExists = LogExistsSafe(logName, machineName);
            bool sourceExists = SourceExistsSafe(sourceName, sourceLogName, machineName);

            var data = new EventSourceCreationData(sourceName, sourceLogName);
            if (!string.IsNullOrEmpty(machineName)) {
                data.MachineName = machineName;
            }

            if (!logExists || !sourceExists) {
                try {
                    EventLog.CreateEventSource(data);
                } catch (Exception createEx) {
                    if (createEx is System.ComponentModel.Win32Exception win32 && win32.NativeErrorCode == 183) {
                        // Already exists – safe to continue.
                    } else {
                        throw;
                    }
                }
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

    private static readonly ConcurrentDictionary<string, bool> _sourceCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, bool> _sourceDenied = new(StringComparer.OrdinalIgnoreCase);

    private static string BuildSourceKey(string source, string? logName, string? machineName) => $"{machineName ?? "."}|{logName ?? string.Empty}|{source}";

    private static bool SourceExistsSafe(string sourceName, string? logName, string? machineName) {
        string key = BuildSourceKey(sourceName, logName, machineName);
        if (_sourceCache.TryGetValue(key, out bool cached)) {
            return cached;
        }

        if (_sourceDenied.ContainsKey(key)) {
            return false;
        }

        bool exists;
        try {
            if (string.IsNullOrEmpty(machineName) && !string.IsNullOrEmpty(logName)) {
                // Local, scoped check via registry to avoid probing restricted logs.
                exists = SourceExistsRegistryLocal(sourceName, logName!);
            } else {
                exists = string.IsNullOrEmpty(machineName)
                    ? EventLog.SourceExists(sourceName)
                    : EventLog.SourceExists(sourceName, machineName);
            }
        } catch {
            exists = false;
        }

        _sourceCache[key] = exists;
        return exists;
    }

    private static bool SourceExistsRegistryLocal(string sourceName, string logName) {
        try {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($"SYSTEM\\CurrentControlSet\\Services\\EventLog\\{logName}\\{sourceName}");
            return key is not null;
        } catch {
            return false;
        }
    }

    private static bool HasLocalAdmin() {
        try {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return false;
            }

            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
            bool exists = LogExistsSafe(logName, machineName);
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
        return LogExistsSafe(logName, machineName);
    }

    private static bool LogExistsSafe(string logName, string? machineName) {
        if (string.IsNullOrWhiteSpace(logName)) {
            return false;
        }

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
        if (string.IsNullOrWhiteSpace(logName)) {
            return false;
        }

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

    /// <summary>
    /// Removes an event source from a machine, using scoped existence checks to avoid probing restricted logs.
    /// </summary>
    /// <param name="sourceName">Event source to delete.</param>
    /// <param name="machineName">Target machine; null for local.</param>
    /// <param name="logName">Optional log name used only for existence probing on local machine.</param>
    /// <returns><c>true</c> when the source was removed.</returns>
    public static bool RemoveSource(string sourceName, string? machineName = null, string? logName = null) {
        if (string.IsNullOrWhiteSpace(sourceName)) {
            return false;
        }

        try {
            bool exists = SourceExistsSafe(sourceName, logName, machineName);
            if (!exists) {
                return false;
            }

            if (string.IsNullOrEmpty(machineName)) {
                EventLog.DeleteEventSource(sourceName);
            } else {
                EventLog.DeleteEventSource(sourceName, machineName);
            }

            _sourceCache[BuildSourceKey(sourceName, logName, machineName)] = false;
            return true;
        } catch {
            return false;
        }
    }
}
