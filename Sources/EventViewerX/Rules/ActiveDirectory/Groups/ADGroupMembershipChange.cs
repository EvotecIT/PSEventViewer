namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Membership Changes
/// 4728: A member was added to a security-enabled global group
/// 4729: A member was removed from a security-enabled global group
/// 4732: A member was added to a security-enabled local group
/// 4733: A member was removed from a security-enabled local group
/// 4746: A member was added to a security-enabled universal group
/// 4747: A member was removed from a security-enabled universal group
/// 4751: A member was added to a distribution group
/// 4752: A member was removed from a distribution group
/// 4756: A member was added to a security-enabled universal group
/// 4757: A member was removed from a security-enabled universal group
/// 4761: A member was added to a security-enabled global group
/// 4762: A member was removed from a security-enabled global group
/// 4785: A member was added to a security-enabled universal group
/// 4786: A member was removed from a security-enabled universal group
/// 4787: A member was added to a security-enabled universal group
/// 4788: A member was removed from a security-enabled universal group
/// </summary>
public class ADGroupMembershipChange : EventRuleBase {
    public override List<int> EventIds => new() { 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADGroupMembershipChange;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    public string Computer;
    public string Action;
    public string GroupName;
    public string MemberName;
    public string Who;
    public DateTime When;

    public ADGroupMembershipChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupMembershipChange";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        MemberName = _eventObject.GetValueFromDataDictionary("MemberNameWithoutCN");

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
