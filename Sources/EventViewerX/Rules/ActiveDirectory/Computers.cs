using System;

namespace EventViewerX.Rules.ActiveDirectory {
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

    /// <summary>
    /// Active Directory Computer Created or Changed
    /// 4741: A computer account was created
    /// 4742: A computer account was changed
    /// </summary>
    public class ADComputerCreateChange : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ComputerAffected;
        public string SamAccountName;
        public string DisplayName;
        public string UserPrincipalName;
        public string HomeDirectory;
        public string HomePath;
        public string ScriptPath;
        public string ProfilePath;
        public string UserWorkstations;
        public string PasswordLastSet;
        public string AccountExpires;
        public string PrimaryGroupId;
        public string AllowedToDelegateTo;
        public string OldUacValue;
        public string NewUacValue;
        public string UserAccountControl;
        public string UserParameters;
        public string SidHistory;
        public string LogonHours;
        public string DnsHostName;
        public string ServicePrincipalNames;
        public string Who;
        public DateTime When;

        public ADComputerCreateChange(EventObject eventObject) : base(eventObject) {
            // common fields
            _eventObject = eventObject;
            Type = "ADComputerChange";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ComputerAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
            SamAccountName = _eventObject.GetValueFromDataDictionary("SamAccountName");
            DisplayName = _eventObject.GetValueFromDataDictionary("DisplayName");
            UserPrincipalName = _eventObject.GetValueFromDataDictionary("UserPrincipalName");
            HomeDirectory = _eventObject.GetValueFromDataDictionary("HomeDirectory");
            HomePath = _eventObject.GetValueFromDataDictionary("HomePath");
            ScriptPath = _eventObject.GetValueFromDataDictionary("ScriptPath");
            ProfilePath = _eventObject.GetValueFromDataDictionary("ProfilePath");
            UserWorkstations = _eventObject.GetValueFromDataDictionary("UserWorkstations");
            PasswordLastSet = _eventObject.GetValueFromDataDictionary("PasswordLastSet");
            AccountExpires = _eventObject.GetValueFromDataDictionary("AccountExpires");
            PrimaryGroupId = _eventObject.GetValueFromDataDictionary("PrimaryGroupId");
            AllowedToDelegateTo = _eventObject.GetValueFromDataDictionary("AllowedToDelegateTo");
            OldUacValue = _eventObject.GetValueFromDataDictionary("OldUacValue");
            NewUacValue = _eventObject.GetValueFromDataDictionary("NewUacValue");
            UserAccountControl = _eventObject.GetValueFromDataDictionary("UserAccountControl");
            UserParameters = _eventObject.GetValueFromDataDictionary("UserParameters");
            SidHistory = _eventObject.GetValueFromDataDictionary("SidHistory");
            LogonHours = _eventObject.GetValueFromDataDictionary("LogonHours");
            DnsHostName = _eventObject.GetValueFromDataDictionary("DnsHostName");
            ServicePrincipalNames = _eventObject.GetValueFromDataDictionary("ServicePrincipalNames");
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;

            // let's try to translate them
            OldUacValue = TranslateUacValue(OldUacValue);
            NewUacValue = TranslateUacValue(NewUacValue);
            UserAccountControl = TranslateUacValue(UserAccountControl);
        }
    }

    /// <summary>
    /// Active Directory Computer Deleted
    /// 4743: A computer account was deleted
    /// </summary>
    public class ADComputerDeleted : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ComputerAffected;
        public string Who;
        public DateTime When;

        public ADComputerDeleted(EventObject eventObject) : base(eventObject) {
            // common fields
            _eventObject = eventObject;
            Type = "ADComputerDeleted";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            ComputerAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }
}
