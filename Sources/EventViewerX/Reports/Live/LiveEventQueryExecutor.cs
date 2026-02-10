using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace EventViewerX.Reports.Live;

/// <summary>
/// Executes live event channel queries using typed contracts.
/// </summary>
public static class LiveEventQueryExecutor {
    /// <summary>
    /// Reads events from a live event log channel.
    /// </summary>
    /// <param name="request">Live query request.</param>
    /// <param name="result">Result payload when successful.</param>
    /// <param name="failure">Failure payload when query fails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryRead(
        LiveEventQueryRequest request,
        out LiveEventQueryResult result,
        out LiveEventQueryFailure? failure,
        CancellationToken cancellationToken = default) {

        if (request is null) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = "request is required."
            };
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.LogName)) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = "logName is required."
            };
            return false;
        }

        if (request.MaxEvents < 0) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = "maxEvents must be greater than or equal to 0."
            };
            return false;
        }

        if (request.MaxMessageChars < 0) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = "maxMessageChars must be greater than or equal to 0."
            };
            return false;
        }

        if (request.SessionTimeoutMs.HasValue && request.SessionTimeoutMs.Value <= 0) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = "sessionTimeoutMs must be positive when provided."
            };
            return false;
        }

        var xpath = string.IsNullOrWhiteSpace(request.XPath) ? "*" : request.XPath!;

        try {
            var rows = new List<LiveEventRow>();

            foreach (var ev in SearchEvents.QueryLogXPath(
                         logName: request.LogName,
                         xpath: xpath,
                         machineName: request.MachineName,
                         maxEvents: request.MaxEvents,
                         oldest: request.OldestFirst,
                         cancellationToken: cancellationToken,
                         sessionTimeoutMs: request.SessionTimeoutMs)) {
                cancellationToken.ThrowIfCancellationRequested();

                rows.Add(new LiveEventRow {
                    TimeCreatedUtc = ev.TimeCreated.ToUniversalTime().ToString("O"),
                    Id = ev.Id,
                    RecordId = ev.RecordId ?? 0,
                    LogName = ev.LogName ?? string.Empty,
                    ProviderName = ev.ProviderName ?? string.Empty,
                    Level = (long)(ev.Level ?? 0),
                    LevelDisplayName = ev.LevelDisplayName ?? string.Empty,
                    Task = (long)(ev.Task ?? 0),
                    Opcode = (long)(ev.Opcode ?? 0),
                    Keywords = (long)(ev.Keywords ?? 0),
                    MachineName = ev.MachineName ?? string.Empty,
                    UserSid = SafeGetUserSid(ev),
                    Message = request.IncludeMessage
                        ? TruncateSafe(SafeGetMessage(ev), request.MaxMessageChars)
                        : null
                });
            }

            result = new LiveEventQueryResult {
                LogName = request.LogName,
                XPath = xpath,
                Count = rows.Count,
                Truncated = request.MaxEvents > 0 && rows.Count >= request.MaxEvents,
                Events = rows
            };
            failure = null;
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch (UnauthorizedAccessException ex) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.AccessDenied,
                Message = ex.Message
            };
            return false;
        } catch (TimeoutException ex) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.Timeout,
                Message = ex.Message
            };
            return false;
        } catch (EventLogException ex) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = IsTimeoutLike(ex.Message) ? LiveEventQueryFailureKind.Timeout : LiveEventQueryFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        } catch (ArgumentException ex) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.InvalidArgument,
                Message = ex.Message
            };
            return false;
        } catch (Exception ex) {
            result = new LiveEventQueryResult();
            failure = new LiveEventQueryFailure {
                Kind = LiveEventQueryFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        }
    }

    private static string SafeGetUserSid(EventObject ev) {
        try {
            return ev.UserId?.Value ?? string.Empty;
        } catch {
            return string.Empty;
        }
    }

    private static string SafeGetMessage(EventObject ev) {
        try {
            return ev.Message ?? string.Empty;
        } catch {
            return string.Empty;
        }
    }

    private static string TruncateSafe(string value, int maxChars) {
        if (maxChars <= 0 || string.IsNullOrEmpty(value)) {
            return string.Empty;
        }
        if (value.Length <= maxChars) {
            return value;
        }
        return value.Substring(0, maxChars);
    }

    private static bool IsTimeoutLike(string? message) {
        if (string.IsNullOrWhiteSpace(message)) {
            return false;
        }
        var text = message!;
        return text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
