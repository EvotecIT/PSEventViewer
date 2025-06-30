using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// System audit policy was changed
/// </summary>
[NamedEvent(NamedEvents.AuditPolicyChange, "Security", 4719)]
public class AuditPolicyChange : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public AuditPolicyChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "AuditPolicyChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
