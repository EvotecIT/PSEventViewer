using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// BitLocker protection key changed or backed up
/// </summary>
[NamedEvent(NamedEvents.BitLockerKeyChange, "Security", 4673, 4692)]
public class BitLockerKeyChange : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public BitLockerKeyChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "BitLockerKeyChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
