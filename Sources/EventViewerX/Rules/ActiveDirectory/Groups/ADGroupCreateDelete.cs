namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Create Delete
/// 4727:
/// 4730:
/// 4731:
/// 4734:
/// 4744:
/// 4748:
/// 4749:
/// 4753: 
/// 4754: 
/// 4758: 
/// 4759: 
/// 4763: 
/// </summary>
public class ADGroupCreateDelete : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763 };

    /// <inheritdoc />
    public override string LogName => "Security";

    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADGroupCreateDelete;

    /// <summary>Accepts matching Security log events without extra filtering.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    /// <summary>Domain controller emitting the event.</summary>
    public string Computer;

    /// <summary>Short description of the create/delete action.</summary>
    public string Action;

    /// <summary>Group impacted by the operation.</summary>
    public string GroupName;

    /// <summary>Account that performed the operation.</summary>
    public string Who;

    /// <summary>Timestamp when the action occurred.</summary>
    public DateTime When;

    /// <summary>Initialises a group creation/deletion wrapper from an event record.</summary>
    public ADGroupCreateDelete(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupCreateDelete";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
