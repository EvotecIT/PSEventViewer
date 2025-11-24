namespace EventViewerX.Rules.Windows;

/// <summary>
/// Device was disabled
/// 6420: A device was disabled.
/// </summary>
public class DeviceDisabled : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 6420 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.DeviceDisabled;

    /// <summary>Accepts device disabled events (6420) in the Security log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Machine where the device was disabled.</summary>
    public string Computer;
    /// <summary>Device identifier (PnP device ID).</summary>
    public string DeviceId;
    /// <summary>Friendly device name/description.</summary>
    public string DeviceName;
    /// <summary>Device class GUID.</summary>
    public string ClassId;
    /// <summary>Device class name.</summary>
    public string ClassName;
    /// <summary>Reason provided for disabling the device.</summary>
    public string Reason;
    /// <summary>Account that disabled the device.</summary>
    public string Who;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises a device-disabled wrapper from an event record.</summary>
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
