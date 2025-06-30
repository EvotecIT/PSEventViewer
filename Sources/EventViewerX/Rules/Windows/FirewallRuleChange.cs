using EventViewerX;
namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows Firewall rule modified
/// </summary>
[NamedEvent(NamedEvents.FirewallRuleChange, "Security", 4947)]
public class FirewallRuleChange : EventObjectSlim {
    public string Computer;
    public string Action;
    public DateTime When;

    public FirewallRuleChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "FirewallRuleChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        When = _eventObject.TimeCreated;
    }
}
