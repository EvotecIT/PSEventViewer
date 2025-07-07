namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Handles user account unlock events (4767).
/// </summary>
public class ADUserUnlocked : EventRuleBase {
    public override List<int> EventIds => new() { 4767 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserUnlocked;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Machine where the unlock occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Domain controller processing the unlock.</summary>
    public string ComputerLockoutOn;
    /// <summary>User account that was unlocked.</summary>
    public string UserAffected;
    /// <summary>User performing the unlock.</summary>
    public string Who;
    /// <summary>Time of the unlock.</summary>
    public DateTime When;

    public ADUserUnlocked(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserUnlocked";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        ComputerLockoutOn = _eventObject.GetValueFromDataDictionary("TargetDomainName");

        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
