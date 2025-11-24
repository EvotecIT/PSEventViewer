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

    /// <summary>Machine that logged the startup in the Security log.</summary>
    public string Computer;
    /// <summary>Action description (Windows is starting up).</summary>
    public string Action;
    /// <summary>Object affected by the action (typically the host).</summary>
    public string ObjectAffected;
    /// <summary>Detail string from the security event.</summary>
    public string ActionDetails;
    /// <summary>Timestamp in UTC parsed from the payload when present.</summary>
    public DateTime? ActionTimestampUtc;
    /// <summary>ISO-8601 representation of the UTC timestamp.</summary>
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    /// <summary>Event timestamp.</summary>
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
