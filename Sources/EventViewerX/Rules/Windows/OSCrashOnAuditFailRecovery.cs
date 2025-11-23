namespace EventViewerX.Rules.Windows;

/// <summary>
/// Administrator recovered system from CrashOnAuditFail
/// Event ID 4621
/// </summary>
public class OSCrashOnAuditFailRecovery : EventRuleBase {
    public override List<int> EventIds => new() { 4621 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.OSCrashOnAuditFailRecovery;

    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Security-Auditing");
    }

    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public DateTime? ActionTimestampUtc;
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    public DateTime When;

    public OSCrashOnAuditFailRecovery(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSCrashOnAuditFailRecovery";
        Computer = _eventObject.ComputerName;
        Action = "Administrator recovered system from CrashOnAuditFail";
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
