using System.ComponentModel;
using System.Diagnostics;
using System.Net;

namespace EventViewerX;

/// <summary>
/// Fast, budgeted health probe over the same owned native reader used by
/// <see cref="EventLogEngine"/>.
/// </summary>
public static class EventLogProbe {
    private const int QuickProbeReadTimeoutMs = 750;
    private const int ErrorEvtInvalidChannelPath = 15000;
    private const int ErrorEvtInvalidQuery = 15001;
    private const int ErrorEvtChannelNotFound = 15007;
    private const int ErrorEvtMalformedXmlText = 15008;

    /// <summary>
    /// Reads the newest matching event from a local or remote channel within a
    /// bounded time budget using the owned native engine.
    /// </summary>
    public static EventLogProbeResult ProbeLatestEvent(
        string logName,
        string? xpathFilter = null,
        string? machineName = null,
        TimeSpan? timeout = null,
        int maxEventsToScan = 4096,
        NetworkCredential? credential = null,
        EventLogAuthentication authentication =
            EventLogAuthentication.Default,
        CancellationToken cancellationToken = default) {

        ValidateArguments(
            logName,
            timeout,
            maxEventsToScan);
        cancellationToken.ThrowIfCancellationRequested();
        if (EventLogTarget.IsLocalMachine(
                machineName) &&
            credential != null) {
            throw new ArgumentException(
                "Credentials can only be used with a remote event-log probe.",
                nameof(credential));
        }

        TimeSpan effectiveTimeout =
            timeout ??
            TimeSpan.FromSeconds(15);
        int timeoutMilliseconds =
            checked((int)Math.Min(
                int.MaxValue,
                Math.Max(
                    1,
                    effectiveTimeout.TotalMilliseconds)));
        var stopwatch = Stopwatch.StartNew();
        string target =
            ResolveProbeTarget(machineName);
        int scanned = 0;
        long? recordCount = null;

        using var probeCancellation =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);
        probeCancellation.CancelAfter(
            effectiveTimeout);
        try {
            var query =
                new EventLogChannelQuery(logName) {
                    MachineName = machineName,
                    Credential = credential,
                    Authentication =
                        authentication,
                    XPath =
                        string.IsNullOrWhiteSpace(
                            xpathFilter)
                            ? "*"
                            : xpathFilter!,
                    Oldest = false,
                    ReadMode =
                        EventReadMode.Metadata,
                    MaxEvents = maxEventsToScan,
                    RemoteConnectionTimeoutMilliseconds =
                        timeoutMilliseconds,
                    RemoteReadTimeoutMilliseconds =
                        Math.Min(
                            timeoutMilliseconds,
                            QuickProbeReadTimeoutMs),
                    BufferCapacity = 1,
                    RpcEndpointPort =
                        Settings.RpcProbePort
                };

            DateTime? eventTimeUtc =
                FindFirstUsableTimestampUtc(
                    EventLogEngine.ReadChannel(
                        query,
                        probeCancellation.Token),
                    out scanned);

            recordCount = RunCancelableProbeStage(
                () => TryReadRecordCount(
                    logName,
                    machineName,
                    credential,
                    authentication,
                    effectiveTimeout -
                    stopwatch.Elapsed),
                probeCancellation.Token);
            if (eventTimeUtc.HasValue) {
                return new EventLogProbeResult(
                    logName,
                    target,
                    eventTimeUtc.Value,
                    EventLogProbeStatus.Ok,
                    null,
                    scanned,
                    recordCount,
                    stopwatch.Elapsed,
                    nativeQueryVerified: true);
            }
            return ProbeFailure(
                logName,
                target,
                scanned == 0
                    ? EventLogProbeStatus.NoEvent
                    : EventLogProbeStatus.LimitReached,
                scanned == 0
                    ? "No event matched the native query."
                    : $"The first {scanned} matching events did not contain a usable timestamp.",
                scanned,
                recordCount,
                stopwatch.Elapsed,
                nativeQueryVerified: true);
        } catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested) {
            throw;
        } catch (OperationCanceledException) {
            return ProbeFailure(
                logName,
                target,
                EventLogProbeStatus.Timeout,
                $"Timed out after {effectiveTimeout.TotalMilliseconds:F0} ms.",
                scanned,
                recordCount,
                stopwatch.Elapsed);
        } catch (Exception exception) {
            EventLogProbeStatus status =
                ClassifyFailure(
                    machineName,
                    exception);
            return ProbeFailure(
                logName,
                target,
                status,
                exception.Message,
                scanned,
                recordCount,
                stopwatch.Elapsed);
        }
    }

    internal static DateTime? FindFirstUsableTimestampUtc(
        IEnumerable<EventObject> events,
        out int scanned) {

        scanned = 0;
        foreach (EventObject eventObject in events) {
            scanned++;
            if (eventObject.TimeCreated ==
                DateTime.MinValue) {
                continue;
            }
            return eventObject.TimeCreated
                .ToUniversalTime();
        }
        return null;
    }

    internal static T RunCancelableProbeStage<T>(
        Func<T> stage,
        CancellationToken cancellationToken) {

        if (stage == null) {
            throw new ArgumentNullException(
                nameof(stage));
        }
        cancellationToken.ThrowIfCancellationRequested();
        T result = stage();
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static long? TryReadRecordCount(
        string logName,
        string? machineName,
        NetworkCredential? credential,
        EventLogAuthentication authentication,
        TimeSpan remaining) {

        if (remaining <= TimeSpan.Zero) {
            return null;
        }
        int budget = checked((int)Math.Min(
            int.MaxValue,
            Math.Max(
                1,
                remaining.TotalMilliseconds)));
        try {
            EventLogDetailsResult? result =
                EventLogCatalog
                    .DisplayEventLogResults(
                        new[] { logName },
                        machineName,
                        budget,
                        includeEventTimes: false,
                        credential,
                        authentication)
                    .SingleOrDefault();
            return result?.Details?.RecordCount;
        } catch {
            return null;
        }
    }

    private static EventLogProbeStatus ClassifyFailure(
        string? machineName,
        Exception exception) {

        if (exception is Win32Exception win32) {
            return win32.NativeErrorCode switch {
                ErrorEvtInvalidChannelPath or
                ErrorEvtChannelNotFound =>
                    EventLogProbeStatus.LogNotFound,
                ErrorEvtInvalidQuery or
                ErrorEvtMalformedXmlText =>
                    EventLogProbeStatus.InvalidQuery,
                5 => EventLogProbeStatus.AccessDenied,
                121 or 1460 =>
                    EventLogProbeStatus.Timeout,
                53 or 64 or 1231 or 1722 or
                1726 or 1818 =>
                    EventLogProbeStatus.HostUnavailable,
                _ => EventLogProbeStatus.Error
            };
        }
        if (EventLogRemoteQueryFailureClassifier
            .TryClassify(
                machineName,
                exception,
                out EventLogRemoteQueryFailureKind kind)) {
            return kind switch {
                EventLogRemoteQueryFailureKind.AccessDenied =>
                    EventLogProbeStatus.AccessDenied,
                EventLogRemoteQueryFailureKind.Timeout =>
                    EventLogProbeStatus.Timeout,
                EventLogRemoteQueryFailureKind.HostUnavailable =>
                    EventLogProbeStatus.HostUnavailable,
                _ => EventLogProbeStatus.Error
            };
        }
        return exception switch {
            UnauthorizedAccessException =>
                EventLogProbeStatus.AccessDenied,
            TimeoutException =>
                EventLogProbeStatus.Timeout,
            _ => EventLogProbeStatus.Error
        };
    }

    private static void ValidateArguments(
        string logName,
        TimeSpan? timeout,
        int maxEventsToScan) {

        if (string.IsNullOrWhiteSpace(
                logName)) {
            throw new ArgumentException(
                "Log name cannot be null or whitespace.",
                nameof(logName));
        }
        if (timeout.HasValue &&
            timeout.Value <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Timeout must be positive when provided.");
        }
        if (maxEventsToScan <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(maxEventsToScan),
                "Maximum events to scan must be positive.");
        }
    }

    private static string ResolveProbeTarget(
        string? machineName) {

        return EventLogTarget.IsLocalMachine(
            machineName)
            ? EventLogTarget.LocalMachineName
            : machineName!.Trim();
    }

    private static EventLogProbeResult ProbeFailure(
        string logName,
        string target,
        EventLogProbeStatus status,
        string message,
        int scanned,
        long? recordCount,
        TimeSpan duration,
        bool nativeQueryVerified = false) {

        return new EventLogProbeResult(
            logName,
            target,
            null,
            status,
            message,
            scanned,
            recordCount,
            duration,
            nativeQueryVerified);
    }
}
