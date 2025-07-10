namespace EventViewerX.Rules.Windows;

/// <summary>
/// Operating system startup event from Security log
/// Event ID 4608
/// </summary>
public class OSStartupSecurity : EventRuleBase {
    public override List<int> EventIds => new() { 4608 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.OSStartupSecurity;

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

    public OSStartupSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSStartupSecurity";
        Computer = _eventObject.ComputerName;
        Action = "Windows is starting up";
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
