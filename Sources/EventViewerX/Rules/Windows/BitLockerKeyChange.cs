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
    public string Computer;
    public string Action;
    public BitLockerVolumeType? Volume;
    public BitLockerProtectorType? ProtectorType;
    public string MasterKeyId;
    public string RecoveryKeyId;
    public string RecoveryServer;
    public string Who;
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

