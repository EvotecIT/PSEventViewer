using Microsoft.Win32;
using System.Xml.Linq;

namespace EventViewerX;

/// <summary>
/// WEC subscriptions (readers).
/// </summary>
public partial class SearchEvents : Settings {
    private const string SubscriptionsKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\EventCollector\\Subscriptions";

    /// <summary>
    /// Enumerates Windows Event Collector (WEC) subscriptions from the registry.
    /// </summary>
    /// <param name="machineName">Remote computer or null for local.</param>
    public static IEnumerable<SubscriptionInfo> GetCollectorSubscriptions(string? machineName = null) {
        foreach (var sub in GetCollectorSubscriptionsInternal(machineName, RegistryView.Registry64)) {
            yield return sub;
        }

        // On some systems the data may live under 32-bit view (rare); try as best-effort
        foreach (var sub in GetCollectorSubscriptionsInternal(machineName, RegistryView.Registry32)) {
            yield return sub;
        }
    }

    private static IEnumerable<SubscriptionInfo> GetCollectorSubscriptionsInternal(string? machineName, RegistryView view) {
        var results = new List<SubscriptionInfo>();
        RegistryKey? hklm = null;
        RegistryKey? subsKey = null;
        try {
            hklm = string.IsNullOrEmpty(machineName)
                ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                : RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName!, view);
            subsKey = hklm.OpenSubKey(SubscriptionsKey, writable: false);
            if (subsKey == null) return results;

            foreach (var name in subsKey.GetSubKeyNames()) {
                using var subKey = subsKey.OpenSubKey(name, false);
                if (subKey == null) continue;

                var info = new SubscriptionInfo {
                    Name = name,
                    MachineName = machineName
                };

                // Enabled may be stored as DWORD or string; read loosely
                object? enabled = subKey.GetValue("Enabled");
                if (enabled is int dword) {
                    info.Enabled = dword != 0;
                } else if (enabled is string s) {
                    if (bool.TryParse(s, out var b)) info.Enabled = b;
                    else if (int.TryParse(s, out var i)) info.Enabled = i != 0;
                }

                info.ContentFormat = subKey.GetValue("ContentFormat") as string;
                info.DeliveryMode = subKey.GetValue("DeliveryMode") as string;

                // Try multiple well-known value names to find the XML
                var xml = subKey.GetValue("Subscription") as string
                          ?? subKey.GetValue("SubscriptionXml") as string
                          ?? subKey.GetValue("Configuration") as string
                          ?? subKey.GetValue("Config") as string;
                if (!string.IsNullOrWhiteSpace(xml) && xml!.IndexOf("<Subscription", StringComparison.OrdinalIgnoreCase) >= 0) {
                    info.RawXml = xml;

                    try {
                        var xdoc = XDocument.Parse(xml);
                        // Description
                        info.Description = xdoc.Root?.Element(XName.Get("Description", xdoc.Root?.Name.NamespaceName ?? ""))?.Value;

                        // Queries: collect any <Query> text under <Select> or direct XPath
                        var queries = new List<string>();
                        foreach (var sel in xdoc.Descendants().Where(e => e.Name.LocalName.Equals("Select", StringComparison.OrdinalIgnoreCase))) {
                            var q = sel.Value?.Trim();
                            if (!string.IsNullOrEmpty(q)) queries.Add(q);
                        }
                        if (queries.Count > 0) info.Queries = queries;
                    } catch (Exception ex) {
                        _logger.WriteWarning($"Failed to parse subscription '{name}' XML on '{machineName ?? GetFQDN()}': {ex.Message}");
                    }
                }

                results.Add(info);
            }
        } catch (Exception ex) {
            _logger.WriteWarning($"Failed to enumerate WEC subscriptions on '{machineName ?? GetFQDN()}' (view={view}): {ex.Message}");
        } finally {
            subsKey?.Dispose();
            hklm?.Dispose();
        }
        return results;
    }
}
