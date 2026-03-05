namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon
/// 4624: An account was successfully logged on
/// </summary>
public class ADUserLogon : EventRuleBase {
    /// <summary>
    /// Computer on which the logon occurred.
    /// </summary>
    public string Computer;

    /// <summary>
    /// Description of the logon action.
    /// </summary>
    public string Action;

    /// <summary>
    /// IP address of the source.
    /// </summary>
    public string IpAddress;

    /// <summary>
    /// Source port number.
    /// </summary>
    public string IpPort;

    /// <summary>
    /// Account that logged on.
    /// </summary>
    public string ObjectAffected;

    /// <summary>
    /// User initiating the logon.
    /// </summary>
    public string Who;

    /// <summary>
    /// Time when the logon happened.
    /// </summary>
    public DateTime When;

    /// <summary>
    /// Name of the logon process.
    /// </summary>
    public string LogonProcessName;

    /// <summary>
    /// Impersonation level used.
    /// </summary>
    public ImpersonationLevel? ImpersonationLevel;

    /// <summary>
    /// Indicates if a virtual account was used.
    /// </summary>
    public VirtualAccount? VirtualAccount;

    /// <summary>
    /// Indicates if an elevated token was used.
    /// </summary>
    public ElevatedToken? ElevatedToken;

    /// <summary>
    /// Logon type value.
    /// </summary>
    public LogonType? LogonType;

    /// <inheritdoc />
    public override List<int> EventIds => new() { 4624 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADUserLogon;

    /// <summary>Accepts any 4624 events in the Security log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Initialises a successful logon wrapper from an event record.</summary>
    public ADUserLogon(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogon";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        IpAddress = _eventObject.GetDataValueOrEmpty(KnownEventField.IpAddress);
        IpPort = _eventObject.GetDataValueOrEmpty(KnownEventField.IpPort);
        LogonProcessName = _eventObject.GetDataValueOrEmpty(KnownEventField.LogonProcessName);
        ImpersonationLevel = EventsHelper.GetImpersonationLevel(_eventObject.GetDataValueOrEmpty("ImpersonationLevel"));
        VirtualAccount = EventsHelper.GetVirtualAccount(_eventObject.GetDataValueOrEmpty("VirtualAccount"));
        ElevatedToken = EventsHelper.GetElevatedToken(_eventObject.GetDataValueOrEmpty("ElevatedToken"));
        LogonType = _eventObject.TryGetDataEnum(KnownEventField.LogonType, out EventViewerX.LogonType parsedLogonType, EventFieldNumericBase.Decimal)
            ? parsedLogonType
            : EventsHelper.GetLogonType(_eventObject.GetDataValueOrEmpty(KnownEventField.LogonType));

        ObjectAffected = _eventObject.GetTargetAccountOrEmpty();

        Who = _eventObject.GetSubjectAccountOrEmpty();
        When = _eventObject.TimeCreated;
    }
}


