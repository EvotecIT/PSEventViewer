namespace EventViewerX;

public partial class SearchEvents : Settings {
    private static string _fqdn;

    /// <summary>
    /// Get the fully qualified domain name of the machine
    /// </summary>
    /// <returns>Machine FQDN.</returns>
    private static string GetFQDN() {
        if (!string.IsNullOrEmpty(_fqdn)) {
            return _fqdn;
        }

        try {
            _fqdn = Dns.GetHostEntry("").HostName;
        } catch (Exception ex) {
            _logger.WriteVerbose($"Failed to resolve FQDN via DNS: {ex.Message}. Falling back to machine name.");
            _fqdn = Environment.MachineName;
        }

        return _fqdn;
    }
}
