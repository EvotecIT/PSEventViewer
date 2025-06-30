using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Change Detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
/// </summary>
[NamedEvent(NamedEvents.ADGroupChangeDetailed, "Security", 5136, 5137, 5139, 5141)]
public class ADGroupChangeDetailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string Group;
    public string FieldChanged;
    public string FieldValue;

    public ADGroupChangeDetailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupChangeDetailed";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Group = _eventObject.GetValueFromDataDictionary("ObjectDN");
        FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");

        Group = OverwriteByField(Action, "A directory service object was moved.", Group, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
        FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        return e.Data.TryGetValue("ObjectClass", out var cls) && cls == "user"
            ? new ADGroupChangeDetailed(e)
            : null;
    }
}
