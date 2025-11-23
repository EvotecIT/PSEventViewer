using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

/// <summary>
/// Channel policy setters (modern wevt + classic fallback where possible).
/// </summary>
public partial class SearchEvents : Settings {
    private const long ClassicMinimumKilobytes = 64L;
    /// <summary>
    /// Applies the provided <see cref="ChannelPolicy"/> to the target log.
    /// For classic logs, only MaximumSizeInBytes and Mode (Circular/Retain) are honored.
    /// </summary>
    /// <param name="policy">Policy object with values to set (nulls are ignored).</param>
    /// <param name="retentionDays">For classic logs when Mode maps to OverwriteOlder; default 7.</param>
    /// <returns>true if applied successfully; false otherwise.</returns>
    public static bool SetChannelPolicy(ChannelPolicy policy, int retentionDays = 7) {
        var result = SetChannelPolicyDetailed(policy, retentionDays);
        // Keep boolean API: only true if all requested changes applied (no partial success)
        return result.Success;
    }

    /// <summary>
    /// Applies the provided policy and returns a detailed result including partial success info.
    /// </summary>
    public static ChannelPolicyApplyResult SetChannelPolicyDetailed(ChannelPolicy policy, int retentionDays = 7) {
        if (policy == null) throw new ArgumentNullException(nameof(policy));
        if (string.IsNullOrWhiteSpace(policy.LogName)) throw new ArgumentException("LogName is required.", nameof(policy));

        EventLogSession? session = null;
        var result = new ChannelPolicyApplyResult { LogName = policy.LogName, MachineName = policy.MachineName };
        try {
            session = CreateSession(policy.MachineName, "ChannelPolicy.Set", policy.LogName, DefaultSessionTimeoutMs);
            if (session == null) {
                result.Errors.Add("Session open timed out or failed.");
                return result;
            }

            try {
                using var cfg = new EventLogConfiguration(policy.LogName, session);
                bool changed = false;

                // Apply properties individually and record outcomes
                if (policy.IsEnabled.HasValue && policy.IsEnabled.Value != cfg.IsEnabled) {
                    try { cfg.IsEnabled = policy.IsEnabled.Value; changed = true; result.AppliedProperties.Add(nameof(policy.IsEnabled)); }
                    catch (Exception ex) { result.Errors.Add($"Failed to set IsEnabled: {ex.Message}"); }
                }
                if (policy.MaximumSizeInBytes.HasValue && policy.MaximumSizeInBytes.Value > 0 && policy.MaximumSizeInBytes.Value != cfg.MaximumSizeInBytes) {
                    try { cfg.MaximumSizeInBytes = policy.MaximumSizeInBytes.Value; changed = true; result.AppliedProperties.Add(nameof(policy.MaximumSizeInBytes)); }
                    catch (Exception ex) { result.Errors.Add($"Failed to set MaximumSizeInBytes: {ex.Message}"); }
                }
                if (!string.IsNullOrEmpty(policy.LogFilePath) && !string.Equals(policy.LogFilePath, cfg.LogFilePath, StringComparison.OrdinalIgnoreCase)) {
                    try { cfg.LogFilePath = policy.LogFilePath; changed = true; result.AppliedProperties.Add(nameof(policy.LogFilePath)); }
                    catch (Exception ex) { result.Errors.Add($"Failed to set LogFilePath: {ex.Message}"); }
                }
                // Isolation is effectively read-only via EventLogConfiguration in most cases
                if (policy.Isolation.HasValue) {
                    result.SkippedOrUnsupported.Add(nameof(policy.Isolation));
                }
                if (policy.Mode.HasValue && policy.Mode.Value != cfg.LogMode) {
                    try { cfg.LogMode = policy.Mode.Value; changed = true; result.AppliedProperties.Add(nameof(policy.Mode)); }
                    catch (Exception ex) { result.Errors.Add($"Failed to set Mode: {ex.Message}"); }
                }
                if (!string.IsNullOrEmpty(policy.SecurityDescriptor) && !string.Equals(policy.SecurityDescriptor, cfg.SecurityDescriptor, StringComparison.Ordinal)) {
                    try { cfg.SecurityDescriptor = policy.SecurityDescriptor; changed = true; result.AppliedProperties.Add(nameof(policy.SecurityDescriptor)); }
                    catch (Exception ex) { result.Errors.Add($"Failed to set SecurityDescriptor: {ex.Message}"); }
                }

                if (changed) {
                    try {
                        cfg.SaveChanges();
                    } catch (Exception ex) {
                        result.Errors.Add($"SaveChanges failed: {ex.Message}");
                    }
                }
                // Success implies: no errors, and no attempt to set Isolation (considered unsupported here)
                result.Success = result.Errors.Count == 0 && (policy.Isolation == null);
                result.PartialSuccess = result.Errors.Count > 0 || result.SkippedOrUnsupported.Count > 0 || (changed && !result.Success);
                return result;
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
                        result.SkippedOrUnsupported.Add(nameof(policy.IsEnabled));
                    }

                    if (policy.MaximumSizeInBytes.HasValue && policy.MaximumSizeInBytes.Value > 0) {
                        try {
                            long kib = Math.Max(ClassicMinimumKilobytes, policy.MaximumSizeInBytes.Value / 1024L);
                            classic.MaximumKilobytes = kib;
                            result.AppliedProperties.Add(nameof(policy.MaximumSizeInBytes));
                        } catch (Exception ex) { result.Errors.Add($"Failed to set MaximumKilobytes (classic): {ex.Message}"); }
                    }

                    if (policy.Mode.HasValue) {
                        try {
                            switch (policy.Mode.Value) {
                                case EventLogMode.Circular:
                                    // Prefer OverwriteAsNeeded; if retentionDays provided explicitly we could use OverwriteOlder
                                    classic.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 0);
                                    result.AppliedProperties.Add(nameof(policy.Mode));
                                    break;
                                case EventLogMode.Retain:
                                    classic.ModifyOverflowPolicy(OverflowAction.DoNotOverwrite, 0);
                                    result.AppliedProperties.Add(nameof(policy.Mode));
                                    break;
                                case EventLogMode.AutoBackup:
                                    _logger.WriteWarning($"AutoBackup mode is not supported via classic API. Ignoring for '{policy.LogName}'.");
                                    result.SkippedOrUnsupported.Add(nameof(policy.Mode));
                                    break;
                            }
                        } catch (Exception ex) { result.Errors.Add($"Failed to set Mode (classic): {ex.Message}"); }
                    }
                    result.Success = result.Errors.Count == 0 && result.SkippedOrUnsupported.Count == 0;
                    result.PartialSuccess = !result.Success && (result.AppliedProperties.Count > 0 || result.Errors.Count > 0);
                    return result;
                } catch (Exception exClassic) {
                    _logger.WriteWarning($"Failed to apply classic log policy for '{policy.LogName}' on '{policy.MachineName ?? GetFQDN()}': {exClassic.Message}");
                    result.Errors.Add(exClassic.Message);
                    result.Success = false;
                    result.PartialSuccess = false;
                    return result;
                }
            }
        } finally {
            session?.Dispose();
        }
        // Should not reach, but ensure deterministic value
        // (dispose in finally already executed)
        // If we do get here, consider it a failure
        // with no partial success.
        // Returning a new object maintains non-null contract.
        // (Defensive programming – not expected path.)
        //
        // Note: callers should inspect Errors for details.
        return result;
    }
}
