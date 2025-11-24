namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Handles user account lockout events (4740).
/// </summary>
public class ADUserLockouts : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4740 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADUserLockouts;

    /// <summary>Accepts account lockout events (4740).</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }
    /// <summary>Machine where the lockout occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Domain controller recording the lockout.</summary>
    public string ComputerLockoutOn;
    /// <summary>Locked out account.</summary>
    public string UserAffected;
    /// <summary>User who performed the action.</summary>
    public string Who;
    /// <summary>Time of the lockout.</summary>
    public DateTime When;

    /// <summary>Initialises an account lockout wrapper from an event record.</summary>
    public ADUserLockouts(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLockouts";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        ComputerLockoutOn = _eventObject.GetValueFromDataDictionary("TargetDomainName");

        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
