namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents creation or modification of computer accounts.
/// Handles events 4741 and 4742.
/// </summary>
public class ADComputerCreateChange : EventRuleBase {
    public override List<int> EventIds => new() { 4741, 4742 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADComputerCreateChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Machine where the change occurred.</summary>
    public string Computer;
    /// <summary>Short description of the action.</summary>
    public string Action;
    /// <summary>Computer affected by the event.</summary>
    public string ComputerAffected;
    /// <summary>SamAccountName of the computer.</summary>
    public string SamAccountName;
    /// <summary>Display name of the computer.</summary>
    public string DisplayName;
    /// <summary>UPN of the computer account.</summary>
    public string UserPrincipalName;
    /// <summary>Home directory path.</summary>
    public string HomeDirectory;
    /// <summary>Home path.</summary>
    public string HomePath;
    /// <summary>Logon script path.</summary>
    public string ScriptPath;
    /// <summary>Profile path.</summary>
    public string ProfilePath;
    /// <summary>Allowed workstations.</summary>
    public string UserWorkstations;
    /// <summary>Password last set timestamp.</summary>
    public string PasswordLastSet;
    /// <summary>Account expiration timestamp.</summary>
    public string AccountExpires;
    /// <summary>Primary group identifier.</summary>
    public string PrimaryGroupId;
    /// <summary>Delegation targets.</summary>
    public string AllowedToDelegateTo;
    /// <summary>Old user account control value.</summary>
    public string OldUacValue;
    /// <summary>New user account control value.</summary>
    public string NewUacValue;
    /// <summary>Translated user account control.</summary>
    public string UserAccountControl;
    /// <summary>User parameters string.</summary>
    public string UserParameters;
    /// <summary>SID history list.</summary>
    public string SidHistory;
    /// <summary>Logon hours configuration.</summary>
    public string LogonHours;
    /// <summary>DNS hostname of the computer.</summary>
    public string DnsHostName;
    /// <summary>Service principal names assigned.</summary>
    public string ServicePrincipalNames;
    /// <summary>User performing the change.</summary>
    public string Who;
    /// <summary>Time of the change.</summary>
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