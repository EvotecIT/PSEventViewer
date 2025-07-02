namespace EventViewerX.Rules.Windows;

/// <summary>
/// System bugcheck event
/// 1001: The computer has rebooted from a bugcheck.
/// </summary>
public class OSBugCheck : EventRuleBase {
    public override List<int> EventIds => new() { 1001 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.OSBugCheck;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string BugCheckCode;
    public string Parameter1;
    public string Parameter2;
    public string Parameter3;
    public string Parameter4;
    public string DumpFile;
    public string ReportId;
    public DateTime When;

    public OSBugCheck(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSBugCheck";
        Computer = _eventObject.ComputerName;
        BugCheckCode = _eventObject.GetValueFromDataDictionary("BugcheckCode", "param1");
        Parameter1 = _eventObject.GetValueFromDataDictionary("BugcheckParameter1", "param2");
        Parameter2 = _eventObject.GetValueFromDataDictionary("BugcheckParameter2", "param3");
        Parameter3 = _eventObject.GetValueFromDataDictionary("BugcheckParameter3", "param4");
        Parameter4 = _eventObject.GetValueFromDataDictionary("BugcheckParameter4", "param5");
        DumpFile = _eventObject.GetValueFromDataDictionary("DumpFile");
        ReportId = _eventObject.GetValueFromDataDictionary("ReportId");
        When = _eventObject.TimeCreated;
    }
}
