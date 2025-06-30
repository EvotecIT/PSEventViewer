using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// A user account was enabled, disabled, unlocked, password changed, password reset, or deleted
/// </summary>
[NamedEvent(NamedEvents.ADUserStatus, "Security", 4722, 4725, 4723, 4724, 4726)]
public class ADUserStatus : EventObjectSlim {
    public string Computer;
    public string Action;
    public string Who;
    public DateTime When;
    public string UserAffected;

    public ADUserStatus(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserStatus";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
    }
}
