namespace EventViewerX.Rules.Windows;

/// <summary>
/// Scheduled task created
/// 4698: A scheduled task was created
/// </summary>
public class ScheduledTaskCreated : EventRuleBase {
    public override List<int> EventIds => new() { 4698 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ScheduledTaskCreated;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string TaskName;
    public string Author;
    public DateTime? Created;
    public DateTime When;

    public ScheduledTaskCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ScheduledTaskCreated";
        Computer = _eventObject.ComputerName;
        TaskName = _eventObject.GetValueFromDataDictionary("TaskName");
        var taskContent = _eventObject.GetValueFromDataDictionary("TaskContent");
        if (!string.IsNullOrEmpty(taskContent)) {
            try {
                var xml = System.Xml.Linq.XDocument.Parse(taskContent);
                Author = xml.Root?.Element("RegistrationInfo")?.Element("Author")?.Value ?? string.Empty;
                var createdStr = xml.Root?.Element("RegistrationInfo")?.Element("Date")?.Value;
                if (DateTime.TryParse(createdStr, out var dt)) {
                    Created = dt;
                }
            } catch {
                // ignore malformed XML
            }
        }
        When = _eventObject.TimeCreated;
    }
}

