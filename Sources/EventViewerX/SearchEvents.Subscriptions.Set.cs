using Microsoft.Win32;

namespace EventViewerX;

/// <summary>
/// WEC subscriptions (writers). Note: changes may require WecSvc restart to take effect.
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Enables or disables a subscription by flipping the 'Enabled' value.
    /// </summary>
    public static bool SetCollectorSubscriptionEnabled(string name, bool enabled, string? machineName = null) {
        return WithSubscriptionKey(name, machineName, writable: true, (key, _) => {
            try {
                key.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
                return true;
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to set Enabled for subscription '{name}' on '{_}': {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Replaces the subscription XML payload. The caller should ensure XML correctness.
    /// </summary>
    public static bool SetCollectorSubscriptionXml(string name, string xml, string? machineName = null) {
        if (string.IsNullOrWhiteSpace(xml) || xml.IndexOf("<Subscription", StringComparison.OrdinalIgnoreCase) < 0) {
            throw new ArgumentException("Provided XML does not look like a WEC Subscription.", nameof(xml));
        }
        return WithSubscriptionKey(name, machineName, writable: true, (key, _) => {
            try {
                // Use a common value name (Subscription); if not present, create it.
                key.SetValue("Subscription", xml, RegistryValueKind.String);
                return true;
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to write XML for subscription '{name}' on '{_}': {ex.Message}");
                return false;
            }
        });
    }

    private static bool WithSubscriptionKey(string name, string? machineName, bool writable, Func<RegistryKey, string, bool> action) {
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };
        foreach (var view in views) {
            RegistryKey? hklm = null; RegistryKey? subsKey = null; RegistryKey? subKey = null;
            try {
                hklm = string.IsNullOrEmpty(machineName)
                    ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                    : RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName!, view);
                subsKey = hklm.OpenSubKey(SubscriptionsKey, writable);
                if (subsKey == null) continue;
                subKey = subsKey.OpenSubKey(name, writable) ?? (writable ? subsKey.CreateSubKey(name) : null);
                if (subKey == null) continue;
                return action(subKey, machineName ?? GetFQDN());
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to access subscription '{name}' on '{machineName ?? GetFQDN()}' (view={view}): {ex.Message}");
            } finally {
                subKey?.Dispose(); subsKey?.Dispose(); hklm?.Dispose();
            }
        }
        return false;
    }
}
