using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Provision credentials ping start/end (Directory Synchronization 653/654).
/// </summary>
public class AADSyncProvisionCredentialsPing : EventRuleBase
{
    public override List<int> EventIds => new() { 653, 654 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADSyncProvisionCredentialsPing;

    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Directory Synchronization");
    }

    public string Computer;
    public string State; // Start/End
    public string? TrackingId;
    public string? PartitionName;
    public string? ConnectorId;
    public DateTime When;

    private static readonly Regex PartitionRegex = new("<partition-name>(?<p>[^<]+)</partition-name>", RegexOptions.IgnoreCase);
    private static readonly Regex ConnectorRegex = new("<connector-id>(?<c>[^<]+)</connector-id>", RegexOptions.IgnoreCase);

    public AADSyncProvisionCredentialsPing(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "AADSyncProvisionCredentialsPing";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        State = eventObject.Id == 653 ? "Start" : "End";
        var msg = Rules.RuleHelpers.GetMessage(_eventObject);
        TrackingId = ExtractAfter(msg, "TrackingID :") ?? ExtractAfter(msg, "TrackingId:");

        var partitionMatch = PartitionRegex.Match(msg);
        if (partitionMatch.Success) PartitionName = partitionMatch.Groups["p"].Value;

        var connectorMatch = ConnectorRegex.Match(msg);
        if (connectorMatch.Success) ConnectorId = connectorMatch.Groups["c"].Value;
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
