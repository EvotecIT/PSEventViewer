using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

[NamedEvent(NamedEvents.ADGroupPolicyChanges, "Security", 5136, 5137, 5141)]
public class ADGroupPolicyChanges : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string GpoName;
    public string AttributeLDAPDisplayName;
    public string AttributeValue;

    public ADGroupPolicyChanges(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyChanges";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        if (e.Data.TryGetValue("ObjectClass", out var cls) && (cls == "groupPolicyContainer" || cls == "container"))
        {
            return new ADGroupPolicyChanges(e);
        }
        return null;
    }
}
