namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Cleared Application, System, Others
/// 104: The audit log was cleared
/// </summary>
public class LogsClearedOther : EventRuleBase {
    public override List<int> EventIds => new() { 104 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.LogsClearedOther;

    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "EventLog");
    }
    public string Computer;
    public string Action;
    public string BackupPath;
    public string LogType;
    public string Who;
    public DateTime When;

    public LogsClearedOther(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "LogsClearedOther";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        BackupPath = _eventObject.GetValueFromDataDictionary("BackupPath");
        LogType = ConvertFromOperationType(_eventObject.Data["Channel"]);

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;


        if (BackupPath == "") {
            BackupPath = "N/A";
        }
    }
}
