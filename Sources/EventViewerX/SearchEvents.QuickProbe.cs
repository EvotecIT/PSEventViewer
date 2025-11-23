using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;

namespace EventViewerX;

/// <summary>
/// Fast, budgeted probe to fetch the newest matching event without scanning huge logs.
/// </summary>
public partial class SearchEvents : Settings
{
    /// <summary>
    /// Maximum time (ms) to wait on a single EventLogReader.ReadEvent call to prevent long RPC stalls.
    /// Tuned to keep overall probe time budgeted while still allowing slow-but-responding hosts.
    /// </summary>
    private const int QuickProbeReadTimeoutMs = 750;

    /// <summary>
    /// Minimum per-read budget (ms) to avoid overly aggressive time slices that would thrash on busy hosts.
    /// </summary>
    private const int QuickProbeMinPerReadMs = 200;
    private static int QuickProbePingMaxMs => Settings.PingTimeoutMs;

    /// <summary>Status of a quick probe.</summary>
    public enum QuickProbeStatus
    {
        /// <summary>Probe succeeded and returned a timestamp.</summary>
        Ok,
        /// <summary>No event matched the query within the scanned window.</summary>
        NoEvent,
        /// <summary>Overall probe timeout was hit.</summary>
        Timeout,
        /// <summary>Scan limit reached without finding a timestamped event.</summary>
        LimitReached,
        /// <summary>Probe failed due to an error (see message).</summary>
        Error
    }

    /// <summary>Outcome for a quick probe.</summary>
    public sealed class QuickProbeResult
    {
        /// <summary>Creates a quick probe result.</summary>
        /// <param name="logName">Log that was queried.</param>
        /// <param name="machine">Machine that was queried.</param>
        /// <param name="eventTimeUtc">Timestamp of the newest matching event in UTC, if found.</param>
        /// <param name="status">Outcome of the probe.</param>
        /// <param name="message">Optional detail describing errors or limits.</param>
        /// <param name="eventsScanned">Number of events inspected.</param>
        /// <param name="recordCount">Optional channel record count when available.</param>
        /// <param name="duration">Total probe duration.</param>
        public QuickProbeResult(string logName, string machine, DateTime? eventTimeUtc, QuickProbeStatus status, string? message, int eventsScanned, long? recordCount, TimeSpan duration)
        {
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
        /// <summary>Timestamp (UTC) of the newest matching event, if found.</summary>
        public DateTime? EventTimeUtc { get; }
        /// <summary>Outcome status for the probe.</summary>
        public QuickProbeStatus Status { get; }
        /// <summary>Optional details describing errors or limits.</summary>
        public string? Message { get; }
        /// <summary>Number of events inspected during the probe.</summary>
        public int EventsScanned { get; }
        /// <summary>Optional channel record count when available.</summary>
        public long? RecordCount { get; }
        /// <summary>Total elapsed time for the probe.</summary>
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// Reads the newest matching event from a channel with time/record limits to cope with very large logs.
    /// </summary>
    /// <param name="logName">Channel name to query.</param>
    /// <param name="xpathFilter">Optional XPath filter (defaults to '*').</param>
    /// <param name="machineName">Optional remote computer; null targets local machine.</param>
    /// <param name="timeout">Total time budget for the probe (default 15s).</param>
    /// <param name="maxEventsToScan">Maximum events to inspect before returning <see cref="QuickProbeStatus.LimitReached"/>.</param>
    /// <returns>Quick probe result with status and optional timestamp.</returns>
    public static QuickProbeResult ProbeLatestEvent(string logName, string? xpathFilter = null, string? machineName = null, TimeSpan? timeout = null, int maxEventsToScan = 4096)
    {
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));
        if (maxEventsToScan <= 0) maxEventsToScan = 4096;

        var sw = Stopwatch.StartNew();
        timeout ??= TimeSpan.FromSeconds(15);

        EventLogSession? session = null;
        try
        {
            var open = TryCreateSession(machineName, timeout.Value);
            if (open.Status != QuickProbeStatus.Ok)
            {
                return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, open.Status, open.Message, 0, null, sw.Elapsed);
            }

