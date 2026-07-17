using System.Threading;

namespace EventViewerX;

/// <summary>
/// Provides logging verbosity switches and default timeouts used throughout EventViewerX.
/// </summary>
public class Settings {
    private static readonly AsyncLocal<InternalLogger?> ContextLogger = new();
    private static readonly InternalLogger DefaultLogger = new();

    /// <summary>
    /// Logger for the current asynchronous execution context.
    /// </summary>
    /// <remarks>
    /// The value flows to child tasks without allowing concurrent PowerShell runspaces or callers to overwrite
    /// one another's logging callbacks.
    /// </remarks>
    public static InternalLogger _logger {
        get => ContextLogger.Value ?? DefaultLogger;
        set => ContextLogger.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>TTL (seconds) for negative host reachability cache; adjust for slower/faster networks.</summary>
    public static int NegativeCacheTtlSeconds { get; set; } = 15;

    /// <summary>RPC reachability probe port; defaults to 135 but can be changed for locked-down environments.</summary>
    public static int RpcProbePort { get; set; } = 135;

    /// <summary>Default timeout (ms) when opening EventLogSession connections.</summary>
    public static int SessionTimeoutMs { get; set; } = 5000;

    /// <summary>Timeout (ms) for RPC probe before attempting a session.</summary>
    public static int RpcProbeTimeoutMs { get; set; } = 2500;

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

}
