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
        return RuleHelpers.IsProvider(eventObject, "Microsoft-Windows-Security-Auditing", "Microsoft-Windows-Windows Firewall With Advanced Security");
    }
    /// <summary>Computer where the rule was modified.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Name of the rule.</summary>
    public string RuleName;
    /// <summary>Firewall profile that changed.</summary>
    public string ProfileChanged;
    /// <summary>Time the event occurred.</summary>
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

