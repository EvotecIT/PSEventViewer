namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Security Full
/// 1104: The security log is now full
/// </summary>
public class LogsFullSecurity : EventRuleBase {
    public override List<int> EventIds => new() { 1104 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.LogsFullSecurity;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public OperationType LogType;
    public string Who;
    public DateTime When;

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
