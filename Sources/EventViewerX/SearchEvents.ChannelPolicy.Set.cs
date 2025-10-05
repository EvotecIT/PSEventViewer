using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

/// <summary>
/// Channel policy setters (modern wevt + classic fallback where possible).
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Applies the provided <see cref="ChannelPolicy"/> to the target log.
    /// For classic logs, only MaximumSizeInBytes and Mode (Circular/Retain) are honored.
    /// </summary>
    /// <param name="policy">Policy object with values to set (nulls are ignored).</param>
    /// <param name="retentionDays">For classic logs when Mode maps to OverwriteOlder; default 7.</param>
    /// <returns>true if applied successfully; false otherwise.</returns>
    public static bool SetChannelPolicy(ChannelPolicy policy, int retentionDays = 7) {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (string.IsNullOrWhiteSpace(policy.LogName)) throw new ArgumentException("LogName is required.", nameof(policy));

        EventLogSession? session = null;
        try {
            session = string.IsNullOrEmpty(policy.MachineName)
                ? new EventLogSession()
                : new EventLogSession(policy.MachineName);

            try {
                using var cfg = new EventLogConfiguration(policy.LogName, session);
                bool changed = false;

                if (policy.IsEnabled.HasValue && policy.IsEnabled.Value != cfg.IsEnabled) {
                    cfg.IsEnabled = policy.IsEnabled.Value;
                    changed = true;
                }
                if (policy.MaximumSizeInBytes.HasValue && policy.MaximumSizeInBytes.Value > 0 && policy.MaximumSizeInBytes.Value != cfg.MaximumSizeInBytes) {
                    cfg.MaximumSizeInBytes = policy.MaximumSizeInBytes.Value;
                    changed = true;
                }
                if (!string.IsNullOrEmpty(policy.LogFilePath) && !string.Equals(policy.LogFilePath, cfg.LogFilePath, StringComparison.OrdinalIgnoreCase)) {
                    cfg.LogFilePath = policy.LogFilePath;
                    changed = true;
                }
                // Isolation is read-only via EventLogConfiguration; ignore if provided
                if (policy.Mode.HasValue && policy.Mode.Value != cfg.LogMode) {
                    cfg.LogMode = policy.Mode.Value;
                    changed = true;
                }
                if (!string.IsNullOrEmpty(policy.SecurityDescriptor) && !string.Equals(policy.SecurityDescriptor, cfg.SecurityDescriptor, StringComparison.Ordinal)) {
                    cfg.SecurityDescriptor = policy.SecurityDescriptor;
                    changed = true;
                }

                if (changed) {
                    cfg.SaveChanges();
                }
                return true;
            } catch (EventLogException modernEx) {
                _logger.WriteVerbose($"EventLogConfiguration not available for '{policy.LogName}' on '{policy.MachineName ?? GetFQDN()}': {modernEx.Message}. Falling back to classic API where possible.");
                // Classic fallback (limited)
                try {
                    using var classic = string.IsNullOrEmpty(policy.MachineName)
                        ? new EventLog(policy.LogName)
                        : new EventLog(policy.LogName, policy.MachineName);

                    if (policy.IsEnabled.HasValue) {
                        // Not supported via classic API
                        _logger.WriteWarning($"Classic logs do not support enabling/disabling via API. Ignoring IsEnabled for '{policy.LogName}'.");
                    }

                    if (policy.MaximumSizeInBytes.HasValue && policy.MaximumSizeInBytes.Value > 0) {
                        long kib = Math.Max(64L, policy.MaximumSizeInBytes.Value / 1024L);
                        classic.MaximumKilobytes = kib;
                    }

                    if (policy.Mode.HasValue) {
                        switch (policy.Mode.Value) {
                            case EventLogMode.Circular:
                                // Prefer OverwriteAsNeeded; if retentionDays provided explicitly we could use OverwriteOlder
                                classic.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 0);
                                break;
                            case EventLogMode.Retain:
                                classic.ModifyOverflowPolicy(OverflowAction.DoNotOverwrite, 0);
                                break;
                            case EventLogMode.AutoBackup:
                                _logger.WriteWarning($"AutoBackup mode is not supported via classic API. Ignoring for '{policy.LogName}'.");
                                break;
                        }
                    }
                    return true;
                } catch (Exception exClassic) {
                    _logger.WriteWarning($"Failed to apply classic log policy for '{policy.LogName}' on '{policy.MachineName ?? GetFQDN()}': {exClassic.Message}");
                    return false;
                }
            }
        } finally {
            session?.Dispose();
        }
    }
}
