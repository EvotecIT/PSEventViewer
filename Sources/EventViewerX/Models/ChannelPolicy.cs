namespace EventViewerX;

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

/// <summary>
/// Strongly typed policy for a Windows Event Log channel (modern wevt or classic).
/// </summary>
public sealed class ChannelPolicy {
    public string LogName { get; set; } = string.Empty;
    public string? MachineName { get; set; }
    public bool? IsEnabled { get; set; }
    public long? MaximumSizeInBytes { get; set; }
    public string? LogFilePath { get; set; }
    public EventLogIsolation? Isolation { get; set; }
    public EventLogMode? Mode { get; set; }
    public string? SecurityDescriptor { get; set; }

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
