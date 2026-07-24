namespace EventViewerX.Providers;

/// <summary>
/// Windows SDK and MSVC tools used only while building a provider package.
/// They are not required to install a package or write events.
/// </summary>
public sealed class EventProviderToolchain {
    /// <summary>Complete path to the Windows Message Compiler.</summary>
    public string MessageCompilerPath { get; internal set; } = string.Empty;

    /// <summary>Complete path to the Windows Resource Compiler.</summary>
    public string ResourceCompilerPath { get; internal set; } = string.Empty;

    /// <summary>Complete path to the MSVC linker.</summary>
    public string LinkerPath { get; internal set; } = string.Empty;

    /// <summary>Detected Windows SDK version.</summary>
    public string WindowsSdkVersion { get; internal set; } = string.Empty;

    /// <summary>Detected MSVC tools version.</summary>
    public string MsvcVersion { get; internal set; } = string.Empty;
}

/// <summary>Optional explicit paths for provider package compilation.</summary>
public sealed class EventProviderToolchainOptions {
    /// <summary>Explicit path to mc.exe.</summary>
    public string MessageCompilerPath { get; set; } = string.Empty;

    /// <summary>Explicit path to rc.exe.</summary>
    public string ResourceCompilerPath { get; set; } = string.Empty;

    /// <summary>Explicit path to the real Microsoft link.exe.</summary>
    public string LinkerPath { get; set; } = string.Empty;
}
