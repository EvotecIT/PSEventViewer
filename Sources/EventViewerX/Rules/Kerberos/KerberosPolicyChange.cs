using EventViewerX;
namespace EventViewerX.Rules.Kerberos;

[NamedEvent(NamedEvents.KerberosPolicyChange, "Security", 4713)]
public class KerberosPolicyChange : EventObjectSlim
{
    public string Computer;
    public string Who;
    public string PolicyChanges;
    public double? KerProxyMinutes;
    public double? KerMaxRDays;
    public double? KerMaxTHours;
    public DateTime When;

    public KerberosPolicyChange(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "KerberosPolicyChange";
        Computer = _eventObject.ComputerName;
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        PolicyChanges = _eventObject.MessageSubject;
        if (double.TryParse(_eventObject.GetValueFromDataDictionary("KerbProxyLifetime"), out var proxy))
        {
            KerProxyMinutes = proxy;
        }
        if (double.TryParse(_eventObject.GetValueFromDataDictionary("KerbMaximumRenewAge"), out var renew))
        {
            KerMaxRDays = renew;
        }
        if (double.TryParse(_eventObject.GetValueFromDataDictionary("KerbMaximumTicketAge"), out var ticket))
        {
            KerMaxTHours = ticket;
        }
        When = _eventObject.TimeCreated;
    }
}
