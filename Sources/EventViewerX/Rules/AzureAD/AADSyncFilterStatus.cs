using System;
using System.Collections.Generic;

namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// ADSync connector filtering status (ADSync 6952).
/// </summary>
public class AADSyncFilterStatus : EventRuleBase
{
    public override List<int> EventIds => new() { 6952 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADSyncFilterStatus;

    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "ADSync");
    }

    public string Computer;
    public string? Connector;
    public bool Enabled;
    public DateTime When;

    public AADSyncFilterStatus(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "AADSyncFilterStatus";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        var msg = Rules.RuleHelpers.GetMessage(_eventObject);
        Enabled = msg.IndexOf("enabled", StringComparison.OrdinalIgnoreCase) >= 0;
        Connector = ExtractAfter(msg, "Connector");
    }

    private static string? ExtractAfter(string message, string marker)
    {
        var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var slice = message.Substring(idx + marker.Length).Trim();
        return slice;
    }
}
