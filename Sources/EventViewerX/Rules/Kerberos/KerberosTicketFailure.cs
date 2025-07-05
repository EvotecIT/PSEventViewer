namespace EventViewerX.Rules.Kerberos;

/// <summary>
/// Represents a failed Kerberos ticket request event.
/// </summary>
public class KerberosTicketFailure : EventRuleBase
{
    public override List<int> EventIds => new() { 4771, 4772 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.KerberosTicketFailure;

    public override bool CanHandle(EventObject eventObject)
    {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string AccountName;
    public string FailureCode;
    public string IpAddress;
    public string IpPort;
    public TicketEncryptionType? EncryptionType;
    public bool WeakEncryptionAlgorithm;
    public DateTime When;

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

