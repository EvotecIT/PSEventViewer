using EventViewerX;
namespace EventViewerX.Rules.Kerberos;

[NamedEvent(NamedEvents.KerberosServiceTicket, "Security", 4769, 4770)]
public class KerberosServiceTicket : EventObjectSlim
{
    public string Computer;
    public string Action;
    public string AccountName;
    public string ServiceName;
    public string IpAddress;
    public string IpPort;
    public DateTime When;

    public KerberosServiceTicket(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosServiceTicket";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        AccountName = _eventObject.GetValueFromDataDictionary("AccountName");
        ServiceName = _eventObject.GetValueFromDataDictionary("ServiceName");
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        When = _eventObject.TimeCreated;
    }
}
