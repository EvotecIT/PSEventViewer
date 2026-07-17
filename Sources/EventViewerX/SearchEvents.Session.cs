using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace EventViewerX;

public partial class SearchEvents : Settings
{
    private static int DefaultSessionTimeoutMs => Settings.SessionTimeoutMs;
    private static int DefaultRpcProbeTimeoutMs => Settings.RpcProbeTimeoutMs;

    /// <summary>How long to remember unreachable hosts to avoid repeated slow probes.</summary>
    private static int NegativeCacheTtlSecondsValue => Settings.NegativeCacheTtlSeconds;

    private static readonly ConcurrentDictionary<string, DateTime> _unreachable = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates an EventLogSession with a timeout and a quick reachability check to avoid long hangs.
    /// Returns null and logs a warning on timeout or failure.
    /// </summary>
    internal static EventLogSession? CreateSession(string? machineName, string? purpose, string? logName, int? timeoutMs = null) {
        return CreateSessionResult(machineName, purpose, logName, timeoutMs).Session;
    }

    internal static EventLogSessionOpenResult CreateSessionResult(
        string? machineName,
        string? purpose,
        string? logName,
        int? timeoutMs = null,
        Func<EventLogSession>? localSessionFactory = null,
        Func<string, int, bool>? rpcProbeOverride = null,
        Func<string, EventLogSession>? remoteSessionFactory = null) {
        int budget = timeoutMs ?? DefaultSessionTimeoutMs;
        if (budget <= 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Session timeout must be positive.");
        }
        string operation = purpose ?? "Session";
        string channel = logName ?? string.Empty;
        var stopwatch = Stopwatch.StartNew();

        // Local is fast; avoid ping/RPC probes (many CI agents block 135)
        if (IsLocalMachine(machineName)) {
            try {
                EventLogSession session = ExecuteWithTimeout(
                    localSessionFactory ?? (static () => new EventLogSession()),
                    budget,
                    $"Timed out opening the local Event Log session for '{channel}' after {budget} ms.",
                    static lateSession => lateSession.Dispose());
                return SessionSuccess(machineName, GetFQDN(), operation, channel, budget, session);
            } catch (TimeoutException ex) {
                _logger.WriteWarning($"{operation}: {ex.Message}");
                return SessionFailure(
                    machineName,
                    GetFQDN(),
                    operation,
                    channel,
                    EventLogSessionOpenStatus.Timeout,
                    ex.Message,
                    budget,
                    ex.GetType().Name);
            } catch (Exception ex) {
                _logger.WriteWarning($"{operation}: failed to open local session for '{channel}': {ex.Message}");
                return SessionFailure(
                    machineName,
                    GetFQDN(),
                    operation,
                    channel,
                    EventLogSessionOpenStatus.LocalSessionUnavailable,
                    $"Failed to open local Event Log session for '{channel}': {ex.Message}",
                    budget,
                    ex.GetType().Name);
            }
        }

        var normalizedHost = machineName?.Trim() ?? string.Empty;
        var targetHost = string.IsNullOrWhiteSpace(normalizedHost) ? GetFQDN() : normalizedHost;
        if (TryGetHostNegativeCacheExpiry(normalizedHost, out DateTime cachedUntilUtc))
        {
            _logger.WriteVerbose($"{operation}: skipping {normalizedHost} (cached unreachable)");
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.NegativeCache,
                $"Host '{targetHost}' is temporarily cached as unreachable until {cachedUntilUtc:u}.",
                budget,
                nameof(EventLogSessionOpenStatus.NegativeCache),
                cachedUntilUtc);
        }

