using System;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;

namespace EventViewerX.Reports;

/// <summary>
/// Engine-owned failure descriptor for stable machine-readable failure semantics.
/// </summary>
public sealed class EventViewerFailureDescriptor {
    /// <summary>
    /// Initializes a new instance of the <see cref="EventViewerFailureDescriptor"/> class.
    /// </summary>
    public EventViewerFailureDescriptor(string errorCode, string category, string entity, bool recoverable) {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "query_failed" : errorCode.Trim();
        Category = string.IsNullOrWhiteSpace(category) ? "query_failed" : category.Trim();
        Entity = string.IsNullOrWhiteSpace(entity) ? EventViewerFailureDescriptorResolver.DefaultEntity : entity.Trim();
        Recoverable = recoverable;
    }

    /// <summary>
    /// Stable error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Machine-readable category for recovery and planning.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Logical entity affected by the failure.
    /// </summary>
    public string Entity { get; }

    /// <summary>
    /// Whether the failure is expected to be recoverable in-session.
    /// </summary>
    public bool Recoverable { get; }
}

/// <summary>
/// Resolves EventViewerX failure kinds to stable engine-facing failure descriptors.
/// </summary>
public static class EventViewerFailureDescriptorResolver {
    /// <summary>
    /// Default failure entity used when no explicit entity is provided.
    /// </summary>
    public const string DefaultEntity = "event_log_query";

    // Recoverability policy:
    // - invalid_argument/access_denied/not_found => non-recoverable
    // - timeout/io_error/query_failed => recoverable
    private static readonly EventViewerFailureDescriptor InvalidArgumentDefault = new("invalid_argument", "invalid_argument", DefaultEntity, recoverable: false);
    private static readonly EventViewerFailureDescriptor AccessDeniedDefault = new("access_denied", "access_denied", DefaultEntity, recoverable: false);
    private static readonly EventViewerFailureDescriptor NotFoundDefault = new("not_found", "not_found", DefaultEntity, recoverable: false);
    private static readonly EventViewerFailureDescriptor TimeoutDefault = new("timeout", "timeout", DefaultEntity, recoverable: true);
    private static readonly EventViewerFailureDescriptor IoErrorDefault = new("io_error", "io_error", DefaultEntity, recoverable: true);
    private static readonly EventViewerFailureDescriptor QueryFailedDefault = new("query_failed", "query_failed", DefaultEntity, recoverable: true);

    /// <summary>
    /// Resolves EVTX query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(EvtxQueryFailureKind kind, string entity = DefaultEntity)
        => kind switch {
            EvtxQueryFailureKind.InvalidArgument => InvalidArgument(entity),
            EvtxQueryFailureKind.AccessDenied => AccessDenied(entity),
            EvtxQueryFailureKind.NotFound => NotFound(entity),
            EvtxQueryFailureKind.IoError => IoError(entity),
            EvtxQueryFailureKind.Exception => QueryFailed(entity),
            _ => QueryFailed(entity)
        };

    /// <summary>
    /// Resolves live-event query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(LiveEventQueryFailureKind kind, string entity = DefaultEntity)
        => kind switch {
            LiveEventQueryFailureKind.InvalidArgument => InvalidArgument(entity),
            LiveEventQueryFailureKind.AccessDenied => AccessDenied(entity),
            LiveEventQueryFailureKind.Timeout => Timeout(entity),
            LiveEventQueryFailureKind.Exception => QueryFailed(entity),
            _ => QueryFailed(entity)
        };

    /// <summary>
    /// Resolves live-stats query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(LiveStatsQueryFailureKind kind, string entity = DefaultEntity)
        => kind switch {
            LiveStatsQueryFailureKind.InvalidArgument => InvalidArgument(entity),
            LiveStatsQueryFailureKind.AccessDenied => AccessDenied(entity),
            LiveStatsQueryFailureKind.Timeout => Timeout(entity),
            LiveStatsQueryFailureKind.Exception => QueryFailed(entity),
            _ => QueryFailed(entity)
        };

    /// <summary>
    /// Resolves event-catalog query failure kind to a typed failure descriptor.
    /// </summary>
    public static EventViewerFailureDescriptor Resolve(EventCatalogFailureKind kind, string entity = DefaultEntity)
        => kind switch {
            EventCatalogFailureKind.InvalidArgument => InvalidArgument(entity),
            EventCatalogFailureKind.AccessDenied => AccessDenied(entity),
            EventCatalogFailureKind.Exception => QueryFailed(entity),
            _ => QueryFailed(entity)
        };

    private static EventViewerFailureDescriptor InvalidArgument(string entity) => Create(InvalidArgumentDefault, entity);
    private static EventViewerFailureDescriptor AccessDenied(string entity) => Create(AccessDeniedDefault, entity);
    private static EventViewerFailureDescriptor NotFound(string entity) => Create(NotFoundDefault, entity);
    private static EventViewerFailureDescriptor Timeout(string entity) => Create(TimeoutDefault, entity);
    private static EventViewerFailureDescriptor IoError(string entity) => Create(IoErrorDefault, entity);
    private static EventViewerFailureDescriptor QueryFailed(string entity) => Create(QueryFailedDefault, entity);

    private static EventViewerFailureDescriptor Create(EventViewerFailureDescriptor template, string entity) {
        var normalizedEntity = NormalizeEntity(entity);
        if (string.Equals(normalizedEntity, DefaultEntity, StringComparison.Ordinal)) {
            return template;
        }

        return new EventViewerFailureDescriptor(
            template.ErrorCode,
            template.Category,
            normalizedEntity,
            template.Recoverable);
    }

    private static string NormalizeEntity(string? entity)
        => string.IsNullOrWhiteSpace(entity) ? DefaultEntity : (entity?.Trim() ?? DefaultEntity);
}
