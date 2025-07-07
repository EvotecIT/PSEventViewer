namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents a modified group policy object event.
/// </summary>
public class GpoModified : EventRuleBase {
    /// <summary>Computer where the modification occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Distinguished name of the modified GPO.</summary>
    public string GpoName;
    /// <summary>LDAP display name of the changed attribute.</summary>
    public string AttributeLDAPDisplayName;
    /// <summary>New value of the attribute.</summary>
    public string AttributeValue;
    /// <summary>User responsible for the modification.</summary>
    public string Who;
    /// <summary>Time of the modification.</summary>
    public DateTime When;
    public override List<int> EventIds => new() { 5136 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.GpoModified;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group policy container object
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    public GpoModified(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoModified";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


