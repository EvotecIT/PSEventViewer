namespace EventViewerX.Rules.Windows;

/// <summary>
/// External device recognized by the system
/// 6416: A new external device was recognized by the System.
/// </summary>
public class DeviceRecognized : EventRuleBase {
    public override List<int> EventIds => new() { 6416 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.DeviceRecognized;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string DeviceId;
    public string DeviceName;
    public string ClassId;
    public string ClassName;
    public string VendorIds;
    public string CompatibleIds;
    public string LocationInformation;
    public string DeviceType;
    public string Vendor;
    public string Who;
    public DateTime When;

    public DeviceRecognized(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DeviceRecognized";
        Computer = _eventObject.ComputerName;
        DeviceId = _eventObject.GetValueFromDataDictionary("DeviceId");
        DeviceName = _eventObject.GetValueFromDataDictionary("DeviceDescription", "DeviceName");
        ClassId = _eventObject.GetValueFromDataDictionary("ClassId");
        ClassName = _eventObject.GetValueFromDataDictionary("ClassName");
        VendorIds = _eventObject.GetValueFromDataDictionary("VendorIds");
        CompatibleIds = _eventObject.GetValueFromDataDictionary("CompatibleIds");
        LocationInformation = _eventObject.GetValueFromDataDictionary("LocationInformation");
        DeviceType = EventsHelper.TranslateDeviceType(ClassName);
        Vendor = EventsHelper.TranslateVendor(VendorIds);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

