namespace EventViewerX.Rules.Windows;

/// <summary>
/// Windows Firewall rule modified
/// 4947: A change has been made to Windows Firewall exception list. A rule was modified.
/// </summary>
public class FirewallRuleChange : EventRuleBase {
    public override List<int> EventIds => new() { 4947 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.FirewallRuleChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string RuleName;
    public string ProfileChanged;
    public DateTime When;

    public FirewallRuleChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "FirewallRuleChange";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        RuleName = _eventObject.GetValueFromDataDictionary("RuleName");
        ProfileChanged = _eventObject.GetValueFromDataDictionary("ProfileChanged");
        When = _eventObject.TimeCreated;
    }
}

