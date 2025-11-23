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
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Kernel-General");
    }

    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public DateTime? ActionTimestampUtc;
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    public DateTime When;

    public OSShutdown(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSShutdown";
        Computer = _eventObject.ComputerName;
        Action = "System Shutdown";
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
