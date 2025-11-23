using System;
using System.Collections.Generic;

namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Azure AD Connect cycle stage (Directory Synchronization event 904).
/// </summary>
public class AADSyncCycleStage : EventRuleBase
{
    public override List<int> EventIds => new() { 904 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADSyncCycleStage;

    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Directory Synchronization");
    }

    public string Computer;
    public string Stage;
    public string? Connector;
    public string? CycleType;
    public string? TrackingId;
    public DateTime When;

    public AADSyncCycleStage(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "AADSyncCycleStage";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        var message = Rules.RuleHelpers.GetMessage(_eventObject);

        // Basic stage derivation
        Stage = DeriveStage(message);
        Connector = ExtractAfter(message, "Connector:");
        CycleType = message.IndexOf("Delta", StringComparison.OrdinalIgnoreCase) >= 0 ? "Delta" :
            message.IndexOf("Full", StringComparison.OrdinalIgnoreCase) >= 0 ? "Full" : null;
        TrackingId = ExtractAfter(message, "TrackingId:") ?? ExtractAfter(message, "TrackingID :");
    }

    private static string DeriveStage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;
        if (message.IndexOf("cycle started", StringComparison.OrdinalIgnoreCase) >= 0) return "CycleStarted";
        if (message.StartsWith("Import/Sync/Export cycle", StringComparison.OrdinalIgnoreCase)) return "CycleStarted";
        if (message.StartsWith("Initializing", StringComparison.OrdinalIgnoreCase)) return "Initializing";
        if (message.StartsWith("Importing", StringComparison.OrdinalIgnoreCase)) return "Importing";
        if (message.StartsWith("Starting:", StringComparison.OrdinalIgnoreCase)) return "Starting";
        if (message.StartsWith("Finished:", StringComparison.OrdinalIgnoreCase)) return "Finished";
        if (message.StartsWith("Before Execute RunProfile", StringComparison.OrdinalIgnoreCase)) return "RunProfileBefore";
        if (message.StartsWith("After Execute RunProfile", StringComparison.OrdinalIgnoreCase)) return "RunProfileAfter";
        if (message.IndexOf("scheduler", StringComparison.OrdinalIgnoreCase) >= 0 &&
            message.IndexOf("completed", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "SchedulerComplete";
        }
        return "Info";
    }

    private static string? ExtractAfter(string message, string marker)
    {
        var idx = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = idx + marker.Length;
        var slice = message.Substring(start).Trim();
        return slice.TrimEnd('.', ';');
    }
}
