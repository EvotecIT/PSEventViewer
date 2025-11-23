namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Security Full
/// 1104: The security log is now full
/// </summary>
public class LogsFullSecurity : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 1104 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.LogsFullSecurity;

    /// <summary>Accepts matching events indicating the security log reached capacity.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Server where the log filled up.</summary>
    public string Computer;
    /// <summary>Message describing the condition.</summary>
    public string Action;
    /// <summary>Channel that reported the full condition.</summary>
    public string LogType;
    /// <summary>Account that triggered/observed the condition.</summary>
    public string Who;
    /// <summary>Timestamp when the log became full.</summary>
    public DateTime When;

    /// <summary>Initialises a wrapper for security log full events.</summary>
    public LogsFullSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "LogsFullSecurity";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        LogType = ConvertFromOperationType(_eventObject.Data["Channel"]);

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
