namespace EventViewerX.Rules.HyperV;

/// <summary>
/// Hyper-V checkpoint created
/// 4096: Hyper-V checkpoint created
/// </summary>
public class VmCheckpointCreated : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4096 };
    /// <inheritdoc />
    public override string LogName => "Microsoft-Windows-Hyper-V-VMMS/Admin";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.HyperVCheckpointCreated;

    /// <summary>Accepts Hyper-V checkpoint creation events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }

    /// <summary>Hyper-V host where the checkpoint was created.</summary>
    public string Computer;
    /// <summary>Name of the virtual machine.</summary>
    public string VmName;
    /// <summary>Name of the created checkpoint.</summary>
    public string CheckpointName;
    /// <summary>User who created the checkpoint.</summary>
    public string User;
    /// <summary>Timestamp of checkpoint creation.</summary>
    public DateTime When;

    /// <summary>Initialises a Hyper-V checkpoint wrapper from an event record.</summary>
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
