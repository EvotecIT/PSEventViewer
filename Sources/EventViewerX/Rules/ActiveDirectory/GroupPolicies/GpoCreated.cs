namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents a newly created group policy object event.
/// </summary>
public class GpoCreated : EventRuleBase {
    public string Computer;
    public string Action;
    public string GpoName;
    public string Who;
    public DateTime When;
    public override List<int> EventIds => new() { 5137 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.GpoCreated;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group policy container object
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    public GpoCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoCreated";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}


