using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using EventViewerX.Reports.Stats;

namespace EventViewerX.Reports.Live;

/// <summary>
/// Executes live event statistics queries using typed contracts.
/// </summary>
public static class LiveStatsQueryExecutor {
    /// <summary>
    /// Reads events from a live channel and produces aggregate statistics.
    /// </summary>
    /// <param name="request">Live stats request.</param>
    /// <param name="result">Result payload on success.</param>
    /// <param name="failure">Failure payload on error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
    public static bool TryBuild(
        LiveStatsQueryRequest request,
        out LiveStatsQueryResult result,
        out LiveStatsQueryFailure? failure,
        CancellationToken cancellationToken = default) {

        if (!TryValidateRequest(request, out result, out failure)) {
            return false;
        }

        var xpath = string.IsNullOrWhiteSpace(request.XPath) ? "*" : request.XPath!;
        var builder = new EvtxStatsReportBuilder();
        var scanned = 0;
        var matched = 0;
        DateTime? minUtc = null;
        DateTime? maxUtc = null;

        try {
            foreach (var ev in SearchEvents.QueryLogXPath(
                         logName: request.LogName,
                         xpath: xpath,
                         machineName: request.MachineName,
                         maxEvents: request.MaxEventsScanned,
                         oldest: request.OldestFirst,
                         cancellationToken: cancellationToken,
                         sessionTimeoutMs: request.SessionTimeoutMs)) {
                cancellationToken.ThrowIfCancellationRequested();

                scanned++;
                var createdUtc = ev.TimeCreated.ToUniversalTime();
                if (!IsWithinRange(createdUtc, request.StartTimeUtc, request.EndTimeUtc)) {
                    if (request.MaxEventsScanned > 0 && scanned >= request.MaxEventsScanned) {
                        break;
                    }
                    continue;
                }

                matched++;
                if (!minUtc.HasValue || createdUtc < minUtc.Value) {
                    minUtc = createdUtc;
                }
                if (!maxUtc.HasValue || createdUtc > maxUtc.Value) {
                    maxUtc = createdUtc;
                }

                builder.Add(ev);

                if (request.MaxEventsScanned > 0 && scanned >= request.MaxEventsScanned) {
                    break;
                }
            }

            result = new LiveStatsQueryResult {
                LogName = request.LogName,
                XPath = xpath,
                OldestFirst = request.OldestFirst,
                MaxEventsScanned = request.MaxEventsScanned,
                ScannedEvents = scanned,
                MatchedEvents = matched,
                Truncated = request.MaxEventsScanned > 0 && scanned >= request.MaxEventsScanned,
                TimeCreatedUtcMin = minUtc,
                TimeCreatedUtcMax = maxUtc,
                StartTimeUtc = request.StartTimeUtc,
                EndTimeUtc = request.EndTimeUtc,
                TopEventIds = builder.GetTopEventIds(request.TopEventIds)
                    .Select(static x => new TopEventIdRow { Id = x.Key, Count = x.Value })
                    .ToList(),
                TopProviders = builder.GetTopProviders(request.TopProviders)
                    .Select(static x => new TopProviderRow { ProviderName = x.Key, Count = x.Value })
                    .ToList(),
                TopComputers = builder.GetTopComputers(request.TopComputers)
                    .Select(static x => new TopComputerRow { ComputerName = x.Key, Count = x.Value })
                    .ToList(),
                TopLevels = builder.GetTopLevels(request.TopLevels)
            };
            failure = null;
            return true;
        } catch (OperationCanceledException) {
            throw;
        } catch (UnauthorizedAccessException ex) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.AccessDenied,
                Message = ex.Message
            };
            return false;
        } catch (TimeoutException ex) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.Timeout,
                Message = ex.Message
            };
            return false;
        } catch (EventLogException ex) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = IsTimeoutLike(ex.Message) ? LiveStatsQueryFailureKind.Timeout : LiveStatsQueryFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        } catch (ArgumentException ex) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = ex.Message
            };
            return false;
        } catch (Exception ex) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.Exception,
                Message = ex.Message
            };
            return false;
        }
    }

    private static bool TryValidateRequest(
        LiveStatsQueryRequest request,
        out LiveStatsQueryResult result,
        out LiveStatsQueryFailure? failure) {
        if (request is null) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "request is required."
            };
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.LogName)) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "logName is required."
            };
            return false;
        }

        if (request.MaxEventsScanned < 0) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "maxEventsScanned must be greater than or equal to 0."
            };
            return false;
        }

        if (request.StartTimeUtc.HasValue && request.EndTimeUtc.HasValue && request.StartTimeUtc.Value > request.EndTimeUtc.Value) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "startTimeUtc must be less than or equal to endTimeUtc."
            };
            return false;
        }

        if (request.TopEventIds < 0 || request.TopProviders < 0 || request.TopComputers < 0 || request.TopLevels < 0) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "Top values must be greater than or equal to 0."
            };
            return false;
        }

        if (request.SessionTimeoutMs.HasValue && request.SessionTimeoutMs.Value <= 0) {
            result = new LiveStatsQueryResult();
            failure = new LiveStatsQueryFailure {
                Kind = LiveStatsQueryFailureKind.InvalidArgument,
                Message = "sessionTimeoutMs must be positive when provided."
            };
            return false;
        }

        result = new LiveStatsQueryResult();
        failure = null;
        return true;
    }

    private static bool IsWithinRange(DateTime createdUtc, DateTime? startUtc, DateTime? endUtc) {
        if (startUtc.HasValue && createdUtc < startUtc.Value) {
            return false;
        }
        if (endUtc.HasValue && createdUtc > endUtc.Value) {
            return false;
        }
        return true;
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
