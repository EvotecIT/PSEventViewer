namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a Kerberos TGT request event.
/// </summary>
public class KerberosTGTRequest : EventObjectSlim
{
    public string Computer;
    public string Action;
    public string AccountName;
    public string IpAddress;
    public string IpPort;
    public TicketOptions? TicketOptions;
    public Status? Status;
    public TicketEncryptionType? EncryptionType;
    public PreAuthType? PreAuthType;
    public bool WeakEncryptionAlgorithm;
    public DateTime When;

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
        Status = EventsHelper.GetStatus(_eventObject.GetValueFromDataDictionary("Status"));
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

