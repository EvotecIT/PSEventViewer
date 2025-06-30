namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Enumeration
/// 4798: A user's local group membership was enumerated
/// 4799: A security-enabled local group membership was enumerated
/// </summary>
public class ADGroupEnumeration : EventObjectSlim {

    public string Computer;
    public string Action;
    public string GroupName;
    public string Who;
    public DateTime When;

    public ADGroupEnumeration(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupEnumeration";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
