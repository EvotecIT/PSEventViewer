namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Change Detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
/// </summary>
public class ADGroupChangeDetailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string Group; // 'User Object'
    public string FieldChanged; // 'Field Changed'
    public string FieldValue; // 'Field Value'

    public ADGroupChangeDetailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupChangeDetailed";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Group = _eventObject.GetValueFromDataDictionary("ObjectDN");
        FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");

        // OverwriteByField logic
        Group = OverwriteByField(Action, "A directory service object was moved.", Group, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
        FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}