namespace EventViewerX;

/// <summary>
/// Provides basic logging and threading configuration for the library.
/// </summary>
public class Settings {
    public static InternalLogger _logger = new InternalLogger();

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

    /// <summary>
    /// Number of threads to use for lingering object detection
    /// </summary>
    public int NumberOfThreads = 8;

}