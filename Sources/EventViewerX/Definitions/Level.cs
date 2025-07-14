namespace EventViewerX;

/// <summary>
/// Common event severity levels.
/// </summary>
/// <para>
/// The numeric values correspond to the order used by Windows Event Log.
/// </para>
public enum Level {
    Verbose = 5,
    Informational = 4,
    Warning = 3,
    Error = 2,
    Critical = 1,
    LogAlways = 0
}