namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents deletion of a computer account (event 4743).
/// </summary>
public class ADComputerDeleted : EventRuleBase {
    public override List<int> EventIds => new() { 4743 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADComputerDeleted;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>Machine where deletion occurred.</summary>
    public string Computer;
    /// <summary>Short description of the deletion.</summary>
    public string Action;
    /// <summary>Computer account deleted.</summary>
    public string ComputerAffected;
    /// <summary>User performing the deletion.</summary>
    public string Who;
    /// <summary>Time of deletion.</summary>
    public DateTime When;

    public ADComputerDeleted(EventObject eventObject) : base(eventObject) {
        // common fields
        _eventObject = eventObject;
        Type = "ADComputerDeleted";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        ComputerAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}