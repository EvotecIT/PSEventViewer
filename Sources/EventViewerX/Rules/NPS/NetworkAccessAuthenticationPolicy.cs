using EventViewerX;
namespace EventViewerX.Rules.NPS;

/// <summary>
/// Network Access Authentication Policy
/// </summary>
[NamedEvent(NamedEvents.NetworkAccessAuthenticationPolicy, "Security", 6272, 6273)]
public class NetworkAccessAuthenticationPolicy : EventObjectSlim {
    public string Computer;
    public string Action;
    public string NpsMessage;
    public string Who;
    public DateTime When;

    public NetworkAccessAuthenticationPolicy(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "NetworkAccessAuthenticationPolicy";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        NpsMessage = _eventObject.GetValueFromDataDictionary("FullMessage");
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
