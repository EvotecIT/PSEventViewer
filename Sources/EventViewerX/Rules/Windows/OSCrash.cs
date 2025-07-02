namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows OS Crash
/// 6008: The previous system shutdown at time on date was unexpected.
/// </summary>
public class OSCrash : EventRuleBase {
    public override List<int> EventIds => new() { 6008 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.OSCrash;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public string ActionDetailsDate;
    public string ActionDetailsTime;
    public string Who;
    public DateTime When;

    public OSCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "OSCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.GetValueFromDataDictionary("EventAction");
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        ActionDetailsDate = _eventObject.GetValueFromDataDictionary("NoNameA1");
        ActionDetailsTime = _eventObject.GetValueFromDataDictionary("NoNameA0");

        When = _eventObject.TimeCreated;
    }
}
