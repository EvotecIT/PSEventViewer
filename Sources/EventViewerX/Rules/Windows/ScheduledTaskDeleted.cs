using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// Scheduled task deleted
/// </summary>
[NamedEvent(NamedEvents.ScheduledTaskDeleted, "Security", 4699)]
public class ScheduledTaskDeleted : EventObjectSlim {
    public string Computer;
    public string Action;
    public string TaskName;
    public DateTime When;

    public ScheduledTaskDeleted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ScheduledTaskDeleted";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        TaskName = _eventObject.GetValueFromDataDictionary("TaskName");
        When = _eventObject.TimeCreated;
    }
}
