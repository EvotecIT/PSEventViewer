using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon
/// 4624: An account was successfully logged on
/// </summary>
[NamedEvent(NamedEvents.ADUserLogon, "Security", 4624)]
public class ADUserLogon : EventObjectSlim {
    public string Computer;
    public string Action;
    public string IpAddress;
    public string LogonType;
    public string LogonProcess;
    public string AuthenticationPackage;
    public string LogonGuid;
    public string TargetDomainName;
    public string TargetUserName;
    public string SubjectDomainName;
    public string SubjectUserName;
    public DateTime When;

    public ADUserLogon(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogon";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        LogonType = _eventObject.GetValueFromDataDictionary("LogonType");
        LogonProcess = _eventObject.GetValueFromDataDictionary("LogonProcessName");
        AuthenticationPackage = _eventObject.GetValueFromDataDictionary("AuthenticationPackageName");
        LogonGuid = _eventObject.GetValueFromDataDictionary("LogonGuid");
        TargetDomainName = _eventObject.GetValueFromDataDictionary("TargetDomainName");
        TargetUserName = _eventObject.GetValueFromDataDictionary("TargetUserName");
        SubjectDomainName = _eventObject.GetValueFromDataDictionary("SubjectDomainName");
        SubjectUserName = _eventObject.GetValueFromDataDictionary("SubjectUserName");
        When = _eventObject.TimeCreated;
    }
}
