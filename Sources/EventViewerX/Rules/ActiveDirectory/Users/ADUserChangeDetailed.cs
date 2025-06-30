using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Changes detailed
/// </summary>
[NamedEvent(NamedEvents.ADUserChangeDetailed, "Security", 5136, 5137, 5139, 5141)]
public class ADUserChangeDetailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string User;
    public string FieldChanged;
    public string FieldValue;

    public ADUserChangeDetailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserChangeDetailed";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        User = _eventObject.GetValueFromDataDictionary("ObjectDN");
        FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        User = OverwriteByField(Action, "A directory service object was moved.", User, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
        FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        return e.Data.TryGetValue("ObjectClass", out var cls) && cls == "user" ? new ADUserChangeDetailed(e) : null;
    }
}
