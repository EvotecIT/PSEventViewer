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
    public DateTime? ActionTimestampUtc;
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    public DateTime When;

    public OSStartupSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSStartupSecurity";
        Computer = _eventObject.ComputerName;
        Action = "Windows is starting up";
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        var rawStartText = _eventObject.GetValueFromDataDictionary("StartTime") ??
                           _eventObject.GetValueFromDataDictionary("#text") ??
                           _eventObject.GetValueFromDataDictionary("ActionDetailsDateTime");
        ActionTimestampUtc = RuleHelpers.ParseUnlabeledOsTimestamp(_eventObject)
                            ?? RuleHelpers.ParseDateTimeLoose(rawStartText)
                            ?? _eventObject.TimeCreated.ToUniversalTime();
        When = ActionTimestampUtc ?? _eventObject.TimeCreated;
    }
}
