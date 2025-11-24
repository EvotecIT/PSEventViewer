namespace EventViewerX.Rules.Windows;

/// <summary>
/// Operating system shutdown event
/// Event ID 13
/// </summary>
public class OSShutdown : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 13 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.OSShutdown;

    /// <summary>Accepts kernel general shutdown events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Kernel-General");
    }

    /// <summary>Machine that generated the shutdown event.</summary>
    public string Computer;
    /// <summary>Action name (Shutdown).</summary>
    public string Action;
    /// <summary>Object affected by the action (typically the host).</summary>
    public string ObjectAffected;
    /// <summary>Additional details from the event payload.</summary>
    public string ActionDetails;
    /// <summary>Timestamp in UTC parsed from the event if present.</summary>
    public DateTime? ActionTimestampUtc;
    /// <summary>ISO-8601 representation of the UTC timestamp.</summary>
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises an OS shutdown wrapper from an event record.</summary>
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
