namespace EventViewerX;

/// <summary>Shared safety limits for Windows Event Log operations.</summary>
public static class EventLogLimits {
    /// <summary>
    /// Maximum number of independent event-log sources opened concurrently by
    /// the shared query, catalog, and named-event engines.
    /// </summary>
    public const int MaximumConcurrency = 64;
}
