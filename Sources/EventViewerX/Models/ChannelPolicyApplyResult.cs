namespace EventViewerX;

using System.Collections.Generic;

/// <summary>
/// Result of attempting to apply a <see cref="ChannelPolicy"/> to a channel, including partial success details.
/// </summary>
public sealed class ChannelPolicyApplyResult {
    /// <summary>Log name that was targeted.</summary>
    public string LogName { get; set; } = string.Empty;
    /// <summary>Remote computer where the policy was applied.</summary>
    public string? MachineName { get; set; }

    /// <summary>True only when all requested changes were applied successfully.</summary>
    public bool Success { get; set; }

    /// <summary>True when at least one change was applied, but some failed or were unsupported.</summary>
    public bool PartialSuccess { get; set; }

    /// <summary>Properties that were successfully updated.</summary>
    public List<string> AppliedProperties { get; } = new();
    /// <summary>Properties that were skipped or unsupported on the target.</summary>
    public List<string> SkippedOrUnsupported { get; } = new();
    /// <summary>Error messages captured while applying the policy.</summary>
    public List<string> Errors { get; } = new();
}

