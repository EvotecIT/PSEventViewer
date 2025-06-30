using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Create Delete
/// 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763
/// </summary>
[NamedEvent(NamedEvents.ADGroupCreateDelete, "Security", 4727, 4730, 4731, 4734, 4744, 4748, 4749, 4753, 4754, 4758, 4759, 4763)]
public class ADGroupCreateDelete : EventObjectSlim {
    public string Computer;
    public string Action;
    public string GroupName;
    public string Who;
    public DateTime When;

    public ADGroupCreateDelete(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupCreateDelete";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
