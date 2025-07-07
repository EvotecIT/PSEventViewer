namespace EventViewerX;

/// <summary>
/// Convenience wrapper exposing static logging switches for the module.
/// </summary>
public class LoggingMessages {
    /// <summary>
    /// Underlying logger instance used by the helpers.
    /// </summary>
    public static InternalLogger Logger = new InternalLogger();

    /// <summary>
    /// Enables or disables error message output.
    /// </summary>
    public static bool Error {
        get => Logger.IsError;
        set => Logger.IsError = value;
    }

    /// <summary>
    /// Enables or disables verbose message output.
    /// </summary>
    public static bool Verbose {
        get => Logger.IsVerbose;
        set => Logger.IsVerbose = value;
    }

    /// <summary>
    /// Enables or disables warning message output.
    /// </summary>
    public static bool Warning {
        get => Logger.IsWarning;
        set => Logger.IsWarning = value;
    }

    /// <summary>
    /// Enables or disables progress message output.
    /// </summary>
    public static bool Progress {
        get => Logger.IsProgress;
        set => Logger.IsProgress = value;
    }

    /// <summary>
    /// Enables or disables debug message output.
    /// </summary>
    public static bool Debug {
        get => Logger.IsDebug;
        set => Logger.IsDebug = value;
    }

}