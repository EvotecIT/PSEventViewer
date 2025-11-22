using System;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EventViewerX;

public partial class SearchEvents : Settings
{
    private const int DefaultSessionTimeoutMs = 5000;
    private const int DefaultRpcProbeTimeoutMs = 2500;
    private const int DefaultPingTimeoutMs = 1000;
    private const int NegativeCacheTtlSeconds = 15;

    private static readonly ConcurrentDictionary<string, DateTime> _unreachable = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an EventLogSession with a timeout and a quick reachability check to avoid long hangs.
    /// Returns null and logs a warning on timeout or failure.
    /// </summary>
    internal static EventLogSession? CreateSession(string? machineName, string? purpose, string? logName, int? timeoutMs = null)
    {
        int budget = Math.Max(1000, timeoutMs ?? DefaultSessionTimeoutMs);

        // Local is fast; avoid Task.Run overhead
        if (string.IsNullOrWhiteSpace(machineName))
        {
            try { return new EventLogSession(); }
            catch (Exception ex)
            {
                _logger.WriteWarning($"{purpose ?? "Session"}: failed to open local session for '{logName}': {ex.Message}");
                return null;
            }
        }

        var normalizedHost = machineName.Trim();
        if (IsHostNegativeCached(normalizedHost))
        {
            _logger.WriteVerbose($"{purpose ?? "Session"}: skipping {normalizedHost} (cached unreachable)");
            return null;
        }

        // Quick reachability check (ping) to fail fast on dead hosts
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send(machineName, Math.Min(DefaultPingTimeoutMs, budget));
            if (reply == null || reply.Status != System.Net.NetworkInformation.IPStatus.Success)
            {
                _logger.WriteWarning($"{purpose ?? "Session"}: ping failed for '{machineName}' when opening '{logName}'. Skipping.");
                MarkHostUnreachable(normalizedHost);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"{purpose ?? "Session"}: ping probe failed for '{machineName}' ({ex.Message}), continuing to session attempt.");
        }

        // RPC (135) preflight to avoid EventLogSession hangs on dead/filtered hosts
        if (!RpcProbe(normalizedHost, Math.Min(DefaultRpcProbeTimeoutMs, budget)))
        {
            _logger.WriteVerbose($"{purpose ?? "Session"}: RPC preflight failed for '{machineName}'");
            MarkHostUnreachable(normalizedHost);
            return null;
        }

        try
        {
            var task = Task.Run(() => new EventLogSession(machineName));
            var completed = Task.WhenAny(task, Task.Delay(budget)).GetAwaiter().GetResult();
            if (completed != task)
            {
                _logger.WriteWarning($"{purpose ?? "Session"}: timeout opening session to '{machineName}' for '{logName}' after {budget} ms");
                MarkHostUnreachable(normalizedHost);
                return null;
            }
            // Success: clear any stale negative entry
            ClearNegativeCache(normalizedHost);
            return task.GetAwaiter().GetResult();
        }
        catch (EventLogException ex)
        {
            _logger.WriteWarning($"{purpose ?? "Session"}: failed opening session to '{machineName}' for '{logName}': {ex.Message}");
            MarkHostUnreachable(normalizedHost);
            return null;
        }
        catch (Exception ex)
        {
            _logger.WriteWarning($"{purpose ?? "Session"}: unexpected error opening session to '{machineName}' for '{logName}': {ex.Message}");
            MarkHostUnreachable(normalizedHost);
            return null;
        }
    }

    /// <summary>
    /// Public helper that exposes the fast session creation logic (shared reachability cache + RPC probe).
    /// </summary>
    public static EventLogSession? OpenSession(string? machineName, int? timeoutMs = null, string? purpose = null, string? logName = null)
    {
        return CreateSession(machineName, purpose, logName, timeoutMs);
    }

    /// <summary>
    /// Clears any cached unreachable state for a specific host.
    /// </summary>
    public static void ClearHostCache(string host)
    {
        ClearNegativeCache(host);
    }

    /// <summary>
    /// Clears all cached unreachable hosts.
    /// </summary>
    public static void ClearAllHostCache()
    {
        _unreachable.Clear();
    }

    private static bool RpcProbe(string host, int timeoutMs)
    {
        string lowerHost = host?.ToLowerInvariant() ?? string.Empty;
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, 135);
            var finished = Task.WhenAny(connectTask, Task.Delay(timeoutMs)).GetAwaiter().GetResult();
            if (finished != connectTask || !tcp.Connected)
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"Session: RPC probe failed for '{host}': {ex.Message}");
            return false;
        }
    }

    private static bool IsHostNegativeCached(string host)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            string lower = host.ToLowerInvariant();
            if (_unreachable.TryGetValue(lower, out var until))
            {
                if (until > DateTime.UtcNow) return true;
                _unreachable.TryRemove(lower, out _);
            }
            return false;
        }
        catch { return false; }
    }

    private static void MarkHostUnreachable(string host)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            string lower = host.ToLowerInvariant();
            _unreachable[lower] = DateTime.UtcNow.AddSeconds(NegativeCacheTtlSeconds);
        }
        catch { }
    }

    private static void ClearNegativeCache(string host)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            string lower = host.ToLowerInvariant();
            _unreachable.TryRemove(lower, out _);
        }
        catch { }
    }
}
