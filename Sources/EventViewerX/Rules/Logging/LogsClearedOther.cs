namespace EventViewerX.Rules.Logging;

/// <summary>
/// Logs Cleared Application, System, Others
/// 104: The audit log was cleared
/// </summary>
public class LogsClearedOther : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 104 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.LogsClearedOther;

    /// <summary>Verifies the event originates from the EventLog provider.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return RuleHelpers.IsProvider(eventObject, "EventLog", "Microsoft-Windows-Eventlog");
    }
    /// <summary>Machine where the log was cleared.</summary>
    public string Computer;
    /// <summary>Action description from the event.</summary>
    public string Action;
    /// <summary>Backup path (if automatic backup occurred).</summary>
    public string BackupPath;
    /// <summary>Channel that was cleared.</summary>
    public string LogType;
    /// <summary>Account responsible for clearing/backing up the log.</summary>
    public string Who;
    /// <summary>Timestamp of the event.</summary>
    public DateTime When;

    /// <summary>Initialises a log-cleared (non-Security) event wrapper.</summary>
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
