namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a Kerberos TGT request event.
/// </summary>
public class KerberosTGTRequest : EventRuleBase
{
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4768 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.KerberosTGTRequest;

    /// <summary>Checks whether the supplied event originates from the security auditing provider.</summary>
    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Security-Auditing");
    }

    /// <summary>Domain controller that issued the TGT.</summary>
    public string Computer;
    /// <summary>Action reported by the event (e.g., issued, failed).</summary>
    public string Action;
    /// <summary>Target account requesting the ticket.</summary>
    public string AccountName;
    /// <summary>Client IP address.</summary>
    public string IpAddress;
    /// <summary>Client IP normalized (v4-mapped/loopback).</summary>
    public string IpAddressNormalized;
    /// <summary>Client source port.</summary>
    public string IpPort;
    /// <summary>Ticket options bitmask parsed from the event.</summary>
    public TicketOptions? TicketOptions;
    /// <summary>Human-friendly ticket options.</summary>
    public string TicketOptionsText;
    /// <summary>Status code reported by the KDC.</summary>
    public StatusCode? Status;
    /// <summary>Status with hex representation.</summary>
    public string StatusText;
    /// <summary>Encryption type used for the ticket.</summary>
    public TicketEncryptionType? EncryptionType;
    public string EncryptionTypeText;
    /// <summary>Pre-authentication type used by the client.</summary>
    public PreAuthType? PreAuthType;
    public string PreAuthTypeText;
    /// <summary>Session key encryption type (from SessionKeyEncryptionType).</summary>
    public TicketEncryptionType? SessionKeyEncryptionType;
    public string SessionKeyEncryptionTypeText;
    /// <summary>Pre-auth encryption type (from PreAuthEncryptionType).</summary>
    public TicketEncryptionType? PreAuthEncryptionType;
    public string PreAuthEncryptionTypeText;
    /// <summary>Client-advertised encryption types string.</summary>
    public string ClientAdvertizedEncryptionTypes;
    /// <summary>Supported/available encryption types reported by account/service/DC.</summary>
    public string AccountSupportedEncryptionTypes;
    public string AccountAvailableKeys;
    public string ServiceSupportedEncryptionTypes;
    public string ServiceAvailableKeys;
    public string DCSupportedEncryptionTypes;
    public string DCAvailableKeys;
    /// <summary>Response ticket hash when present.</summary>
    public string ResponseTicket;
    /// <summary>True when a weak encryption algorithm (e.g., RC4/DES) was used.</summary>
    public bool WeakEncryptionAlgorithm;
    /// <summary>Time the event was created.</summary>
    public DateTime When;

    /// <summary>Initialises a TGT request wrapper from an event record.</summary>
    public KerberosTGTRequest(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosTGTRequest";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        AccountName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpAddressNormalized = Rules.RuleHelpers.NormalizeIp(IpAddress);
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        var rawTicketOptions = _eventObject.GetValueFromDataDictionary("TicketOptions");
        TicketOptions = EventsHelper.GetTicketOptions(rawTicketOptions);
        TicketOptionsText = EventsHelper.DescribeTicketOptions(TicketOptions, rawTicketOptions);

        var rawStatus = _eventObject.GetValueFromDataDictionary("Status");
        Status = EventsHelper.GetStatusCode(rawStatus);
        StatusText = EventsHelper.DescribeStatus(Status, rawStatus);

        var rawTicketEtype = _eventObject.GetValueFromDataDictionary("TicketEncryptionType");
        EncryptionType = EventsHelper.GetTicketEncryptionType(rawTicketEtype);
        EncryptionTypeText = EventsHelper.DescribeEncryption(EncryptionType, rawTicketEtype);

        var rawPreAuth = _eventObject.GetValueFromDataDictionary("PreAuthType");
        PreAuthType = EventsHelper.GetPreAuthType(rawPreAuth);
        PreAuthTypeText = EventsHelper.DescribePreAuthType(PreAuthType, rawPreAuth);

        var rawSessionEtype = _eventObject.GetValueFromDataDictionary("SessionKeyEncryptionType");
        SessionKeyEncryptionType = EventsHelper.GetTicketEncryptionType(rawSessionEtype);
        SessionKeyEncryptionTypeText = EventsHelper.DescribeEncryption(SessionKeyEncryptionType, rawSessionEtype);

        var rawPreAuthEtype = _eventObject.GetValueFromDataDictionary("PreAuthEncryptionType");
        PreAuthEncryptionType = EventsHelper.GetTicketEncryptionType(rawPreAuthEtype);
        PreAuthEncryptionTypeText = EventsHelper.DescribeEncryption(PreAuthEncryptionType, rawPreAuthEtype);
        ClientAdvertizedEncryptionTypes = _eventObject.GetValueFromDataDictionary("ClientAdvertizedEncryptionTypes");
        AccountSupportedEncryptionTypes = _eventObject.GetValueFromDataDictionary("AccountSupportedEncryptionTypes");
        AccountAvailableKeys = _eventObject.GetValueFromDataDictionary("AccountAvailableKeys");
        ServiceSupportedEncryptionTypes = _eventObject.GetValueFromDataDictionary("ServiceSupportedEncryptionTypes");
        ServiceAvailableKeys = _eventObject.GetValueFromDataDictionary("ServiceAvailableKeys");
        DCSupportedEncryptionTypes = _eventObject.GetValueFromDataDictionary("DCSupportedEncryptionTypes");
        DCAvailableKeys = _eventObject.GetValueFromDataDictionary("DCAvailableKeys");
        ResponseTicket = _eventObject.GetValueFromDataDictionary("ResponseTicket");
        When = _eventObject.TimeCreated;

        WeakEncryptionAlgorithm = EncryptionType is TicketEncryptionType.DES_CBC_CRC
            or TicketEncryptionType.DES_CBC_MD5
            or TicketEncryptionType.RC4_HMAC
            or TicketEncryptionType.RC4_HMAC_EXP;

        if (IpAddress == "::1")
        {
            IpAddress = "Localhost";
        }
    }
}

