namespace EventViewerX.Rules.Windows;

/// <summary>
/// Scheduled task deleted
/// 4699: A scheduled task was deleted
/// </summary>
public class ScheduledTaskDeleted : EventRuleBase {
    public override List<int> EventIds => new() { 4699 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ScheduledTaskDeleted;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Computer where the task was deleted.</summary>
    public string Computer;
    /// <summary>Name of the task.</summary>
    public string TaskName;
    /// <summary>User that deleted the task.</summary>
    public string Who;
    /// <summary>Time the event occurred.</summary>
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

