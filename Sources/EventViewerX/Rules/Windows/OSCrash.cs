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
        return RuleHelpers.IsProvider(eventObject, "EventLog", "Microsoft-Windows-Eventlog");
    }
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public DateTime? ActionTimestampUtc;
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    public string Who;
    public DateTime When;

    public OSCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "OSCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.GetValueFromDataDictionary("EventAction");
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        var rawStartText = _eventObject.GetValueFromDataDictionary("StartTime") ??
                           _eventObject.GetValueFromDataDictionary("#text") ??
                           _eventObject.GetValueFromDataDictionary("ActionDetailsDateTime");

        ActionTimestampUtc = RuleHelpers.ParseUnlabeledOsTimestamp(_eventObject)
                            ?? RuleHelpers.ParseDateTimeLoose(rawStartText)
                            ?? _eventObject.TimeCreated.ToUniversalTime();

        When = ActionTimestampUtc ?? _eventObject.TimeCreated;

        if (string.IsNullOrWhiteSpace(Action)) {
            Action = "Unexpected system shutdown";
        }
    }
}
