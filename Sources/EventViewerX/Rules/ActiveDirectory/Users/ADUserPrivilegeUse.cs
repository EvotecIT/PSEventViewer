namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Special privileges assigned to new logon
/// 4672: Special privileges assigned to new logon
/// </summary>
public class ADUserPrivilegeUse : EventRuleBase {
    public override List<int> EventIds => new() { 4672 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserPrivilegeUse;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string Who;
    public DateTime When;
    public List<string> Privileges;
    public List<string> PrivilegesTranslated;

    public ADUserPrivilegeUse(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserPrivilegeUse";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;

        var privilegeList = _eventObject.GetValueFromDataDictionary("PrivilegeList");
        if (!string.IsNullOrEmpty(privilegeList)) {
            Privileges = privilegeList.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            PrivilegesTranslated = Privileges.Select(EventsHelper.TranslatePrivilege).ToList();
        } else {
            Privileges = new List<string>();
            PrivilegesTranslated = new List<string>();
        }
    }
}

