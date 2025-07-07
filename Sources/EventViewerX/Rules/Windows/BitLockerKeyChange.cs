namespace EventViewerX.Rules.Windows;

/// <summary>
/// BitLocker protection key changed or backed up
/// 4673: A privileged service was called
/// 4692: Backup of data protection master key was attempted
/// </summary>
public class BitLockerKeyChange : EventRuleBase {
    public override List<int> EventIds => new() { 4673, 4692 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.BitLockerKeyChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Computer where the key operation happened.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Volume type.</summary>
    public BitLockerVolumeType? Volume;
    /// <summary>Protector type used.</summary>
    public BitLockerProtectorType? ProtectorType;
    /// <summary>Master key identifier.</summary>
    public string MasterKeyId;
    /// <summary>Recovery key identifier.</summary>
    public string RecoveryKeyId;
    /// <summary>Server where recovery key was stored.</summary>
    public string RecoveryServer;
    /// <summary>User responsible for the change.</summary>
    public string Who;
    /// <summary>Time of the event.</summary>
    public DateTime When;

    public BitLockerKeyChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "BitLockerKeyChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        Volume = EventsHelper.GetBitLockerVolumeType(
            _eventObject.GetValueFromDataDictionary("VolumeName", "Volume"));
        ProtectorType = EventsHelper.GetBitLockerProtectorType(
            _eventObject.GetValueFromDataDictionary("ProtectorType", "KeyProtection"));
        MasterKeyId = _eventObject.GetValueFromDataDictionary("MasterKeyId");
        RecoveryKeyId = _eventObject.GetValueFromDataDictionary("RecoveryKeyId");
        RecoveryServer = _eventObject.GetValueFromDataDictionary("RecoveryServer");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

