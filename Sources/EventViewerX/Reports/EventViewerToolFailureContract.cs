using System;
using EventViewerX.Reports.Evtx;
using EventViewerX.Reports.Inventory;
using EventViewerX.Reports.Live;

namespace EventViewerX.Reports;

/// <summary>
/// Engine-owned typed failure contract used by external tool wrappers.
/// </summary>
public sealed class EventViewerToolFailureContract {
    /// <summary>
    /// Stable tool error code aligned to host/tool envelope contracts.
    /// </summary>
    public string ErrorCode { get; set; } = "query_failed";

    /// <summary>
    /// Machine-readable category for recovery/planning.
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
/// Resolves EventViewerX failure kinds to stable tool-facing contracts.
/// </summary>
public static class EventViewerToolFailureContractResolver {
    /// <summary>
    /// Resolves EVTX query failure kind to a typed tool-facing contract.
    /// </summary>
    public static EventViewerToolFailureContract Resolve(EvtxQueryFailureKind kind)
        => ResolveCore(kind.ToString());

    /// <summary>
    /// Resolves live-event query failure kind to a typed tool-facing contract.
    /// </summary>
    public static EventViewerToolFailureContract Resolve(LiveEventQueryFailureKind kind)
        => ResolveCore(kind.ToString());

    /// <summary>
    /// Resolves live-stats query failure kind to a typed tool-facing contract.
    /// </summary>
    public static EventViewerToolFailureContract Resolve(LiveStatsQueryFailureKind kind)
        => ResolveCore(kind.ToString());

    /// <summary>
    /// Resolves event-catalog query failure kind to a typed tool-facing contract.
    /// </summary>
    public static EventViewerToolFailureContract Resolve(EventCatalogFailureKind kind)
        => ResolveCore(kind.ToString());

    private static EventViewerToolFailureContract ResolveCore(string? codeName) {
        var normalized = Normalize(codeName);
        return normalized switch {
            "invalidargument" or "invalidrequest" => new EventViewerToolFailureContract {
                ErrorCode = "invalid_argument",
                Category = "invalid_argument",
                Entity = "event_log_query",
                Recoverable = false
            },
            "accessdenied" => new EventViewerToolFailureContract {
                ErrorCode = "access_denied",
                Category = "access_denied",
                Entity = "event_log_query",
                Recoverable = false
            },
            "notfound" => new EventViewerToolFailureContract {
                ErrorCode = "not_found",
                Category = "not_found",
                Entity = "event_log_query",
                Recoverable = false
            },
            "timeout" or "timedout" => new EventViewerToolFailureContract {
                ErrorCode = "timeout",
                Category = "timeout",
                Entity = "event_log_query",
                Recoverable = true
            },
            "ioerror" or "iofailure" => new EventViewerToolFailureContract {
                ErrorCode = "io_error",
                Category = "io_error",
                Entity = "event_log_query",
                Recoverable = true
            },
            _ => new EventViewerToolFailureContract {
                ErrorCode = "query_failed",
                Category = "query_failed",
                Entity = "event_log_query",
                Recoverable = true
            }
        };
    }

    private static string Normalize(string? value) {
        if (value is null) {
            return string.Empty;
        }

        var chars = value.Trim();
        if (chars.Length == 0) {
            return string.Empty;
        }

        var buffer = new char[chars.Length];
        var j = 0;
        for (var i = 0; i < chars.Length; i++) {
            var c = chars[i];
            if (char.IsLetterOrDigit(c)) {
                buffer[j++] = char.ToLowerInvariant(c);
            }
        }

        return new string(buffer, 0, j);
    }
}
