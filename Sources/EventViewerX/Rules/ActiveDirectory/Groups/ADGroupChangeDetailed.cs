namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Change Detailed
/// 5136: A directory service object was modified
/// 5137: A directory service object was created
/// 5139: A directory service object was deleted
/// 5141: A directory service object was moved
/// </summary>
public class ADGroupChangeDetailed : EventRuleBase {
    /// <summary>Domain controller where the change was captured.</summary>
    public string Computer;

    /// <summary>Action description from the event record.</summary>
    public string Action;

    /// <summary>LDAP object class (should be <c>group</c>).</summary>
    public string ObjectClass;

    /// <summary>Operation type translated to human-readable text.</summary>
    public string OperationType;

    /// <summary>Account that performed the modification.</summary>
    public string Who;

    /// <summary>Timestamp of the modification.</summary>
    public DateTime When;

    /// <summary>Distinguished name of the group affected.</summary>
    public string Group; // 'User Object'

    /// <summary>LDAP attribute that was changed.</summary>
    public string FieldChanged; // 'Field Changed'

    /// <summary>New value written to the attribute.</summary>
    public string FieldValue; // 'Field Value'

    /// <inheritdoc />
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };

    /// <inheritdoc />
    public override string LogName => "Security";

    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADGroupChangeDetailed;

    /// <summary>Processes only directory object events where the object class is <c>group</c>.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group object change
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "group";
    }

    /// <summary>Initialises a detailed group change wrapper from an event record.</summary>
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