        // RPC (135) preflight to avoid EventLogSession hangs on dead/filtered hosts
        int rpcBudget = Math.Min(DefaultRpcProbeTimeoutMs, RemainingSessionBudget(budget, stopwatch));
        if (rpcBudget <= 0) {
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.Timeout,
                $"Timed out opening Event Log session to '{targetHost}' for '{channel}' after {budget} ms.",
                budget,
                nameof(EventLogSessionOpenStatus.Timeout));
        }
        if (!(rpcProbeOverride?.Invoke(normalizedHost, rpcBudget) ?? RpcProbe(normalizedHost, rpcBudget))) {
            _logger.WriteVerbose($"{operation}: RPC preflight failed for '{machineName}'");
            MarkHostUnreachable(normalizedHost);
            TryGetHostNegativeCacheExpiry(normalizedHost, out DateTime rpcCachedUntilUtc);
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.RpcUnavailable,
                $"RPC preflight to '{targetHost}' on port {Settings.RpcProbePort} failed within the {budget} ms session budget.",
                budget,
                nameof(EventLogSessionOpenStatus.RpcUnavailable),
                rpcCachedUntilUtc);
        }

        try {
            int sessionBudget = RemainingSessionBudget(budget, stopwatch);
            if (sessionBudget <= 0) {
                return SessionFailure(
                    machineName,
                    targetHost,
                    operation,
                    channel,
                    EventLogSessionOpenStatus.Timeout,
                    $"Timed out opening Event Log session to '{targetHost}' for '{channel}' after {budget} ms.",
                    budget,
                    nameof(EventLogSessionOpenStatus.Timeout));
            }

            Func<string, EventLogSession> sessionFactory = remoteSessionFactory ?? (static host => new EventLogSession(host));
            EventLogSession session = ExecuteWithTimeout(
                () => sessionFactory(machineName!),
                sessionBudget,
                $"Timed out opening Event Log session to '{targetHost}' for '{channel}' after {budget} ms.",
                static lateSession => lateSession.Dispose());
            // Success: clear any stale negative entry
            ClearNegativeCache(normalizedHost);
            return SessionSuccess(machineName, targetHost, operation, channel, budget, session);
        } catch (TimeoutException ex) {
            _logger.WriteWarning($"{operation}: {ex.Message}");
            MarkHostUnreachable(normalizedHost);
            TryGetHostNegativeCacheExpiry(normalizedHost, out DateTime timeoutCachedUntilUtc);
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.Timeout,
                ex.Message,
                budget,
                ex.GetType().Name,
                timeoutCachedUntilUtc);
        } catch (UnauthorizedAccessException ex) {
            _logger.WriteWarning($"{operation}: access denied opening session to '{machineName}' for '{channel}': {ex.Message}");
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.AccessDenied,
                ex.Message,
                budget,
                ex.GetType().Name);
        }
        catch (EventLogException ex)
        {
            _logger.WriteWarning($"{operation}: failed opening session to '{machineName}' for '{channel}': {ex.Message}");
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.EventLogSessionUnavailable,
                ex.Message,
                budget,
                ex.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.WriteWarning($"{operation}: unexpected error opening session to '{machineName}' for '{channel}': {ex.Message}");
            return SessionFailure(
                machineName,
                targetHost,
                operation,
                channel,
                EventLogSessionOpenStatus.Error,
                ex.Message,
                budget,
                ex.GetType().Name);
        }
    }

    private static int RemainingSessionBudget(int budget, Stopwatch stopwatch) {
        long remaining = budget - stopwatch.ElapsedMilliseconds;
        return remaining > 0 ? (int)Math.Min(int.MaxValue, remaining) : 0;
    }

    /// <summary>
    /// Public helper that exposes the fast session creation logic (shared reachability cache + RPC probe).
    /// </summary>
    public static EventLogSession? OpenSession(string? machineName, int? timeoutMs = null, string? purpose = null, string? logName = null)
    {
        return CreateSession(machineName, purpose, logName, timeoutMs);
    }

    /// <summary>
    /// Opens an Event Log session and returns diagnostic details when the session cannot be created.
    /// </summary>
    public static EventLogSessionOpenResult OpenSessionResult(string? machineName, int? timeoutMs = null, string? purpose = null, string? logName = null)
    {
        return CreateSessionResult(machineName, purpose, logName, timeoutMs);
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

    private static bool RpcProbe(string host, int timeoutMs) {
        try {
            using var tcp = new TcpClient();
            Task connectTask = tcp.ConnectAsync(host, Settings.RpcProbePort);
            bool connectedWithinTimeout;
            try {
                connectedWithinTimeout = connectTask.Wait(timeoutMs);
            } catch (AggregateException) {
                connectTask.GetAwaiter().GetResult();
                return false;
            }

            if (!connectedWithinTimeout || !tcp.Connected) {
                _ = connectTask.ContinueWith(
                    t => _ = t.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                return false;
            }
            return true;
        } catch (Exception ex) {
            _logger.WriteVerbose($"Session: RPC probe failed for '{host}': {ex.Message}");
            return false;
        }
    }

    private static bool IsHostNegativeCached(string host)
    {
        try
        {
            return TryGetHostNegativeCacheExpiry(host, out _);
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"Negative cache check failed for '{host}': {ex.Message}");
            return false;
        }
    }

    private static bool TryGetHostNegativeCacheExpiry(string host, out DateTime until)
    {
        until = default;
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            string lower = host.ToLowerInvariant();
            if (_unreachable.TryGetValue(lower, out until))
            {
                if (until > DateTime.UtcNow) return true;
                // Older frameworks don't expose TryRemove(KeyValuePair<,>), fall back to key+out.
                _unreachable.TryRemove(lower, out _);
            }
            until = default;
            return false;
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"Negative cache check failed for '{host}': {ex.Message}");
            until = default;
            return false;
        }
    }

    private static void MarkHostUnreachable(string host)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            string lower = host.ToLowerInvariant();
            _unreachable[lower] = DateTime.UtcNow.AddSeconds(NegativeCacheTtlSecondsValue);
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"Failed to mark '{host}' unreachable: {ex.Message}");
        }
    }

    private static void ClearNegativeCache(string host)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host)) return;
            string lower = host.ToLowerInvariant();
            _unreachable.TryRemove(lower, out _);
        }
        catch (Exception ex)
        {
            _logger.WriteVerbose($"Failed clearing negative cache for '{host}': {ex.Message}");
        }
    }

    private static bool IsLocalMachine(string? machineName)
    {
        if (string.IsNullOrWhiteSpace(machineName)) return true;

        string name = machineName!.Trim();
        var cmp = StringComparison.OrdinalIgnoreCase;
        return name == "." || name == "localhost" || name.Equals(Environment.MachineName, cmp) || name.Equals(GetFQDN(), cmp);
    }

    private static EventLogSessionOpenResult SessionSuccess(
        string? machineName,
        string targetHost,
        string purpose,
        string logName,
        int timeoutMs,
        EventLogSession session) =>
        new()
        {
            MachineName = machineName,
            TargetHost = targetHost,
            Purpose = purpose,
            LogName = logName,
            Status = EventLogSessionOpenStatus.Success,
            Session = session,
            TimeoutMs = timeoutMs
        };

    private static EventLogSessionOpenResult SessionFailure(
        string? machineName,
        string targetHost,
        string purpose,
        string logName,
        EventLogSessionOpenStatus status,
        string errorMessage,
        int timeoutMs,
        string? errorType = null,
        DateTime? cachedUntilUtc = null) =>
        new()
        {
            MachineName = machineName,
            TargetHost = targetHost,
            Purpose = purpose,
            LogName = logName,
            Status = status,
            ErrorMessage = errorMessage,
            ErrorType = errorType ?? status.ToString(),
            TimeoutMs = timeoutMs,
            CachedUntilUtc = cachedUntilUtc
        };
}
