using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using EventViewerX.Reports.QueryHelpers;

namespace EventViewerX;

/// <summary>
/// Fast, budgeted probe to fetch the newest matching event without scanning a large log.
/// </summary>
public partial class SearchEvents : Settings {
    private const int QuickProbeReadTimeoutMs = 750;

    /// <summary>Status of a quick probe.</summary>
    public enum QuickProbeStatus {
        /// <summary>Probe succeeded and returned a timestamp.</summary>
        Ok,
        /// <summary>No event matched the query.</summary>
        NoEvent,
        /// <summary>Overall probe timeout was hit.</summary>
        Timeout,
        /// <summary>Scan limit reached without finding a timestamped event.</summary>
        LimitReached,
        /// <summary>The caller does not have permission to read the target.</summary>
        AccessDenied,
        /// <summary>The requested event log does not exist.</summary>
        LogNotFound,
        /// <summary>The supplied XPath query is invalid.</summary>
        InvalidQuery,
        /// <summary>The target host or Event Log RPC endpoint is unavailable.</summary>
        HostUnavailable,
        /// <summary>Probe failed due to another error.</summary>
        Error
    }

    /// <summary>Outcome for a quick probe.</summary>
    public sealed class QuickProbeResult {
        /// <summary>Creates a quick probe result.</summary>
        public QuickProbeResult(string logName, string machine, DateTime? eventTimeUtc, QuickProbeStatus status, string? message, int eventsScanned, long? recordCount, TimeSpan duration) {
            LogName = logName;
            Machine = machine;
            EventTimeUtc = eventTimeUtc;
            Status = status;
            Message = message;
            EventsScanned = eventsScanned;
            RecordCount = recordCount;
            Duration = duration;
        }

        /// <summary>Log that was queried.</summary>
        public string LogName { get; }
        /// <summary>Machine that was queried.</summary>
        public string Machine { get; }
        /// <summary>Timestamp of the newest matching event in UTC.</summary>
        public DateTime? EventTimeUtc { get; }
        /// <summary>Outcome status.</summary>
        public QuickProbeStatus Status { get; }
        /// <summary>Optional diagnostic message.</summary>
        public string? Message { get; }
        /// <summary>Number of events inspected.</summary>
        public int EventsScanned { get; }
        /// <summary>Channel record count when available.</summary>
        public long? RecordCount { get; }
        /// <summary>Total elapsed time.</summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// Reads the newest matching event from a local or remote channel within a bounded time budget.
    /// </summary>
    public static QuickProbeResult ProbeLatestEvent(
        string logName,
        string? xpathFilter = null,
        string? machineName = null,
        TimeSpan? timeout = null,
        int maxEventsToScan = 4096) {

        ValidateQuickProbeArguments(logName, timeout, maxEventsToScan);
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var stopwatch = Stopwatch.StartNew();
        using EventLogSessionOpenResult sessionResult = CreateSessionResult(
            machineName,
            "QuickProbe",
            logName,
            (int)Math.Min(int.MaxValue, Math.Max(1, effectiveTimeout.TotalMilliseconds)));
        if (!sessionResult.Success || sessionResult.Session == null) {
            return new QuickProbeResult(
                logName,
                ResolveProbeTarget(machineName),
                null,
                MapSessionProbeStatus(sessionResult.Status),
                sessionResult.ErrorMessage,
                0,
                null,
                stopwatch.Elapsed);
        }

        return ProbeLatestEventCore(logName, xpathFilter, sessionResult.Session, machineName, effectiveTimeout, maxEventsToScan, stopwatch);
    }

    /// <summary>
    /// Reads the newest matching event using an existing session owned by the caller.
    /// </summary>
    public static QuickProbeResult ProbeLatestEvent(
        string logName,
        string? xpathFilter,
        EventLogSession session,
        string? machineName = null,
        TimeSpan? timeout = null,
        int maxEventsToScan = 4096) {

        if (session == null) {
            throw new ArgumentNullException(nameof(session));
        }
        ValidateQuickProbeArguments(logName, timeout, maxEventsToScan);
        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        return ProbeLatestEventCore(logName, xpathFilter, session, machineName, effectiveTimeout, maxEventsToScan, Stopwatch.StartNew());
    }

