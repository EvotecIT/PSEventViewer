namespace EventViewerX;

/// <summary>
/// Provides logging verbosity switches and default timeouts used throughout EventViewerX.
/// </summary>
public class Settings {
    /// <summary>Shared logger used across the library; adjust verbosity via the instance properties.</summary>
    public static InternalLogger _logger = new InternalLogger();

    /// <summary>TTL (seconds) for negative host reachability cache; adjust for slower/faster networks.</summary>
    public static int NegativeCacheTtlSeconds { get; set; } = 15;

    /// <summary>RPC reachability probe port; defaults to 135 but can be changed for locked-down environments.</summary>
    public static int RpcProbePort { get; set; } = 135;

    /// <summary>Default timeout (ms) when opening EventLogSession connections.</summary>
    public static int SessionTimeoutMs { get; set; } = 5000;

    /// <summary>Timeout (ms) for RPC probe before attempting a session.</summary>
    public static int RpcProbeTimeoutMs { get; set; } = 2500;

    /// <summary>Timeout (ms) for ICMP ping reachability check.</summary>
    public static int PingTimeoutMs { get; set; } = 1000;

    /// <summary>Warm-up budget (ms) for listing log names before queries.</summary>
    public static int ListLogWarmupMs { get; set; } = 3000;

    /// <summary>
    /// Stall timeout (ms) while reading events from a log. Values less than or equal to zero disable the stall timeout (unbounded reads).
    /// Session establishment still respects <see cref="SessionTimeoutMs"/>.
    /// </summary>
    public static int QuerySessionTimeoutMs { get; set; } = 0;

    /// <summary>When set, error messages are written to the console.</summary>
    public bool Error {
        get => _logger.IsError;
        set => _logger.IsError = value;
    }

    /// <summary>Enables verbose output.</summary>
    public bool Verbose {
        get => _logger.IsVerbose;
        set => _logger.IsVerbose = value;
    }

    /// <summary>Enables warning output.</summary>
    public bool Warning {
        get => _logger.IsWarning;
        set => _logger.IsWarning = value;
    }

    /// <summary>Enables progress reporting.</summary>
    public bool Progress {
        get => _logger.IsProgress;
        set => _logger.IsProgress = value;
    }

    /// <summary>Enables debug output.</summary>
    public bool Debug {
        get => _logger.IsDebug;
        set => _logger.IsDebug = value;
    }

    /// <summary>Default degree of parallelism used by operations that support threading.</summary>
    public int NumberOfThreads = 8;

}
