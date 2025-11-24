namespace EventViewerX;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

/// <summary>
/// Strongly typed policy for a Windows Event Log channel (modern wevt or classic).
/// </summary>
public sealed class ChannelPolicy {
    /// <summary>Log name to apply policy to.</summary>
    public string LogName { get; set; } = string.Empty;
    /// <summary>Optional remote computer name; <c>null</c> targets the local host.</summary>
    public string? MachineName { get; set; }
    /// <summary>Enables or disables the channel when set.</summary>
    public bool? IsEnabled { get; set; }
    /// <summary>Maximum log size in bytes.</summary>
    public long? MaximumSizeInBytes { get; set; }
    /// <summary>Full file path for the log.</summary>
    public string? LogFilePath { get; set; }
    /// <summary>Isolation level (application/system/custom) for the log.</summary>
    public EventLogIsolation? Isolation { get; set; }
    /// <summary>Retention mode for the channel.</summary>
    public EventLogMode? Mode { get; set; }
    /// <summary>SDDL security descriptor controlling access.</summary>
    public string? SecurityDescriptor { get; set; }

    /// <summary>Serializes the policy into a key/value dictionary for diagnostics or JSON output.</summary>
    public IReadOnlyDictionary<string, object?> ToDictionary() => new Dictionary<string, object?> {
        [nameof(LogName)] = LogName,
        [nameof(MachineName)] = MachineName,
        [nameof(IsEnabled)] = IsEnabled,
        [nameof(MaximumSizeInBytes)] = MaximumSizeInBytes,
        [nameof(LogFilePath)] = LogFilePath,
        [nameof(Isolation)] = Isolation?.ToString(),
        [nameof(Mode)] = Mode?.ToString(),
        [nameof(SecurityDescriptor)] = SecurityDescriptor,
    };
}
