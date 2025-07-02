namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V virtual machine started
/// 18500: The virtual machine was started
/// </summary>
public class HyperVVirtualMachineStarted : EventRuleBase {
    public override List<int> EventIds => new() { 18500 };
    public override string LogName => "Microsoft-Windows-Hyper-V-VMMS/Admin";
    public override NamedEvents NamedEvent => NamedEvents.HyperVVirtualMachineStarted;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string VirtualMachine;
    public string VirtualMachineId;
    public DateTime When;

    public HyperVVirtualMachineStarted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "HyperVVirtualMachineStarted";
        Computer = _eventObject.ComputerName;
        VirtualMachine = _eventObject.GetValueFromDataDictionary("Name", "VMName");
        VirtualMachineId = _eventObject.GetValueFromDataDictionary("VMId", "VirtualMachineId");
        When = _eventObject.TimeCreated;
    }
}
