using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// OS Time Change
/// </summary>
[NamedEvent(NamedEvents.OSTimeChange, "Security", 4616)]
public class OSTimeChange : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public OSTimeChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSTimeChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
