namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Detailed audit information for group policy changes.
/// </summary>
public class ADGroupPolicyChangesDetailed : EventRuleBase {
    /// <summary>Domain controller that emitted the event.</summary>
    public string Computer;

    /// <summary>Short description of the change.</summary>
    public string Action;

    /// <summary>LDAP object class (expected <c>groupPolicyContainer</c>).</summary>
    public string ObjectClass;

    /// <summary>Operation type translated to text.</summary>
    public string OperationType;

    /// <summary>Account performing the modification.</summary>
    public string Who;

    /// <summary>Timestamp of the change.</summary>
    public DateTime When;

    /// <summary>Distinguished name of the affected GPO.</summary>
    public string GpoName;

    /// <summary>LDAP display name of the changed attribute.</summary>
    public string AttributeLDAPDisplayName;

    /// <summary>New value written to the attribute.</summary>
    public string AttributeValue;

    /// <inheritdoc />
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };

    /// <inheritdoc />
    public override string LogName => "Security";

    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADGroupPolicyChangesDetailed;

    /// <summary>Handles only events where the object class is a group policy container.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    /// <summary>Initialises a detailed GPO change wrapper from an event record.</summary>
    public ADGroupPolicyChangesDetailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyChangesDetailed";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
    }
}
