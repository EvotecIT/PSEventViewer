namespace EventViewerX.Rules.Windows;

/// <summary>
/// Operating system shutdown event
/// Event ID 13
/// </summary>
public class OSShutdown : EventRuleBase {
    public override List<int> EventIds => new() { 13 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.OSShutdown;

    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public string ActionDetailsDate;
    public string ActionDetailsTime;
    public string ActionDetailsDateTime;
    public DateTime When;

    public OSShutdown(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSShutdown";
        Computer = _eventObject.ComputerName;
        Action = "System Shutdown";
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        ActionDetailsDate = _eventObject.GetValueFromDataDictionary("NoNameA1");
        ActionDetailsTime = _eventObject.GetValueFromDataDictionary("NoNameA0");
        ActionDetailsDateTime = _eventObject.GetValueFromDataDictionary("ActionDetailsDateTime");
        When = _eventObject.TimeCreated;

        var startTime = _eventObject.GetValueFromDataDictionary("StartTime");
        if (startTime != null) {
            ActionDetailsDateTime = startTime;
        } else {
            var text = _eventObject.GetValueFromDataDictionary("#text");
            if (text != null) {
                ActionDetailsDateTime = text;
            }
        }
    }
}
