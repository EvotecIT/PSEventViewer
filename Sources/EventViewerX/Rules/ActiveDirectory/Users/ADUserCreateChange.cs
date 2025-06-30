namespace EventViewerX.Rules.ActiveDirectory;

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
