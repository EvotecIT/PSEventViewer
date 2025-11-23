using System;

namespace EventViewerX.Rules.ActiveDirectory {

    /// <summary>
    /// Active Directory Other Change Detailed
    /// 5136: A directory service object was modified
    /// 5137: A directory service object was created
    /// 5139: A directory service object was deleted
    /// 5141: A directory service object was moved
    /// </summary>
    public class ADOtherChangeDetailed : EventRuleBase {
        /// <summary>Domain controller where the change was recorded.</summary>
        public string Computer;

        /// <summary>Short description of the object change.</summary>
        public string Action;

        /// <summary>LDAP object class for the changed object.</summary>
        public string ObjectClass;

        /// <summary>Operation type translated to human-friendly text.</summary>
        public string OperationType;

        /// <summary>Account that performed the change.</summary>
        public string Who;

        /// <summary>Timestamp of the change.</summary>
        public DateTime When;

        /// <summary>Distinguished name of the changed object.</summary>
        public string User; // 'User Object'

        /// <summary>Attribute modified on the object.</summary>
        public string FieldChanged; // 'Field Changed'

        /// <summary>Value written to the attribute.</summary>
        public string FieldValue; // 'Field Value'
    /// <inheritdoc />
    public override List<int> EventIds => new() { 5136, 5137, 5139, 5141 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADOtherChangeDetailed;

    /// <summary>Ignores user, computer, OU and group classes; captures all other object changes.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Only handle objects that are NOT user, computer, organizationalUnit, or group
        if (eventObject.Data.TryGetValue("ObjectClass", out var objectClass)) {
            return objectClass != "user" && objectClass != "computer" &&
                   objectClass != "organizationalUnit" && objectClass != "group";
        }
        return false;
    }


        /// <summary>Initialises a detailed wrapper for non-standard directory object changes.</summary>
        public ADOtherChangeDetailed(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;

            Type = "ADOtherChangeDetailed";
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
}
