namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory Group Change
/// 4735: A security-enabled local group was created
/// 4737: A security-enabled global group was created
/// 4745: A security-enabled universal group was created
/// 4750: A security-enabled universal group was changed
/// 4760: A security-enabled global group was changed
/// 4764: A security-enabled local group was changed
/// 4784: A security-enabled universal group was deleted
/// 4791: A security-enabled global group was deleted
/// </summary>
public class ADGroupChange : EventRuleBase {

    public string Computer;
    public string Action;
    public string GroupName;
    public string Who;
    public DateTime When;
    public string GroupTypeChange;
    public string SamAccountName;
    public string SidHistory;
    public override List<int> EventIds => new() { 4735, 4737, 4745, 4750, 4760, 4764, 4784, 4791 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADGroupChange;

    public override bool CanHandle(EventObject eventObject) {
        // Ignore *ANONYMOUS* events as they are not useful and clutter the view
        var who = eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        return who != "*ANONYMOUS*";
    }
    public ADGroupChange(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupChange";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        GroupName = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        GroupTypeChange = _eventObject.GetValueFromDataDictionary("GroupTypeChange");
        SamAccountName = _eventObject.GetValueFromDataDictionary("SamAccountName");
        SidHistory = _eventObject.GetValueFromDataDictionary("SidHistory");

        // common fields
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}
