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
        return true;
    }

    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string ActionDetails;
    public string ActionDetailsDate;
    public string ActionDetailsTime;
    public string ActionDetailsDateTime;
    public DateTime When;

    public OSCrashOnAuditFailRecovery(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSCrashOnAuditFailRecovery";
        Computer = _eventObject.ComputerName;
        Action = "Administrator recovered system from CrashOnAuditFail";
        ObjectAffected = _eventObject.MachineName;
        ActionDetails = _eventObject.MessageSubject;
        ActionDetailsDate = _eventObject.GetValueFromDataDictionary("NoNameA1");
        ActionDetailsTime = _eventObject.GetValueFromDataDictionary("NoNameA0");
        ActionDetailsDateTime = _eventObject.GetValueFromDataDictionary("ActionDetailsDateTime");
        When = _eventObject.TimeCreated;

        var startTime = _eventObject.GetValueFromDataDictionary("StartTime");
        if (startTime != null) {
            ActionDetailsDateTime = startTime;
        } else {
            var text = _eventObject.GetValueFromDataDictionary("#text");
            if (text != null) {
                ActionDetailsDateTime = text;
            }
        }
    }
}
