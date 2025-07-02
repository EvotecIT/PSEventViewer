namespace EventViewerX.Rules.Windows;

/// <summary>
/// DFS Replication partner communication failure
/// 5002: The DFS Replication service encountered an error communicating with a partner.
/// </summary>
public class DfsReplicationError : EventRuleBase {
    public override List<int> EventIds => new() { 5002 };
    public override string LogName => "DFS Replication";
    public override NamedEvents NamedEvent => NamedEvents.DfsReplicationError;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public string ReplicationGroup;
    public string ErrorCode;
    public string Partner;
    public DateTime When;

    public DfsReplicationError(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DfsReplicationError";
        ReplicationGroup = _eventObject.GetValueFromDataDictionary("Replication Group");
        ErrorCode = _eventObject.GetValueFromDataDictionary("Error Code", "Error");
        Partner = _eventObject.GetValueFromDataDictionary("Partner Name");
        When = _eventObject.TimeCreated;
    }
}
