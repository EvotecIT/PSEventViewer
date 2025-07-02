namespace EventViewerX.Rules.Windows;

/// <summary>
/// BitLocker protection suspended
/// 24660: BitLocker protection was suspended
/// </summary>
public class BitLockerSuspended : EventRuleBase {
    public override List<int> EventIds => new() { 24660 };
    public override string LogName => "BitLocker-API";
    public override NamedEvents NamedEvent => NamedEvents.BitLockerSuspended;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public BitLockerVolumeType? Volume;
    public string Who;
    public DateTime When;

    public BitLockerSuspended(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "BitLockerSuspended";
        Computer = _eventObject.ComputerName;
        Volume = EventsHelper.GetBitLockerVolumeType(
            _eventObject.GetValueFromDataDictionary("VolumeName", "Volume"));
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
