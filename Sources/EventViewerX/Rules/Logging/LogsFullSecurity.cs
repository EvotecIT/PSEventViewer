using EventViewerX;
namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Security Full
/// </summary>
[NamedEvent(NamedEvents.LogsFullSecurity, "Security", 1104)]
public class LogsFullSecurity : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public LogsFullSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "LogsFullSecurity";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
