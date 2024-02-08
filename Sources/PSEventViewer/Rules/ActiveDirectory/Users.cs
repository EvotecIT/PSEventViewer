using System;
using System.Collections.Generic;
using System.Text;

namespace PSEventViewer.Rules.ActiveDirectory {



    /// <summary>
    /// Includes users added or modified in Active Directory
    /// 4720: A user account was created
    /// 4738: A user account was changed
    /// </summary>
    public class ADUsersChange : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ObjectAffected;
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
        public string Who;
        public string Date;
        //'Computer' = 'Domain Controller'
        //'Action' = 'Action'
        //'ObjectAffected' = 'User Affected'
        //'SamAccountName' = 'SamAccountName'
        //'DisplayName' = 'DisplayName'
        //'UserPrincipalName' = 'UserPrincipalName'
        //'HomeDirectory' = 'Home Directory'
        //'HomePath' = 'Home Path'
        //'ScriptPath' = 'Script Path'
        //'ProfilePath' = 'Profile Path'
        //'UserWorkstations' = 'User Workstations'
        //'PasswordLastSet' = 'Password Last Set'
        //'AccountExpires' = 'Account Expires'
        //'PrimaryGroupId' = 'Primary Group Id'
        //'AllowedToDelegateTo' = 'Allowed To Delegate To'
        //'OldUacValue' = 'Old Uac Value'
        //'NewUacValue' = 'New Uac Value'
        //'UserAccountControl' = 'User Account Control'
        //'UserParameters' = 'User Parameters'
        //'SidHistory' = 'Sid History'
        //'Who' = 'Who'
        //'Date' = 'When'
        //# Common Fields
        //'ID' = 'Event ID'
        //'RecordID' = 'Record ID'
        //'GatheredFrom' = 'Gathered From'
        //'GatheredLogName' = 'Gathered LogName'


        public ADUsersChange(EventObject eventObject) : base(eventObject) {
            // Additional initialization code specific to UsersAdd
            _eventObject = eventObject;
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
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
            Who = _eventObject.GetValueFromDataDictionary("Who");
            Date = _eventObject.GetValueFromDataDictionary("Date");
        }
    }

    public class ADUsersStatus : EventObjectSlim {
        public ADUsersStatus(EventObject eventObject) : base(eventObject) {
            // Additional initialization code specific to UsersRemove
        }
    }

    public class ADUserChangeDetailed : EventObjectSlim {

        public ADUserChangeDetailed(EventObject eventObject) : base(eventObject) {
            // Additional initialization code specific to UsersModify
            _eventObject = eventObject;
        }
    }
}
