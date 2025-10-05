namespace EventViewerX;

using System.Collections.Generic;

/// <summary>
/// Result of attempting to apply a <see cref="ChannelPolicy"/> to a channel, including partial success details.
/// </summary>
public sealed class ChannelPolicyApplyResult {
    public string LogName { get; set; } = string.Empty;
    public string? MachineName { get; set; }

    /// <summary>True only when all requested changes were applied successfully.</summary>
    public bool Success { get; set; }

    /// <summary>True when at least one change was applied, but some failed or were unsupported.</summary>
    public bool PartialSuccess { get; set; }

    public List<string> AppliedProperties { get; } = new();
    public List<string> SkippedOrUnsupported { get; } = new();
    public List<string> Errors { get; } = new();
}

