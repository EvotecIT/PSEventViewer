using System;
using System.Collections.Generic;
using System.Text;

namespace PSEventViewer.Rules.ActiveDirectory {
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

    }

}
