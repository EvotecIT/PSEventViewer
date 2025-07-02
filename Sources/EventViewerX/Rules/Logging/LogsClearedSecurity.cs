namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Cleared Security
/// 1102: The audit log was cleared
/// 1105: Event log automatic backup
/// Url: https://learn.microsoft.com/en-us/windows/security/threat-protection/auditing/event-1105
/// </summary>
public class LogsClearedSecurity : EventRuleBase {
    public override List<int> EventIds => new() { 1102, 1105 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.LogsClearedSecurity;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string BackupPath;
    public string LogType;
    public string Who;
    public DateTime When;

    public LogsClearedSecurity(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "LogsClearedSecurity";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        BackupPath = _eventObject.GetValueFromDataDictionary("BackupPath");
        LogType = ConvertFromOperationType(_eventObject.Data["Channel"]);

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;

        if (_eventObject.Id == 1105) {
            Who = "Automatic Backup";
        }
        if (BackupPath == "") {
            BackupPath = "N/A";
        }
    }

}
