namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a failed Kerberos ticket request event.
/// </summary>
public class KerberosTicketFailure : EventRuleBase
{
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4771, 4772 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.KerberosTicketFailure;

    /// <summary>Checks whether the supplied event is a Kerberos ticket failure from the security auditing provider.</summary>
    public override bool CanHandle(EventObject eventObject)
    {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Security-Auditing");
    }
    /// <summary>Domain controller processing the ticket.</summary>
    public string Computer;
    /// <summary>Action reported in the event (e.g., failure reason).</summary>
    public string Action;
    /// <summary>Account associated with the failed ticket request.</summary>
    public string AccountName;
    /// <summary>Status or failure code returned by the KDC.</summary>
    public string FailureCode;
    /// <summary>Source IP address.</summary>
    public string IpAddress;
    /// <summary>Source port.</summary>
    public string IpPort;
    /// <summary>Encryption type requested/used.</summary>
    public TicketEncryptionType? EncryptionType;
    /// <summary>True when a weak encryption algorithm was involved.</summary>
    public bool WeakEncryptionAlgorithm;
    /// <summary>Time the event was created.</summary>
    public DateTime When;

    /// <summary>Initialises a Kerberos ticket failure wrapper from an event record.</summary>
    public KerberosTicketFailure(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosTicketFailure";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        AccountName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        FailureCode = _eventObject.GetValueFromDataDictionary("Status");
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        EncryptionType = EventsHelper.GetTicketEncryptionType(_eventObject.GetValueFromDataDictionary("TicketEncryptionType"));
        When = _eventObject.TimeCreated;

        WeakEncryptionAlgorithm = EncryptionType is TicketEncryptionType.DES_CBC_CRC
            or TicketEncryptionType.DES_CBC_MD5
            or TicketEncryptionType.RC4_HMAC
            or TicketEncryptionType.RC4_HMAC_EXP;
    }
}

