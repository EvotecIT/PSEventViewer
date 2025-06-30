using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// OS Startup, Shutdown, Crash
/// </summary>
[NamedEvent(NamedEvents.OSStartupShutdownCrash, "System", 12, 13, 41, 4608, 4621, 6008)]
public class OSStartupShutdownCrash : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public OSStartupShutdownCrash(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "OSStartupShutdownCrash";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
