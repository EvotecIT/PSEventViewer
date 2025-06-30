using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// External device recognized by the system
/// </summary>
[NamedEvent(NamedEvents.DeviceRecognized, "Security", 6416)]
public class DeviceRecognized : EventObjectSlim {
    public string Computer;
    public string DeviceId;
    public string DeviceName;
    public DateTime When;

    public DeviceRecognized(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DeviceRecognized";
        Computer = _eventObject.ComputerName;
        DeviceId = _eventObject.GetValueFromDataDictionary("DeviceId");
        DeviceName = _eventObject.GetValueFromDataDictionary("FriendlyName");
        When = _eventObject.TimeCreated;
    }
}
