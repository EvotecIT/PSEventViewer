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
    /// <inheritdoc />
    public override List<int> EventIds => new() { 5137 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.GpoCreated;

    /// <summary>Processes only groupPolicyContainer object creations.</summary>
    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group policy container object
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               objectClass == "groupPolicyContainer";
    }

    /// <summary>Initialises a GPO creation wrapper from an event record.</summary>
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


