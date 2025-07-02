namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V checkpoint created
/// 4096: Hyper-V checkpoint created
/// </summary>
public class VmCheckpointCreated : EventRuleBase {
    public override List<int> EventIds => new() { 4096 };
    public override string LogName => "Microsoft-Windows-Hyper-V-VMMS/Admin";
    public override NamedEvents NamedEvent => NamedEvents.HyperVCheckpointCreated;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string VmName;
    public string CheckpointName;
    public string User;
    public DateTime When;

    public VmCheckpointCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "VmCheckpointCreated";
        Computer = _eventObject.ComputerName;
        VmName = _eventObject.GetValueFromDataDictionary("VmName");
        if (string.IsNullOrEmpty(VmName)) {
            VmName = _eventObject.GetValueFromDataDictionary("Name");
        }
        if (string.IsNullOrEmpty(VmName)) {
            VmName = _eventObject.GetValueFromDataDictionary("VMName");
        }

        CheckpointName = _eventObject.GetValueFromDataDictionary("CheckpointName");
        if (string.IsNullOrEmpty(CheckpointName)) {
            CheckpointName = _eventObject.GetValueFromDataDictionary("SnapshotName");
        }
        if (string.IsNullOrEmpty(CheckpointName)) {
            CheckpointName = _eventObject.GetValueFromDataDictionary("Name", "FriendlyName");
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
