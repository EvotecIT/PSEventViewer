namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents a newly created group policy object event.
/// </summary>
public class GpoCreated : EventRuleBase {
    /// <summary>Computer on which the GPO was created.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Distinguished name of the new GPO.</summary>
    public string GpoName;
    /// <summary>User that created the GPO.</summary>
    public string Who;
    /// <summary>Time the GPO was created.</summary>
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


