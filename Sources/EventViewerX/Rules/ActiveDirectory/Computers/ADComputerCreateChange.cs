using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Computer Created or Changed
/// 4741: A computer account was created
/// 4742: A computer account was changed
/// </summary>
[NamedEvent(NamedEvents.ADComputerCreateChange, "Security", 4741, 4742)]
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

        OldUacValue = TranslateUacValue(OldUacValue);
        NewUacValue = TranslateUacValue(NewUacValue);
        UserAccountControl = TranslateUacValue(UserAccountControl);
    }
}
