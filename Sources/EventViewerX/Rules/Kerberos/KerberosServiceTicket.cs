namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a Kerberos service ticket request event.
/// </summary>
public class KerberosServiceTicket : EventRuleBase
{
    public override List<int> EventIds => new() { 4769, 4770 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.KerberosServiceTicket;

    public override bool CanHandle(EventObject eventObject)
    {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer = string.Empty;
    public string Action = string.Empty;
    public string AccountName = string.Empty;
    public string ServiceName = string.Empty;
    public string IpAddress = string.Empty;
    public string IpPort = string.Empty;
    public string TicketOptions = string.Empty;
    public TicketEncryptionType? EncryptionType;
    public bool WeakEncryptionAlgorithm;
    public bool UnusualTicketOptions;
    public DateTime When;

    public KerberosServiceTicket(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosServiceTicket";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        AccountName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        ServiceName = _eventObject.GetValueFromDataDictionary("ServiceName");
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        TicketOptions = _eventObject.GetValueFromDataDictionary("TicketOptions");
        EncryptionType = EventsHelper.GetTicketEncryptionType(_eventObject.GetValueFromDataDictionary("TicketEncryptionType"));
        When = _eventObject.TimeCreated;

        WeakEncryptionAlgorithm = EncryptionType is TicketEncryptionType.DES_CBC_CRC
            or TicketEncryptionType.DES_CBC_MD5
            or TicketEncryptionType.RC4_HMAC
            or TicketEncryptionType.RC4_HMAC_EXP;

        UnusualTicketOptions = !(TicketOptions?.Equals("0x40810010", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

