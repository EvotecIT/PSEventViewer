using System;
using System.Collections.Generic;
using System.Text;

namespace PSEventViewer.Rules.ActiveDirectory {
    /// Active Directory Computer Change Detailed
    /// 5136: A directory service object was modified
    /// 5137: A directory service object was created
    /// 5139: A directory service object was deleted
    /// 5141: A directory service object was moved
    public class ADComputerChangeDetailed : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ObjectClass;
        public string OperationType;
        public string Who;
        public DateTime When;
        public string ComputerObject; // 'Computer Object'
        public string FieldChanged;
        public string FieldValue;

        public ADComputerChangeDetailed(EventObject eventObject) : base(eventObject) {
            // common fields
            _eventObject = eventObject;
            Type = "ADComputerChangeDetailed";
            Computer = _eventObject.ComputerName;
            ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
            Action = _eventObject.MessageSubject;
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
            // 
            OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
            ComputerObject = _eventObject.GetValueFromDataDictionary("ObjectDN");
            FieldChanged = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
            FieldValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
            // OverwriteByField logic
            ComputerObject = OverwriteByField(Action, "A directory service object was moved.", ComputerObject, _eventObject.GetValueFromDataDictionary("OldObjectDN"));
            FieldValue = OverwriteByField(Action, "A directory service object was moved.", FieldValue, _eventObject.GetValueFromDataDictionary("NewObjectDN"));

        }
    }
}
