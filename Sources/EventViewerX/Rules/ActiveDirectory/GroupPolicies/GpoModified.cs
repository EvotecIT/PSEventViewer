namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents a modified group policy object event.
/// </summary>
public class GpoModified : EventRuleBase {
    public string Computer;
    public string Action;
    public string GpoName;
    public string AttributeLDAPDisplayName;
    public string AttributeValue;
    public string Who;
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


