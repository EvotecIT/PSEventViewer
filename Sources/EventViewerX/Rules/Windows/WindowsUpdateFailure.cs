using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows Update installation failure
/// </summary>
[NamedEvent(NamedEvents.WindowsUpdateFailure, "Setup", 20)]
public class WindowsUpdateFailure : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public WindowsUpdateFailure(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "WindowsUpdateFailure";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
