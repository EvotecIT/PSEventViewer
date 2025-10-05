using Microsoft.Win32;
using System.Xml;
using System.Xml.Linq;

namespace EventViewerX;

/// <summary>
/// WEC subscriptions (writers). Note: changes may require WecSvc restart to take effect.
/// </summary>
public partial class SearchEvents : Settings {
    /// <summary>
    /// Enables or disables a subscription by flipping the 'Enabled' value.
    /// </summary>
    public static bool SetCollectorSubscriptionEnabled(string name, bool enabled, string? machineName = null) {
        return WithSubscriptionKey(name, machineName, writable: true, (key, host) => {
            try {
                key.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
                return true;
            } catch (UnauthorizedAccessException ex) {
                _logger.WriteWarning($"Access denied setting 'Enabled' for subscription '{name}' on '{host}'. Ensure permissions to HKLM are granted. Details: {ex.Message}");
                return false;
            } catch (System.Security.SecurityException ex) {
                _logger.WriteWarning($"Security exception setting 'Enabled' for subscription '{name}' on '{host}': {ex.Message}");
                return false;
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to set Enabled for subscription '{name}' on '{host}': {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// Replaces the subscription XML payload. The caller should ensure XML correctness.
    /// </summary>
    public static bool SetCollectorSubscriptionXml(string name, string xml, string? machineName = null) {
        if (string.IsNullOrWhiteSpace(xml)) {
            throw new ArgumentException("XML cannot be null or empty.", nameof(xml));
        }

        // Basic XML validation with DTD prohibited to limit XML injection/XXE risk
        try {
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(new System.IO.StringReader(xml), settings);
            var xdoc = XDocument.Load(reader, LoadOptions.None);
            var root = xdoc.Root;
            if (root == null || !root.Name.LocalName.Equals("Subscription", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("Root element must be <Subscription>.", nameof(xml));
            }
        } catch (XmlException xex) {
            throw new ArgumentException($"Invalid XML content: {xex.Message}", nameof(xml));
        }

        return WithSubscriptionKey(name, machineName, writable: true, (key, host) => {
            try {
                key.SetValue("Subscription", xml, RegistryValueKind.String);
                return true;
            } catch (UnauthorizedAccessException ex) {
                _logger.WriteWarning($"Access denied writing XML for subscription '{name}' on '{host}'. Ensure permissions to HKLM are granted. Details: {ex.Message}");
                return false;
            } catch (System.Security.SecurityException ex) {
                _logger.WriteWarning($"Security exception writing XML for subscription '{name}' on '{host}': {ex.Message}");
                return false;
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to write XML for subscription '{name}' on '{host}': {ex.Message}");
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
            } catch (UnauthorizedAccessException ex) {
                _logger.WriteWarning($"Access denied accessing subscription '{name}' on '{machineName ?? GetFQDN()}' (view={view}). Details: {ex.Message}");
            } catch (System.Security.SecurityException ex) {
                _logger.WriteWarning($"Security exception accessing subscription '{name}' on '{machineName ?? GetFQDN()}' (view={view}). Details: {ex.Message}");
            } catch (System.IO.IOException ex) {
                _logger.WriteWarning($"I/O error accessing subscription '{name}' on '{machineName ?? GetFQDN()}' (view={view}). Possibly Remote Registry service disabled or network issue. Details: {ex.Message}");
            } catch (Exception ex) {
                _logger.WriteWarning($"Failed to access subscription '{name}' on '{machineName ?? GetFQDN()}' (view={view}): {ex.Message}");
            } finally {
                subKey?.Dispose(); subsKey?.Dispose(); hklm?.Dispose();
            }
        }
        return false;
    }
}
