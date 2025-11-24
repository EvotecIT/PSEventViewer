namespace EventViewerX.Rules.Windows;

/// <summary>
/// System bugcheck event
/// 1001: The computer has rebooted from a bugcheck.
/// </summary>
public class OSBugCheck : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 1001 };
    /// <inheritdoc />
    public override string LogName => "System";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.OSBugCheck;

    /// <summary>Accepts any bugcheck (1001) event in the System log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Computer where the bugcheck occurred.</summary>
    public string Computer;
    /// <summary>Bugcheck code.</summary>
    public string BugCheckCode;
    /// <summary>First bugcheck parameter.</summary>
    public string Parameter1;
    /// <summary>Second bugcheck parameter.</summary>
    public string Parameter2;
    /// <summary>Third bugcheck parameter.</summary>
    public string Parameter3;
    /// <summary>Fourth bugcheck parameter.</summary>
    public string Parameter4;
    /// <summary>Path to created dump file.</summary>
    public string DumpFile;
    /// <summary>Report identifier.</summary>
    public string ReportId;
    /// <summary>Event time.</summary>
    public DateTime When;

    /// <summary>Initialises a bugcheck wrapper from an event record.</summary>
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
