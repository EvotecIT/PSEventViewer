namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V VM shutdown event
/// 18560: The virtual machine was turned off
/// </summary>
public class VmShutdown : EventRuleBase {
    public override List<int> EventIds => new() { 18560 };
    public override string LogName => "Microsoft-Windows-Hyper-V-Worker/Operational";
    public override NamedEvents NamedEvent => NamedEvents.HyperVVmShutdown;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string VmName;
    public string User;
    public DateTime When;

    public VmShutdown(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "VmShutdown";
        Computer = _eventObject.ComputerName;
        VmName = _eventObject.GetValueFromDataDictionary("VmName");
        if (string.IsNullOrEmpty(VmName)) {
            VmName = _eventObject.GetValueFromDataDictionary("Name");
        }
        if (string.IsNullOrEmpty(VmName)) {
            VmName = _eventObject.GetValueFromDataDictionary("VMName");
        }

        User = _eventObject.GetValueFromDataDictionary("User");
        if (string.IsNullOrEmpty(User)) {
            User = _eventObject.GetValueFromDataDictionary("UserName");
        }
        if (string.IsNullOrEmpty(User)) {
            User = _eventObject.GetValueFromDataDictionary("AccountName");
        }
        if (string.IsNullOrEmpty(User)) {
            User = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        }
        When = _eventObject.TimeCreated;
    }
}
