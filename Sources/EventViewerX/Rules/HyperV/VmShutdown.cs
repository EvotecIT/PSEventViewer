namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V VM shutdown event
/// 18560: The virtual machine was turned off
/// </summary>
public class VmShutdown : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 18560 };
    /// <inheritdoc />
    public override string LogName => "Microsoft-Windows-Hyper-V-Worker/Operational";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.HyperVVirtualMachineShutdown;

    /// <summary>Accepts Hyper-V VM shutdown events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Hyper-V host where the VM was shut down.</summary>
    public string Computer;
    /// <summary>Name of the virtual machine.</summary>
    public string VmName;
    /// <summary>User or account that initiated the shutdown.</summary>
    public string User;
    /// <summary>Timestamp of the shutdown event.</summary>
    public DateTime When;

    /// <summary>Initialises a Hyper-V VM shutdown wrapper from an event record.</summary>
    public VmShutdown(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "HyperVVirtualMachineShutdown";
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
