using System;
using EventViewerX;

namespace EventViewerX.Rules.ActiveDirectory {

    /// <summary>
    /// Active Directory Other Change Detailed
    /// 5136: A directory service object was modified
    /// 5137: A directory service object was created
    /// 5139: A directory service object was deleted
    /// 5141: A directory service object was moved
    /// </summary>
[NamedEvent(NamedEvents.ADOtherChangeDetailed, "Security", 5136, 5137, 5139, 5141)]
    public class ADOtherChangeDetailed : EventObjectSlim {

        public string Computer;
        public string Action;
        public string ObjectClass;
        public string OperationType;
        public string Who;
        public DateTime When;
        public string User; // 'User Object'
        public string FieldChanged; // 'Field Changed'
        public string FieldValue; // 'Field Value'


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
    public static EventObjectSlim? TryCreate(EventObject e)
    {
        if (e.Data.TryGetValue("ObjectClass", out var cls) && cls != "user" && cls != "computer" && cls != "organizationalUnit" && cls != "group")
        {
            return new ADOtherChangeDetailed(e);
        }
        return null;
    }

}}
