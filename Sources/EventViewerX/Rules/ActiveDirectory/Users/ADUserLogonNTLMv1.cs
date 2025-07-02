namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon NTLMv1
/// 4624: An account was successfully logged on (NTLMv1)
/// </summary>
public class ADUserLogonNTLMv1 : EventRuleBase {
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
    public string LmPackageName;
    public string PackageName;
    public string KeyLength;
    public string ProcessId;
    public string ProcessName;

    public override List<int> EventIds => new() { 4624 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserLogonNTLMv1;

    public override bool CanHandle(EventObject eventObject) {
        // Only handle NTLMv1 logons
        return eventObject.ValueMatches("LmPackageName", "NTLM V1");
    }

    public ADUserLogonNTLMv1(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogonNTLMv1";

        // Copy all the base ADUserLogon fields
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

        // Additional NTLMv1-specific fields
        LmPackageName = _eventObject.GetValueFromDataDictionary("LmPackageName");
        PackageName = _eventObject.GetValueFromDataDictionary("AuthenticationPackageName");
        KeyLength = _eventObject.GetValueFromDataDictionary("KeyLength");
        ProcessId = _eventObject.GetValueFromDataDictionary("ProcessId");
        ProcessName = _eventObject.GetValueFromDataDictionary("ProcessName");
    }
}

