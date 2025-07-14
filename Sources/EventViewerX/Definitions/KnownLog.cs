namespace EventViewerX;

/// <summary>
/// Enumerates common Windows event logs.
/// </summary>
/// <para>
/// Values map directly to log names used by the operating system.
/// </para>
public enum KnownLog {
    Application,
    System,
    Security,
    Setup,
    ForwardedEvents,
    DirectoryService,
    DNSServer,
    WindowsPowerShell,
    // Add other well-known logs as needed
}
