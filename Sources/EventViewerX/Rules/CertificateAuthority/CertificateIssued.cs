using EventViewerX;
namespace EventViewerX.Rules.CertificateAuthority;

/// <summary>
/// Certificate issued by Certificate Authority
/// </summary>
[NamedEvent(NamedEvents.CertificateIssued, "Security", 4886, 4887)]
public class CertificateIssued : EventObjectSlim
{
    public string Computer;
    public string Action;
    public string Requester;
    public string Template;
    public string SerialNumber;
    public DateTime When;

    public CertificateIssued(EventObject eventObject) : base(eventObject)
    {
        _eventObject = eventObject;
        Type = "CertificateIssued";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        Requester = _eventObject.GetValueFromDataDictionary("RequestIdString");
        Template = _eventObject.GetValueFromDataDictionary("CertificateTemplateOid");
        SerialNumber = _eventObject.GetValueFromDataDictionary("SerialNumber");
        When = _eventObject.TimeCreated;
    }
}