            session = open.Session;

            var query = new EventLogQuery(logName, PathType.LogName, string.IsNullOrWhiteSpace(xpathFilter) ? "*" : xpathFilter)
            {
                Session = session,
                ReverseDirection = true,
                TolerateQueryErrors = true
            };

            using var reader = new EventLogReader(query);
            int scanned = 0;
            while (true)
            {
                if (sw.Elapsed > timeout)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Timeout,
                        $"Timed out after {timeout.Value.TotalMilliseconds:F0} ms", scanned, null, sw.Elapsed);
                }

                // Bound each ReadEvent call so RPC stalls can't exceed the overall budget
                TimeSpan perReadBudget = timeout.Value - sw.Elapsed;
                if (perReadBudget < TimeSpan.FromMilliseconds(QuickProbeMinPerReadMs)) perReadBudget = TimeSpan.FromMilliseconds(QuickProbeMinPerReadMs);

                EventRecord? rec = null;
                try
                {
                    // Synchronous bounded read avoids runaway Task.Run that could keep RPC handles alive.
                    var readWindow = perReadBudget < TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs)
                        ? perReadBudget
                        : TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs);

                    rec = reader.ReadEvent(readWindow);
                    if (rec == null)
                    {
                        return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Timeout,
                            $"Timed out after {timeout.Value.TotalMilliseconds:F0} ms", scanned, null, sw.Elapsed);
                    }
                }
                catch (EventLogException ex)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Error, ex.Message, scanned, null, sw.Elapsed);
                }

                scanned++;
                DateTime? created = rec.TimeCreated?.ToUniversalTime();
                rec.Dispose();

                if (created.HasValue)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), created, QuickProbeStatus.Ok, null, scanned, null, sw.Elapsed);
                }

                if (scanned >= maxEventsToScan)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.LimitReached,
                        $"Scanned {scanned} events without timestamp (limit {maxEventsToScan}).", scanned, null, sw.Elapsed);
                }
            }
        }
        catch (Exception ex)
        {
            return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Error, ex.Message, 0, null, sw.Elapsed);
        }
        finally
        {
            session?.Dispose();
        }
    }

    /// <summary>
    /// Overload that reuses an existing session (caller owns its lifetime).
    /// </summary>
    /// <param name="logName">Channel name to query.</param>
    /// <param name="xpathFilter">Optional XPath filter (defaults to '*').</param>
    /// <param name="session">Existing EventLogSession to reuse.</param>
    /// <param name="machineName">Optional remote computer; null targets local machine.</param>
    /// <param name="timeout">Total time budget for the probe (default 15s).</param>
    /// <param name="maxEventsToScan">Maximum events to inspect before returning <see cref="QuickProbeStatus.LimitReached"/>.</param>
    /// <returns>Quick probe result with status and optional timestamp.</returns>
    public static QuickProbeResult ProbeLatestEvent(string logName, string? xpathFilter, EventLogSession session, string? machineName = null, TimeSpan? timeout = null, int maxEventsToScan = 4096)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (string.IsNullOrWhiteSpace(logName)) throw new ArgumentException("logName cannot be null or empty", nameof(logName));
        if (maxEventsToScan <= 0) maxEventsToScan = 4096;

        var sw = Stopwatch.StartNew();
        timeout ??= TimeSpan.FromSeconds(15);

        try
        {
            var query = new EventLogQuery(logName, PathType.LogName, string.IsNullOrWhiteSpace(xpathFilter) ? "*" : xpathFilter)
            {
                Session = session,
                ReverseDirection = true,
                TolerateQueryErrors = true
            };

            using var reader = new EventLogReader(query);
            int scanned = 0;
            while (true)
            {
                if (sw.Elapsed > timeout)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Timeout,
                        $"Timed out after {timeout.Value.TotalMilliseconds:F0} ms", scanned, null, sw.Elapsed);
                }

                TimeSpan perReadBudget = timeout.Value - sw.Elapsed;
                if (perReadBudget < TimeSpan.FromMilliseconds(QuickProbeMinPerReadMs)) perReadBudget = TimeSpan.FromMilliseconds(QuickProbeMinPerReadMs);

                EventRecord? rec = null;
                try
                {
                    var readWindow = perReadBudget < TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs)
                        ? perReadBudget
                        : TimeSpan.FromMilliseconds(QuickProbeReadTimeoutMs);

                    rec = reader.ReadEvent(readWindow);
                    if (rec == null)
                    {
                        return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Timeout,
                            $"Timed out after {timeout.Value.TotalMilliseconds:F0} ms", scanned, null, sw.Elapsed);
                    }
                }
                catch (EventLogException ex)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Error, ex.Message, scanned, null, sw.Elapsed);
                }

                scanned++;
                DateTime? created = rec.TimeCreated?.ToUniversalTime();
                rec.Dispose();

                if (created.HasValue)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), created, QuickProbeStatus.Ok, null, scanned, null, sw.Elapsed);
                }

                if (scanned >= maxEventsToScan)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.LimitReached,
                        $"Scanned {scanned} events without timestamp (limit {maxEventsToScan}).", scanned, null, sw.Elapsed);
                }
            }
        }
        catch (Exception ex)
        {
            return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.Error, ex.Message, 0, null, sw.Elapsed);
        }
    }

    private static (EventLogSession? Session, QuickProbeStatus Status, string? Message) TryCreateSession(string? machineName, TimeSpan budget)
    {
        // Local: avoid extra work and RPC/ping probes
        if (IsLocalMachine(machineName))
        {
            try { return (new EventLogSession(), QuickProbeStatus.Ok, null); }
            catch (Exception ex) { return (null, QuickProbeStatus.Error, ex.Message); }
        }

        int budgetMs = (int)Math.Max(500, budget.TotalMilliseconds);

        // Shared preflight with negative cache + RPC probe
        var preflight = Preflight(machineName, budgetMs);
        if (preflight.Status != QuickProbeStatus.Ok)
        {
            return (null, preflight.Status, preflight.Message);
        }

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(budget);
            var task = Task.Run(() => new EventLogSession(machineName), cts.Token);
            var completed = Task.WhenAny(task, Task.Delay(budget, cts.Token)).GetAwaiter().GetResult();
            if (completed != task)
            {
                // Ensure the eventual session is disposed when it completes to avoid lingering handles.
                task.ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion && t.Result != null)
                    {
                        t.Result.Dispose();
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

                return (null, QuickProbeStatus.Timeout, $"Session open timed out after {budget.TotalMilliseconds:F0} ms");
            }

            return (task.GetAwaiter().GetResult(), QuickProbeStatus.Ok, null);
        }
        catch (EventLogException ex)
        {
            return (null, QuickProbeStatus.Error, ex.Message);
        }
        catch (Exception ex)
        {
            return (null, QuickProbeStatus.Error, ex.Message);
        }
    }

    private static (QuickProbeStatus Status, string? Message) Preflight(string host, int budgetMs)
    {
        try
        {
            if (IsHostNegativeCached(host))
            {
                return (QuickProbeStatus.Error, "Cached unreachable");
            }

            // Ping
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var pingTimeout = Math.Min(QuickProbePingMaxMs, budgetMs / 2);
                var reply = ping.Send(host, pingTimeout);
                if (reply == null || reply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    MarkHostUnreachable(host);
                    return (QuickProbeStatus.Error, "Ping failed");
                }
            }
            catch (Exception ex)
            {
                Settings._logger.WriteVerbose($"Preflight: ping failed for '{host}': {ex.Message}");
            }

            // RPC probe
            if (!RpcProbe(host, Math.Min(DefaultRpcProbeTimeoutMs, budgetMs)))
            {
                MarkHostUnreachable(host);
                return (QuickProbeStatus.Error, "RPC probe failed");
            }

            ClearNegativeCache(host);
            return (QuickProbeStatus.Ok, null);
        }
        catch (Exception ex)
        {
            return (QuickProbeStatus.Error, ex.Message);
        }
    }
}
