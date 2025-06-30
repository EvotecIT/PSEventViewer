using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows OS Crash
/// </summary>
[NamedEvent(NamedEvents.OSCrash, "System", 6008)]
public class OSCrash : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public OSCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
