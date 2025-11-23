namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents creation or modification of user accounts (events 4720 and 4738).
/// </summary>
public class ADUserCreateChange : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4720, 4738 };

    /// <inheritdoc />
    public override string LogName => "Security";

    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADUserCreateChange;

    /// <summary>Accepts matching create/change user events without extra filtering.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Machine where the change occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>User account affected.</summary>
    public string UserAffected;
    /// <summary>SamAccountName of the user.</summary>
    public string SamAccountName;
    /// <summary>Display name of the user.</summary>
    public string DisplayName;
    /// <summary>User principal name.</summary>
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
    /// <summary>Old UAC value.</summary>
    public string OldUacValue;
    /// <summary>New UAC value.</summary>
    public string NewUacValue;
    /// <summary>Translated UAC value.</summary>
    public string UserAccountControl;
    /// <summary>User parameters string.</summary>
    public string UserParameters;
    /// <summary>SID history list.</summary>
    public string SidHistory;
    /// <summary>Logon hours settings.</summary>
    public string LogonHours;
    /// <summary>User performing the change.</summary>
    public string Who;
    /// <summary>Time of the change.</summary>
    public DateTime When;

    /// <summary>Initialises a user creation/change wrapper from an event record.</summary>
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
