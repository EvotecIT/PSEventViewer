namespace EventViewerX.Rules.Windows;

/// <summary>
/// Device was disabled
/// 6420: A device was disabled.
/// </summary>
public class DeviceDisabled : EventRuleBase {
    public override List<int> EventIds => new() { 6420 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.DeviceDisabled;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string DeviceId;
    public string DeviceName;
    public string ClassId;
    public string ClassName;
    public string Reason;
    public string Who;
    public DateTime When;

    public DeviceDisabled(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DeviceDisabled";
        Computer = _eventObject.ComputerName;
        DeviceId = _eventObject.GetValueFromDataDictionary("DeviceId");
        DeviceName = _eventObject.GetValueFromDataDictionary("DeviceDescription", "DeviceName");
        ClassId = _eventObject.GetValueFromDataDictionary("ClassId");
        ClassName = _eventObject.GetValueFromDataDictionary("ClassName");
        Reason = _eventObject.GetValueFromDataDictionary("Reason");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
