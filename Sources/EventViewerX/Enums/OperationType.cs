namespace EventViewerX;

/// <summary>
/// Describes the modification operation from event data.
/// </summary>
public enum OperationType {
    /// <summary>Value was added.</summary>
    ValueAdded,
    /// <summary>Value was deleted.</summary>
    ValueDeleted,
    /// <summary>Operation type is unknown.</summary>
    Unknown
}
