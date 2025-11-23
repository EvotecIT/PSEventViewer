namespace EventViewerX.Rules.Windows;

/// <summary>
/// Operating system rebooted without clean shutdown
/// Event ID 41
/// </summary>
public class OSUncleanShutdown : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 41 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.OSUncleanShutdown;

    /// <summary>Accepts kernel power events indicating unclean shutdowns.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Kernel-Power");
    }

    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public DateTime? ActionTimestampUtc;
    public string ActionTimestampIso => ActionTimestampUtc?.ToString("o") ?? string.Empty;
    public DateTime When;

    /// <summary>Initialises an unclean shutdown wrapper from an event record.</summary>
    public OSUncleanShutdown(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSUncleanShutdown";
        Computer = _eventObject.ComputerName;
        Action = "System Dirty Reboot";
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
