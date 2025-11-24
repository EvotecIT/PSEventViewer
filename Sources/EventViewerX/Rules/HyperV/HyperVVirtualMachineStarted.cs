namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V virtual machine started
/// 18500: The virtual machine was started
/// </summary>
public class HyperVVirtualMachineStarted : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 18500 };
    /// <inheritdoc />
    public override string LogName => "Microsoft-Windows-Hyper-V-VMMS/Admin";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.HyperVVirtualMachineStarted;

    /// <summary>Accepts Hyper-V VM start events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Hyper-V host that started the VM.</summary>
    public string Computer;
    /// <summary>Name of the virtual machine.</summary>
    public string VirtualMachine;
    /// <summary>Unique identifier of the virtual machine.</summary>
    public string VirtualMachineId;
    /// <summary>Timestamp when the VM was started.</summary>
    public DateTime When;

    /// <summary>Initialises a Hyper-V VM start wrapper from an event record.</summary>
    public HyperVVirtualMachineStarted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "HyperVVirtualMachineStarted";
        Computer = _eventObject.ComputerName;
        VirtualMachine = _eventObject.GetValueFromDataDictionary("Name", "VMName");
        VirtualMachineId = _eventObject.GetValueFromDataDictionary("VMId", "VirtualMachineId");
        When = _eventObject.TimeCreated;
    }
}
