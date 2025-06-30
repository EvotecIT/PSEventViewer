namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Changes detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
/// </summary>
public class ADUserChangeDetailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string User; // 'User Object'
    public string FieldChanged; // 'Field Changed'
    public string FieldValue; // 'Field Value'


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
        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;

        // OverwriteByField logic
        User = OverwriteByField(Action, "A directory service object was moved.", User, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
        FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));
    }
}
