namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Changes detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
/// </summary>
public class ADUserChangeDetailed : EventRuleBase {
    /// <summary>
    /// Computer where the change occurred.
    /// </summary>
    public string Computer;

    /// <summary>
    /// Description of the action.
    /// </summary>
    public string Action;

    /// <summary>
    /// Class of the changed object.
    /// </summary>
    public string ObjectClass;

    /// <summary>
    /// Operation type description.
    /// </summary>
    public string OperationType;

    /// <summary>
    /// User performing the change.
    /// </summary>
    public string Who;

    /// <summary>
    /// Time when the change happened.
    /// </summary>
    public DateTime When;

    /// <summary>
    /// Affected user object.
    /// </summary>
    public string User; // 'User Object'

    /// <summary>
    /// Name of the field that was changed.
    /// </summary>
    public string FieldChanged; // 'Field Changed'

    /// <summary>
    /// New value of the changed field.
    /// </summary>
    public string FieldValue; // 'Field Value'
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserChangeDetailed;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a user object change
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "user";
    }


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
