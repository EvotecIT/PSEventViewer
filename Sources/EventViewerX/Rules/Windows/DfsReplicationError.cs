namespace EventViewerX.Rules.Windows;

/// <summary>
/// DFS Replication partner communication failure
/// 5002: The DFS Replication service encountered an error communicating with a partner.
/// </summary>
public class DfsReplicationError : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 5002 };
    /// <inheritdoc />
    public override string LogName => "DFS Replication";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.DfsReplicationError;

    /// <summary>Accepts DFS Replication partner communication errors.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Replication group affected.</summary>
    public string ReplicationGroup;
    /// <summary>Error code reported.</summary>
    public string ErrorCode;
    /// <summary>Replication partner.</summary>
    public string Partner;
    /// <summary>Event time.</summary>
    public DateTime When;

    /// <summary>Initialises a DFS replication error wrapper from an event record.</summary>
    public DfsReplicationError(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DfsReplicationError";
        ReplicationGroup = _eventObject.GetValueFromDataDictionary("Replication Group");
        ErrorCode = _eventObject.GetValueFromDataDictionary("Error Code", "Error");
        Partner = _eventObject.GetValueFromDataDictionary("Partner Name");
        When = _eventObject.TimeCreated;
    }
}
