using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;

namespace EventViewerX.Reports;

/// <summary>
/// Engine-owned failure descriptor for stable machine-readable failure semantics.
/// </summary>
public sealed class EventViewerFailureDescriptor {
    /// <summary>
    /// Stable error code.
    /// </summary>
    public string ErrorCode { get; set; } = "query_failed";

    /// <summary>
    /// Machine-readable category for recovery and planning.
    /// </summary>
    public string Category { get; set; } = "query_failed";

    /// <summary>
    /// Logical entity affected by the failure.
    /// </summary>
    public string Entity { get; set; } = "event_log_query";

    /// <summary>
    /// Whether the failure is expected to be recoverable in-session.
    /// </summary>
    public bool Recoverable { get; set; }
}

/// <summary>
/// Resolves EventViewerX failure kinds to stable engine-facing failure descriptors.
/// </summary>
public static class EventViewerFailureDescriptorResolver {
    /// <summary>
    /// Resolves EVTX query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(EvtxQueryFailureKind kind)
        => kind switch {
            EvtxQueryFailureKind.InvalidArgument => InvalidArgument(),
            EvtxQueryFailureKind.AccessDenied => AccessDenied(),
            EvtxQueryFailureKind.NotFound => NotFound(),
            EvtxQueryFailureKind.IoError => IoError(),
            _ => QueryFailed()
        };

    /// <summary>
    /// Resolves live-event query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(LiveEventQueryFailureKind kind)
        => kind switch {
            LiveEventQueryFailureKind.InvalidArgument => InvalidArgument(),
            LiveEventQueryFailureKind.AccessDenied => AccessDenied(),
            LiveEventQueryFailureKind.Timeout => Timeout(),
            _ => QueryFailed()
        };

    /// <summary>
    /// Resolves live-stats query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(LiveStatsQueryFailureKind kind)
        => kind switch {
            LiveStatsQueryFailureKind.InvalidArgument => InvalidArgument(),
            LiveStatsQueryFailureKind.AccessDenied => AccessDenied(),
            LiveStatsQueryFailureKind.Timeout => Timeout(),
            _ => QueryFailed()
        };

    /// <summary>
    /// Resolves event-catalog query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(EventCatalogFailureKind kind)
        => kind switch {
            EventCatalogFailureKind.InvalidArgument => InvalidArgument(),
            EventCatalogFailureKind.AccessDenied => AccessDenied(),
            _ => QueryFailed()
        };

    private static EventViewerFailureDescriptor InvalidArgument()
        => new() {
            ErrorCode = "invalid_argument",
            Category = "invalid_argument",
            Entity = "event_log_query",
            Recoverable = false
        };

    private static EventViewerFailureDescriptor AccessDenied()
        => new() {
            ErrorCode = "access_denied",
            Category = "access_denied",
            Entity = "event_log_query",
            Recoverable = false
        };

    private static EventViewerFailureDescriptor NotFound()
        => new() {
            ErrorCode = "not_found",
            Category = "not_found",
            Entity = "event_log_query",
            Recoverable = false
        };

    private static EventViewerFailureDescriptor Timeout()
        => new() {
            ErrorCode = "timeout",
            Category = "timeout",
            Entity = "event_log_query",
            Recoverable = true
        };

    private static EventViewerFailureDescriptor IoError()
        => new() {
            ErrorCode = "io_error",
            Category = "io_error",
            Entity = "event_log_query",
            Recoverable = true
        };

    private static EventViewerFailureDescriptor QueryFailed()
        => new() {
            ErrorCode = "query_failed",
            Category = "query_failed",
            Entity = "event_log_query",
            Recoverable = true
        };
}
