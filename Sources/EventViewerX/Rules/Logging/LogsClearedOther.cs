using EventViewerX;
namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Cleared Application, System, Others
/// </summary>
[NamedEvent(NamedEvents.LogsClearedOther, "System", 104)]
public class LogsClearedOther : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public LogsClearedOther(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "LogsClearedOther";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
