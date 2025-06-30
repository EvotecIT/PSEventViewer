namespace EventViewerX.Rules.Windows;

/// <summary>
/// Scheduled task deleted
/// 4699: A scheduled task was deleted
/// </summary>
public class ScheduledTaskDeleted : EventObjectSlim {
    public string Computer;
    public string TaskName;
    public string Who;
    public DateTime When;

    public ScheduledTaskDeleted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ScheduledTaskDeleted";
        Computer = _eventObject.ComputerName;
        TaskName = _eventObject.GetValueFromDataDictionary("TaskName");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
