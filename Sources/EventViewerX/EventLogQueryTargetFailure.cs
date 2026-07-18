namespace EventViewerX;

/// <summary>Describes an expected failure isolated to one remote event-log target.</summary>
public sealed class EventLogQueryTargetFailure {
    internal EventLogQueryTargetFailure(
        string machineName,
        EventLogRemoteQueryFailureKind kind,
        string message) {
        if (string.IsNullOrWhiteSpace(machineName)) {
            throw new ArgumentException("Remote target cannot be null or empty.", nameof(machineName));
        }
        if (kind == EventLogRemoteQueryFailureKind.None) {
            throw new ArgumentOutOfRangeException(nameof(kind), "A target failure must have a classified failure kind.");
        }

        MachineName = machineName.Trim();
        Kind = kind;
        Message = message ?? string.Empty;
    }

    /// <summary>Normalized remote machine name.</summary>
    public string MachineName { get; }

    /// <summary>Typed remote-target failure kind.</summary>
    public EventLogRemoteQueryFailureKind Kind { get; }

    /// <summary>Failure message returned by the event-log boundary.</summary>
    public string Message { get; }
}
