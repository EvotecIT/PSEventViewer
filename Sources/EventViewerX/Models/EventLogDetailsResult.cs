namespace EventViewerX;

/// <summary>
/// Status returned when querying event log details.
/// </summary>
public enum EventLogDetailsStatus {
    /// <summary>Event log details were collected successfully.</summary>
    Success,
    /// <summary>The query exceeded the caller-provided timeout.</summary>
    Timeout,
    /// <summary>An EventLogSession could not be opened for the target host.</summary>
    SessionUnavailable,
    /// <summary>The target event log channel could not be opened or does not exist.</summary>
    LogConfigurationUnavailable,
    /// <summary>The event log configuration was collected but runtime log information was unavailable.</summary>
    LogInformationUnavailable,
    /// <summary>An unexpected error occurred while collecting event log details.</summary>
    Error
}

/// <summary>
/// Result of querying event log details, including diagnostic status when details are unavailable.
/// </summary>
public sealed class EventLogDetailsResult {
    /// <summary>Event log channel name that was queried.</summary>
    public string LogName { get; set; } = string.Empty;

    /// <summary>Machine name used for the query.</summary>
    public string? MachineName { get; set; }

    /// <summary>Query status.</summary>
    public EventLogDetailsStatus Status { get; set; }

    /// <summary>True when details were collected successfully or partial details are available.</summary>
    public bool Success => Status == EventLogDetailsStatus.Success || Details != null;

    /// <summary>Collected event log details when available.</summary>
    public EventLogDetails? Details { get; set; }

    /// <summary>Diagnostic message when the query did not fully succeed.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Underlying exception type when available.</summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>Timeout budget used for the query, in milliseconds.</summary>
    public int TimeoutMs { get; set; }
}
