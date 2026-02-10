using System;
using System.Collections.Generic;

namespace EventViewerX.Reports.Live;

/// <summary>
/// Query contract for reading events from a live event log channel.
/// </summary>
public sealed class LiveEventQueryRequest {
    /// <summary>
    /// Log name (for example <c>Security</c>, <c>System</c>, <c>Application</c>).
    /// </summary>
    public string LogName { get; set; } = string.Empty;

    /// <summary>
    /// Optional XPath query. Defaults to <c>*</c>.
    /// </summary>
    public string? XPath { get; set; }

    /// <summary>
    /// Optional target machine name. Null/empty targets local machine.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Maximum events to return. Use 0 for unlimited.
    /// </summary>
    public int MaxEvents { get; set; }

    /// <summary>
    /// Read direction. When true, reads oldest to newest.
    /// </summary>
    public bool OldestFirst { get; set; }

    /// <summary>
    /// When true, includes formatted event message text.
    /// </summary>
    public bool IncludeMessage { get; set; }

    /// <summary>
    /// Maximum characters kept in formatted message text.
    /// </summary>
    public int MaxMessageChars { get; set; } = 4000;

    /// <summary>
    /// Optional session timeout override in milliseconds.
    /// </summary>
    public int? SessionTimeoutMs { get; set; }
}

/// <summary>
/// Canonical failure kinds produced by live event queries.
/// </summary>
public enum LiveEventQueryFailureKind {
    /// <summary>
    /// Invalid request arguments.
    /// </summary>
    InvalidArgument,

    /// <summary>
    /// Access to event logs was denied.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Event log session or read timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Unexpected failure.
    /// </summary>
    Exception
}

/// <summary>
/// Failure payload produced by live event queries.
/// </summary>
public sealed class LiveEventQueryFailure {
    /// <summary>
    /// Gets or sets failure kind.
    /// </summary>
    public LiveEventQueryFailureKind Kind { get; set; }

    /// <summary>
    /// Gets or sets failure message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Single event row returned by live event queries.
/// </summary>
public sealed class LiveEventRow {
    /// <summary>
    /// Event creation time in UTC (ISO-8601).
    /// </summary>
    public string TimeCreatedUtc { get; set; } = string.Empty;

    /// <summary>
    /// Event ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Event record ID.
    /// </summary>
    public long RecordId { get; set; }

    /// <summary>
    /// Source log name.
    /// </summary>
    public string LogName { get; set; } = string.Empty;

    /// <summary>
    /// Provider name.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Numeric event level.
    /// </summary>
    public long Level { get; set; }

    /// <summary>
    /// Localized level name.
    /// </summary>
    public string LevelDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Task value.
    /// </summary>
    public long Task { get; set; }

    /// <summary>
    /// Opcode value.
    /// </summary>
    public long Opcode { get; set; }

    /// <summary>
    /// Keywords bitmask value.
    /// </summary>
    public long Keywords { get; set; }

    /// <summary>
    /// Computer name.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// User SID value.
    /// </summary>
    public string UserSid { get; set; } = string.Empty;

    /// <summary>
    /// Optional formatted event message.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Query result for live event reads.
/// </summary>
public sealed class LiveEventQueryResult {
    /// <summary>
    /// Queried log name.
    /// </summary>
    public string LogName { get; set; } = string.Empty;

    /// <summary>
    /// Effective XPath query.
    /// </summary>
    public string XPath { get; set; } = string.Empty;

    /// <summary>
    /// Number of returned events.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Indicates whether output was truncated to the request cap.
    /// </summary>
    public bool Truncated { get; set; }

    /// <summary>
    /// Returned event rows.
    /// </summary>
    public IReadOnlyList<LiveEventRow> Events { get; set; } = Array.Empty<LiveEventRow>();
}
