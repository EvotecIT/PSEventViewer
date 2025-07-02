namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Unlocked
/// 4767: A user account was unlocked
/// </summary>
public class ADUserUnlocked : EventRuleBase {
    public override List<int> EventIds => new() { 4767 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserUnlocked;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string ComputerLockoutOn;
    public string UserAffected;
    public string Who;
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