    private static QuickProbeResult ProbeLatestEventCore(
        string logName,
        string? xpathFilter,
        EventLogSession session,
        string? machineName,
        TimeSpan timeout,
        int maxEventsToScan,
        Stopwatch stopwatch) {

        string target = ResolveProbeTarget(machineName);
        long? recordCount = null;
        int scanned = 0;
        try {
            TimeSpan remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero) {
                return ProbeFailure(logName, target, QuickProbeStatus.Timeout, $"Timed out after {timeout.TotalMilliseconds:F0} ms.", 0, null, stopwatch.Elapsed);
            }

            int operationBudgetMs = (int)Math.Min(int.MaxValue, Math.Max(1, remaining.TotalMilliseconds));
            recordCount = ExecuteWithTimeout(
                () => session.GetLogInformation(logName, PathType.LogName),
                operationBudgetMs,
                $"Timed out reading log information for '{logName}' on '{target}' after {operationBudgetMs} ms.").RecordCount;
            var query = new EventLogQuery(logName, PathType.LogName, string.IsNullOrWhiteSpace(xpathFilter) ? "*" : xpathFilter) {
                Session = session,
                ReverseDirection = true,
                TolerateQueryErrors = false
            };

            remaining = timeout - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero) {
                return ProbeFailure(logName, target, QuickProbeStatus.Timeout, $"Timed out after {timeout.TotalMilliseconds:F0} ms.", 0, recordCount, stopwatch.Elapsed);
            }
            operationBudgetMs = (int)Math.Min(int.MaxValue, Math.Max(1, remaining.TotalMilliseconds));
            using var reader = CreateEventLogReader(query, machineName, operationBudgetMs);
            while (scanned < maxEventsToScan) {
                remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero) {
                    return ProbeFailure(logName, target, QuickProbeStatus.Timeout, $"Timed out after {timeout.TotalMilliseconds:F0} ms.", scanned, recordCount, stopwatch.Elapsed);
                }

                TimeSpan readWindow = remaining < TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs)
                    ? remaining
                    : TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs);
                var readStopwatch = Stopwatch.StartNew();
                EventRecord? record = reader.ReadEvent(readWindow);
                if (record == null) {
                    bool exhaustedReadWindow = readStopwatch.Elapsed >= TimeSpan.FromTicks((long)(readWindow.Ticks * 0.9));
                    return ProbeFailure(
                        logName,
                        target,
                        exhaustedReadWindow ? QuickProbeStatus.Timeout : QuickProbeStatus.NoEvent,
                        exhaustedReadWindow ? $"The event read exceeded its {readWindow.TotalMilliseconds:F0} ms window." : "No event matched the query.",
                        scanned,
                        recordCount,
                        stopwatch.Elapsed);
                }

                DateTime? created;
                using (record) {
                    scanned++;
                    created = record.TimeCreated?.ToUniversalTime();
                }
                if (created.HasValue) {
                    return new QuickProbeResult(logName, target, created, QuickProbeStatus.Ok, null, scanned, recordCount, stopwatch.Elapsed);
                }
            }

            return ProbeFailure(logName, target, QuickProbeStatus.LimitReached, $"Scanned {maxEventsToScan} events without a timestamp.", maxEventsToScan, recordCount, stopwatch.Elapsed);
        } catch (UnauthorizedAccessException ex) {
            return ProbeFailure(logName, target, QuickProbeStatus.AccessDenied, ex.Message, scanned, recordCount, stopwatch.Elapsed);
        } catch (TimeoutException ex) {
            return ProbeFailure(logName, target, QuickProbeStatus.Timeout, ex.Message, scanned, recordCount, stopwatch.Elapsed);
        } catch (EventLogNotFoundException ex) {
            return ProbeFailure(logName, target, QuickProbeStatus.LogNotFound, ex.Message, scanned, recordCount, stopwatch.Elapsed);
        } catch (EventLogException ex) {
            QuickProbeStatus status = QueryFailureHelpers.IsInvalidEventQuery(ex) ? QuickProbeStatus.InvalidQuery : QuickProbeStatus.Error;
            return ProbeFailure(logName, target, status, ex.Message, scanned, recordCount, stopwatch.Elapsed);
        } catch (Exception ex) {
            return ProbeFailure(logName, target, QuickProbeStatus.Error, ex.Message, scanned, recordCount, stopwatch.Elapsed);
        }
    }

    private static void ValidateQuickProbeArguments(string logName, TimeSpan? timeout, int maxEventsToScan) {
        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException("Log name cannot be null or whitespace.", nameof(logName));
        }
        if (timeout.HasValue && timeout.Value <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive when provided.");
        }
        if (maxEventsToScan <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxEventsToScan), "Maximum events to scan must be positive.");
        }
    }

    internal static QuickProbeStatus MapSessionProbeStatus(EventLogSessionOpenStatus status) {
        return status switch {
            EventLogSessionOpenStatus.AccessDenied => QuickProbeStatus.AccessDenied,
            EventLogSessionOpenStatus.Timeout => QuickProbeStatus.Timeout,
            EventLogSessionOpenStatus.NegativeCache => QuickProbeStatus.HostUnavailable,
            EventLogSessionOpenStatus.RpcUnavailable => QuickProbeStatus.HostUnavailable,
            EventLogSessionOpenStatus.EventLogSessionUnavailable => QuickProbeStatus.HostUnavailable,
            _ => QuickProbeStatus.Error
        };
    }

    private static string ResolveProbeTarget(string? machineName) {
        return string.IsNullOrWhiteSpace(machineName) ? GetFQDN() : machineName!.Trim();
    }

    private static QuickProbeResult ProbeFailure(
        string logName,
        string target,
        QuickProbeStatus status,
        string message,
        int scanned,
        long? recordCount,
        TimeSpan duration) {

        return new QuickProbeResult(logName, target, null, status, message, scanned, recordCount, duration);
    }
}
