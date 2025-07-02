namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a Kerberos service ticket request event.
/// </summary>
public class KerberosServiceTicket : EventObjectSlim
{
    public string Computer;
    public string Action;
    public string AccountName;
    public string ServiceName;
    public string IpAddress;
    public string IpPort;
    public string TicketOptions;
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

