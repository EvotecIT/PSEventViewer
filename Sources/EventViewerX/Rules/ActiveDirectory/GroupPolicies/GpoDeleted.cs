namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents a deleted group policy object event.
/// </summary>
public class GpoDeleted : EventRuleBase {
    public string Computer;
    public string Action;
    public string GpoName;
    public string Who;
    public DateTime When;
    public override List<int> EventIds => new() { 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.GpoDeleted;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group policy container object
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    public GpoDeleted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoDeleted";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


