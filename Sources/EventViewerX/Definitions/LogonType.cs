namespace EventViewerX;

/// <summary>
/// Enumerates possible Windows logon types.
/// </summary>
public enum LogonType {
    Interactive = 2,
    Network = 3,
    Batch = 4,
    Service = 5,
    Unlock = 7,
    NetworkCleartext = 8,
    NewCredentials = 9,
    RemoteInteractive = 10,
    CachedInteractive = 11
}
