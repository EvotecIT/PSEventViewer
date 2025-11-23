using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Password hash sync manager heartbeat (Directory Synchronization 663).
/// </summary>
public class AADSyncPasswordHashSyncStatus : EventRuleBase
{
    public override List<int> EventIds => new() { 663 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADSyncPasswordHashSyncStatus;

    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Directory Synchronization");
    }

    public string Computer;
    public string? PartitionName;
    public string? ConnectorId;
    public bool Alive = true;
    public DateTime When;

    private static readonly Regex PartitionRegex = new("<partition-name>(?<p>[^<]+)</partition-name>", RegexOptions.IgnoreCase);
    private static readonly Regex ConnectorRegex = new("<connector-id>(?<c>[^<]+)</connector-id>", RegexOptions.IgnoreCase);

    public AADSyncPasswordHashSyncStatus(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "AADSyncPasswordHashSyncStatus";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        var msg = Rules.RuleHelpers.GetMessage(_eventObject);
        var partitionMatch = PartitionRegex.Match(msg);
        if (partitionMatch.Success) PartitionName = partitionMatch.Groups["p"].Value;

        var connectorMatch = ConnectorRegex.Match(msg);
        if (connectorMatch.Success) ConnectorId = connectorMatch.Groups["c"].Value;
    }
}
