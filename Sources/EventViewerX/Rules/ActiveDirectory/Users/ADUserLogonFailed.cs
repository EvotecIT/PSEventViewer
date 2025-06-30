using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

[NamedEvent(NamedEvents.ADUserLogonFailed, "Security", 4625)]
public class ADUserLogonFailed : EventObjectSlim {
    public string Computer;
    public string Action;
    public string IpAddress;
    public string FailureReason;
    public string TargetUserName;
    public string TargetDomainName;
    public DateTime When;

    public ADUserLogonFailed(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogonFailed";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        FailureReason = _eventObject.GetValueFromDataDictionary("FailureReason");
        TargetUserName = _eventObject.GetValueFromDataDictionary("TargetUserName");
        TargetDomainName = _eventObject.GetValueFromDataDictionary("TargetDomainName");
        When = _eventObject.TimeCreated;
    }
}
