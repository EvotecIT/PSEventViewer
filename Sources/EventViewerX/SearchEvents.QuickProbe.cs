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
    private const int QuickProbeReadTimeoutMs = 750;
    private const int QuickProbeMinPerReadMs = 200;

    /// <summary>Status of a quick probe.</summary>
    public enum QuickProbeStatus
    {
        Ok,
        NoEvent,
        Timeout,
        LimitReached,
        Error
    }

    /// <summary>Outcome for a quick probe.</summary>
    public sealed class QuickProbeResult
    {
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

        public string LogName { get; }
        public string Machine { get; }
        public DateTime? EventTimeUtc { get; }
        public QuickProbeStatus Status { get; }
        public string? Message { get; }
        public int EventsScanned { get; }
        public long? RecordCount { get; }
        public TimeSpan Duration { get; }
    }

    /// <summary>
    /// Reads the newest matching event from a channel with time/record limits to cope with very large logs.
    /// </summary>
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

                if (rec == null)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.NoEvent,
                        "No matching events returned.", scanned, null, sw.Elapsed);
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

                if (rec == null)
                {
                    return new QuickProbeResult(logName, machineName ?? GetFQDN(), null, QuickProbeStatus.NoEvent,
                        "No matching events returned.", scanned, null, sw.Elapsed);
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
        // Local: avoid extra work
        if (string.IsNullOrWhiteSpace(machineName))
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
            var task = Task.Run(() => new EventLogSession(machineName));
            var completed = Task.WhenAny(task, Task.Delay(budget)).GetAwaiter().GetResult();
            if (completed != task)
            {
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
                var reply = ping.Send(host, Math.Min(1000, budgetMs / 2));
                if (reply == null || reply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    MarkHostUnreachable(host);
                    return (QuickProbeStatus.Error, "Ping failed");
                }
            }
            catch { /* ignore ping errors */ }

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
