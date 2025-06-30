namespace EventViewerX.Rules.ActiveDirectory;

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
