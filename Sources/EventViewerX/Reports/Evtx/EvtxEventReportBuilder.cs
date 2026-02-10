using System;
using System.Collections.Generic;
using System.Threading;

namespace EventViewerX.Reports.Evtx;

/// <summary>
/// Builds typed EVTX event reports from <see cref="EvtxQueryRequest"/>.
/// </summary>
public static class EvtxEventReportBuilder {
    /// <summary>
    /// Reads an EVTX file and projects typed event rows for tool/report consumption.
    /// </summary>
    /// <param name="request">EVTX query request.</param>
    /// <param name="includeMessage">Whether to include formatted message text.</param>
    /// <param name="maxMessageChars">Maximum message length when included.</param>
    /// <param name="report">Projected event report on success.</param>
    /// <param name="failure">Failure payload on error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryBuild(
        EvtxQueryRequest request,
        bool includeMessage,
        int maxMessageChars,
        out EvtxEventReportResult report,
        out EvtxQueryFailure? failure,
        CancellationToken cancellationToken = default) {

        if (maxMessageChars < 0) {
            report = new EvtxEventReportResult();
            failure = new EvtxQueryFailure {
                Kind = EvtxQueryFailureKind.InvalidArgument,
                Message = "maxMessageChars must be greater than or equal to 0."
            };
            return false;
        }

        if (!EvtxQueryExecutor.TryRead(request, out var queried, out failure, cancellationToken)) {
            report = new EvtxEventReportResult();
            return false;
        }

        var rows = new List<EvtxEventReportRow>(queried.Events.Count);
        foreach (var ev in queried.Events) {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new EvtxEventReportRow {
                TimeCreatedUtc = ev.TimeCreated.ToUniversalTime().ToString("O"),
                Id = ev.Id,
                RecordId = ev.RecordId ?? 0,
                LogName = ev.LogName ?? string.Empty,
                ProviderName = ev.ProviderName ?? string.Empty,
                Level = (long)(ev.Level ?? 0),
                LevelDisplayName = ev.LevelDisplayName ?? string.Empty,
                ComputerName = ev.ComputerName ?? string.Empty,
                QueriedMachine = ev.QueriedMachine ?? string.Empty,
                GatheredFrom = ev.GatheredFrom ?? string.Empty,
                MessageSubject = ev.MessageSubject ?? string.Empty,
                UserSid = SafeGetUserSid(ev),
                Data = NormalizeDict(ev.Data),
                MessageData = NormalizeDict(ev.MessageData),
                Message = includeMessage ? TruncateSafe(SafeGetMessage(ev), maxMessageChars) : null
            });
        }

        var effectivePath = request?.FilePath ?? string.Empty;
        report = new EvtxEventReportResult {
            Path = effectivePath,
            Count = rows.Count,
            Truncated = request?.MaxEvents > 0 && rows.Count >= request.MaxEvents,
            Events = rows
        };
        failure = null;
        return true;
    }

    private static IReadOnlyDictionary<string, string> NormalizeDict(IReadOnlyDictionary<string, string>? dict) {
        if (dict is null || dict.Count == 0) {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, string>(dict.Count, StringComparer.Ordinal);
        foreach (var kvp in dict) {
            result[kvp.Key] = kvp.Value ?? string.Empty;
        }
        return result;
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
}
