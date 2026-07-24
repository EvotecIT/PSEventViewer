namespace EventViewerX.Rules.Windows;

/// <summary>
/// External device recognized by the system
/// 6416: A new external device was recognized by the System.
/// </summary>
public class DeviceRecognized : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 6416 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.DeviceRecognized;

    /// <summary>Accepts device recognition events (6416) in the Security log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }
    /// <summary>Machine where the device was detected.</summary>
    public string Computer;
    /// <summary>Device identifier (PnP device ID).</summary>
    public string DeviceId;
    /// <summary>Friendly device name/description.</summary>
    public string DeviceName;
    /// <summary>Device class GUID.</summary>
    public string ClassId;
    /// <summary>Device class name.</summary>
    public string ClassName;
    /// <summary>Vendor identifiers.</summary>
    public string VendorIds;
    /// <summary>Compatible IDs list.</summary>
    public string CompatibleIds;
    /// <summary>Physical location info reported by PnP.</summary>
    public string LocationInformation;
    /// <summary>Translated device type based on class.</summary>
    public string DeviceType;
    /// <summary>Translated vendor string.</summary>
    public string Vendor;
    /// <summary>Account that initiated/triggered detection.</summary>
    public string Who;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises a device recognition wrapper from an event record.</summary>
    public DeviceRecognized(EventObject eventObject) : base(eventObject) {
        Event = eventObject;
        Type = "DeviceRecognized";
        Computer = Event.ComputerName;
        DeviceId = Event.GetValueFromDataDictionary("DeviceId");
        DeviceName = Event.GetValueFromDataDictionary("DeviceDescription", "DeviceName");
        ClassId = Event.GetValueFromDataDictionary("ClassId");
        ClassName = Event.GetValueFromDataDictionary("ClassName");
        VendorIds = Event.GetValueFromDataDictionary("VendorIds");
        CompatibleIds = Event.GetValueFromDataDictionary("CompatibleIds");
        LocationInformation = Event.GetValueFromDataDictionary("LocationInformation");
        DeviceType = EventsHelper.TranslateDeviceType(ClassName);
        Vendor = EventsHelper.TranslateVendor(VendorIds);
        Who = Event.GetSubjectAccountOrEmpty();
        When = Event.TimeCreated;
    }
}



