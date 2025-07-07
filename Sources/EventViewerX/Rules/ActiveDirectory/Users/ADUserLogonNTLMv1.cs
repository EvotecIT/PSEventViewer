namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Handles NTLMv1 logon events (4624).
/// </summary>
public class ADUserLogonNTLMv1 : EventRuleBase {
    /// <summary>Machine where the logon occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Source IP address.</summary>
    public string IpAddress;
    /// <summary>Source port number.</summary>
    public string IpPort;
    /// <summary>Account that logged on.</summary>
    public string ObjectAffected;
    /// <summary>User initiating the logon.</summary>
    public string Who;
    /// <summary>Time of the logon.</summary>
    public DateTime When;
    /// <summary>Logon process name.</summary>
    public string LogonProcessName;
    /// <summary>Impersonation level used.</summary>
    public ImpersonationLevel? ImpersonationLevel;
    /// <summary>Whether a virtual account was used.</summary>
    public VirtualAccount? VirtualAccount;
    /// <summary>Whether an elevated token was used.</summary>
    public ElevatedToken? ElevatedToken;
    /// <summary>Logon type value.</summary>
    public LogonType? LogonType;
    /// <summary>LM package name.</summary>
    public string LmPackageName;
    /// <summary>Authentication package name.</summary>
    public string PackageName;
    /// <summary>Length of the key.</summary>
    public string KeyLength;
    /// <summary>Caller process identifier.</summary>
    public string ProcessId;
    /// <summary>Caller process name.</summary>
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

