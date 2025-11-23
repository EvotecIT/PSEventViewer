namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows OS Crash
/// 6008: The previous system shutdown at time on date was unexpected.
/// </summary>
public class OSCrash : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 6008 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.OSCrash;

    /// <summary>Accepts unexpected shutdown events from the EventLog provider.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "EventLog", "Microsoft-Windows-Eventlog");
    }
    /// <summary>Machine that experienced the crash.</summary>
    public string Computer;
    /// <summary>Description of the crash action.</summary>
    public string Action;
    /// <summary>Target object (machine name).</summary>
    public string ObjectAffected;
    /// <summary>Additional details from the event message.</summary>
    public string ActionDetails;
    /// <summary>Crash timestamp in UTC.</summary>
    public DateTime? ActionTimestampUtc;
    /// <summary>Crash timestamp in ISO-8601 format.</summary>
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    /// <summary>User field if available (often empty for system crashes).</summary>
    public string Who;
    /// <summary>Event time used for ordering.</summary>
    public DateTime When;

    /// <summary>Initialises an OS crash wrapper from an event record.</summary>
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
