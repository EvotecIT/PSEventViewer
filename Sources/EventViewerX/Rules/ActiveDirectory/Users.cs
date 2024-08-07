﻿namespace EventViewerX.Rules.ActiveDirectory {
    /// <summary>
    /// Includes users added or modified in Active Directory
    /// 4720: A user account was created
    /// 4738: A user account was changed
    /// </summary>
    public class ADUserCreateChange : EventObjectSlim {
        public string Computer;
        public string Action;
        public string UserAffected;
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
        public string Who;
        public DateTime When;

        public ADUserCreateChange(EventObject eventObject) : base(eventObject) {
            // main object initialization
            _eventObject = eventObject;
            // dedicated properties initialization
            Type = "ADUserChange";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
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
            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;

            // let's try to translate them
            OldUacValue = TranslateUacValue(OldUacValue);
            NewUacValue = TranslateUacValue(NewUacValue);
            UserAccountControl = TranslateUacValue(UserAccountControl);
        }
    }

    /// <summary>
    /// A user account was enabled, disabled, unlocked, password changed, password reset, or deleted
    /// 4722: A user account was enabled (this includes computer accounts)
    /// 4725: A user account was disabled (this includes computer accounts)
    /// 4767: A user account was unlocked
    /// 4723: An attempt was made to change an account's password
    /// 4724: An attempt was made to reset an account's password
    /// 4726: A user account was deleted
    /// </summary>
    public class ADUserStatus : EventObjectSlim {

        public string Computer;
        public string Action;
        public string Who;
        public DateTime When;
        public string UserAffected;

        public ADUserStatus(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADUsersStatus";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

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


    /// <summary>
    /// Active Directory User Lockouts
    /// 4740: A user account was locked out
    /// </summary>
    public class ADUserLockouts : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ComputerLockoutOn;
        public string UserAffected;
        public string Who;
        public DateTime When;

        public ADUserLockouts(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADUserLockouts";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            ComputerLockoutOn = _eventObject.GetValueFromDataDictionary("TargetDomainName");

            UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

    /// <summary>
    /// Active Directory User Logon
    /// 4624: An account was successfully logged on
    /// </summary>
    public class ADUserLogon : EventObjectSlim {
        public string Computer;
        public string Action;
        public string IpAddress;
        public string IpPort;
        public string ObjectAffected;
        public string Who;
        public DateTime When;
        public string LogonProcessName;
        public ImpersonationLevel? ImpersonationLevel;
        public VirtualAccount? VirtualAccount;
        public ElevatedToken? ElevatedToken;
        public LogonType? LogonType;

        public ADUserLogon(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADUserLogon";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
            IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
            LogonProcessName = _eventObject.GetValueFromDataDictionary("LogonProcessName");
            ImpersonationLevel = EventsHelper.GetImpersonationLevel(_eventObject.GetValueFromDataDictionary("ImpersonationLevel"));
            VirtualAccount = EventsHelper.GetVirtualAccount(_eventObject.GetValueFromDataDictionary("VirtualAccount"));
            ElevatedToken = EventsHelper.GetElevatedToken(_eventObject.GetValueFromDataDictionary("ElevatedToken"));
            LogonType = EventsHelper.GetLogonType(_eventObject.GetValueFromDataDictionary("LogonType"));

            ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }

    /// <summary>
    /// Active Directory User Logon Kerberos
    /// 4768: A Kerberos authentication ticket (TGT) was requested
    /// </summary>
    public class ADUserLogonKerberos : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ObjectAffected;
        public string IpAddress;
        public string IpPort;
        public string TicketOptions;
        public string Status;
        public string TicketEncryptionType;
        public string PreAuthType;
        public DateTime When;

        public ADUserLogonKerberos(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADUserLogonKerberos";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
            IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
            IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
            TicketOptions = _eventObject.GetValueFromDataDictionary("TicketOptions");
            Status = _eventObject.GetValueFromDataDictionary("Status");
            TicketEncryptionType = _eventObject.GetValueFromDataDictionary("TicketEncryptionType");
            PreAuthType = _eventObject.GetValueFromDataDictionary("PreAuthType");

            When = _eventObject.TimeCreated;

            if (IpAddress == "::1") {
                IpAddress = "Localhost";
            }
        }
    }

    /// <summary>
    /// Active Directory User Unlocked
    /// 4767: A user account was unlocked
    /// </summary>
    public class ADUserUnlocked : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ComputerLockoutOn;
        public string UserAffected;
        public string Who;
        public DateTime When;

        public ADUserUnlocked(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADUserUnlocked";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;

            ComputerLockoutOn = _eventObject.GetValueFromDataDictionary("TargetDomainName");

            UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

            Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
            When = _eventObject.TimeCreated;
        }
    }
}
