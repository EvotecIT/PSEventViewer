using EventViewerX;
namespace EventViewerX.Rules.Kerberos;

[NamedEvent(NamedEvents.KerberosTicketFailure, "Security", 4771, 4772)]
public class KerberosTicketFailure : EventObjectSlim
{
    public string Computer;
    public string Action;
    public string AccountName;
    public string FailureCode;
    public string IpAddress;
    public string IpPort;
    public DateTime When;

    public KerberosTicketFailure(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosTicketFailure";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        AccountName = _eventObject.GetValueFromDataDictionary("AccountName");
        FailureCode = _eventObject.GetValueFromDataDictionary("Status");
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        When = _eventObject.TimeCreated;
    }
}
