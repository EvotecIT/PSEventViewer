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
    /// <summary>Client source port.</summary>
    public string IpPort;
    /// <summary>Ticket options bitmask parsed from the event.</summary>
    public TicketOptions? TicketOptions;
    /// <summary>Status code reported by the KDC.</summary>
    public StatusCode? Status;
    /// <summary>Encryption type used for the ticket.</summary>
    public TicketEncryptionType? EncryptionType;
    /// <summary>Pre-authentication type used by the client.</summary>
    public PreAuthType? PreAuthType;
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
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        TicketOptions = EventsHelper.GetTicketOptions(_eventObject.GetValueFromDataDictionary("TicketOptions"));
        Status = EventsHelper.GetStatusCode(_eventObject.GetValueFromDataDictionary("Status"));
        EncryptionType = EventsHelper.GetTicketEncryptionType(_eventObject.GetValueFromDataDictionary("TicketEncryptionType"));
        PreAuthType = EventsHelper.GetPreAuthType(_eventObject.GetValueFromDataDictionary("PreAuthType"));
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

