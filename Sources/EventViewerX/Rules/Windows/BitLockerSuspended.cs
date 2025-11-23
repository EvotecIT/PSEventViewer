namespace EventViewerX.Rules.Windows;

/// <summary>
/// BitLocker protection suspended
/// 24660: BitLocker protection was suspended
/// </summary>
public class BitLockerSuspended : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 24660 };
    /// <inheritdoc />
    public override string LogName => "BitLocker-API";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.BitLockerSuspended;

    /// <summary>Accepts BitLocker suspend events from the BitLocker-API log.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }
    /// <summary>Machine where protection was suspended.</summary>
    public string Computer;
    /// <summary>Volume affected.</summary>
    public BitLockerVolumeType? Volume;
    /// <summary>Account that suspended protection.</summary>
    public string Who;
    /// <summary>Event timestamp.</summary>
    public DateTime When;

    /// <summary>Initialises a BitLocker suspended wrapper from an event record.</summary>
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
