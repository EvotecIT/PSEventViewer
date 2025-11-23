using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EventViewerX.Rules.AzureAD;

/// <summary>
/// Azure AD Connect import/sync status (Directory Synchronization 105/132/133/134).
/// </summary>
public class AADSyncImportStatus : EventRuleBase
{
    public override List<int> EventIds => new() { 105, 132, 133, 134 };
    public override string LogName => "Application";
    public override NamedEvents NamedEvent => NamedEvents.AADSyncImportStatus;

    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Directory Synchronization");
    }

    public string Computer;
    public string Phase;
    public string? TrackingId;
    public string? SessionId;
    public string? WatermarkHash;
    public string? PartitionName;
    public DateTime When;

    private static readonly Regex WatermarkHashRegex = new("Hash:\\s*(?<h>[A-Za-z0-9+/=\\-]+)", RegexOptions.IgnoreCase);
    private static readonly Regex TrackingRegex = new("TrackingId:\\s*(?<t>[A-Za-z0-9\\-]+)", RegexOptions.IgnoreCase);
    private static readonly Regex SessionRegex = new("Session\\s*Id:?\\s*(?<s>[A-Za-z0-9\\-]+)", RegexOptions.IgnoreCase);
    private static readonly Regex PartitionRegex = new("<partition-name>(?<p>[^<]+)</partition-name>", RegexOptions.IgnoreCase);

    public AADSyncImportStatus(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "AADSyncImportStatus";
        Computer = _eventObject.ComputerName;
        When = _eventObject.TimeCreated;

        var msg = Rules.RuleHelpers.GetMessage(_eventObject);

        Phase = eventObject.Id switch
        {
            105 => "ImportIteration",
            132 => "RefetchBaseline",
            133 => "ImportStart",
            134 => "ImportComplete",
            _ => "Info"
        };

        TrackingId = MatchOrNull(TrackingRegex, msg);
        SessionId = MatchOrNull(SessionRegex, msg);
        WatermarkHash = MatchOrNull(WatermarkHashRegex, msg);

        var partitionMatch = PartitionRegex.Match(msg);
        if (partitionMatch.Success) PartitionName = partitionMatch.Groups["p"].Value;
    }

    private static string? MatchOrNull(Regex rx, string input)
    {
        var m = rx.Match(input);
        return m.Success ? m.Groups[1].Value : null;
    }
}
