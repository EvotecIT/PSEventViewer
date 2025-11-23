using System.Diagnostics;

namespace EventViewerX;

/// <summary>
/// Provides functionality to modify existing event log limits.
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Updates size and overflow policy of a Windows event log.
    /// </summary>
    /// <para>
    /// This method mirrors the functionality of the <c>Limit-EventLog</c> cmdlet
    /// but operates directly on <see cref="EventLog"/>. It can be used on
    /// systems where the native cmdlet is unavailable.
    /// </para>
    /// <param name="logName">Name of the log to modify.</param>
    /// <param name="machineName">Remote machine; <c>null</c> for local host.</param>
    /// <param name="maximumKilobytes">Maximum size in kilobytes. Set to <c>0</c> to leave unchanged.</param>
    /// <param name="overflowAction">
    /// Overflow behaviour when the log is full. <see cref="OverflowAction.OverwriteAsNeeded"/>
    /// removes the oldest events, <see cref="OverflowAction.OverwriteOlder"/> keeps events for
    /// <paramref name="retentionDays"/> days, and <see cref="OverflowAction.DoNotOverwrite"/>
    /// stops writing new events when the log reaches its limit.
    /// </param>
    /// <param name="retentionDays">Retention in days when using <see cref="OverflowAction.OverwriteOlder"/>.</param>
    /// <returns><c>true</c> when modification succeeds; otherwise, <c>false</c>.</returns>
    /// <example>
    ///   <summary>Increase size and keep events for seven days.</summary>
    ///   <code language="powershell">
    ///   Limit-EVXLog -LogName Application -MaximumKilobytes 20480 -OverflowAction OverwriteOlder -RetentionDays 7
    ///   </code>
    ///   <para>Overwrites the oldest entries after seven days.</para>
    /// </example>
    /// <example>
    ///   <summary>Overwrite as needed without retention.</summary>
    ///   <code language="powershell">
    ///   Limit-EVXLog -LogName Application -MaximumKilobytes 10240 -OverflowAction OverwriteAsNeeded
    ///   </code>
    ///   <para>Oldest events are removed first when the log becomes full.</para>
    /// </example>
    public static bool LimitLog(string logName, string machineName = null, int maximumKilobytes = 0, OverflowAction overflowAction = OverflowAction.OverwriteAsNeeded, int retentionDays = 7, string sourceLogName = null) {
        if (string.IsNullOrWhiteSpace(logName)) {
            return false;
        }

        try {
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
}
