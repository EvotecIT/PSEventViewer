namespace EventViewerX;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a Windows Event Collector (WEC) subscription stored under
/// HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\EventCollector\Subscriptions.
/// </summary>
public sealed class SubscriptionInfo {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? Enabled { get; set; }
    public string? ContentFormat { get; set; }
    public string? DeliveryMode { get; set; }
    public string? MachineName { get; set; }
    public string? RawXml { get; set; }
    public IReadOnlyList<string> Queries { get; set; } = Array.Empty<string>();
}

