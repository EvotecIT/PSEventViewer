using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

[NamedEvent(NamedEvents.GpoDeleted, "Security", 5141)]
public class GpoDeleted : EventObjectSlim {
    public string Computer;
    public string Action;
    public string GpoName;
    public string Who;
    public DateTime When;

    public GpoDeleted(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "GpoDeleted";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        return e.Data.TryGetValue("ObjectClass", out var cls) && cls == "groupPolicyContainer" ? new GpoDeleted(e) : null;
    }
}
