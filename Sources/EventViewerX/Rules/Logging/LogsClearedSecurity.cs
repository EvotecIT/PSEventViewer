using EventViewerX;
namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Cleared Security
/// 1102: The audit log was cleared
/// 1105: Event log automatic backup
/// </summary>
[NamedEvent(NamedEvents.LogsClearedSecurity, "Security", 1102, 1105)]
public class LogsClearedSecurity : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public LogsClearedSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "LogsClearedSecurity";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
