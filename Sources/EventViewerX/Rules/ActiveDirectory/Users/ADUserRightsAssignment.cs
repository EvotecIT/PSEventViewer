namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// A user right was assigned or removed
/// 4704: A user right was assigned
/// 4705: A user right was removed
/// </summary>
public class ADUserRightsAssignment : EventRuleBase {
    public override List<int> EventIds => new() { 4704, 4705 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADUserRightsAssignment;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string UserAffected;
    public string Who;
    public DateTime When;
    public List<string> Rights;
    public List<string> RightsTranslated;

    public ADUserRightsAssignment(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserRightsAssignment";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;

        UserAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        if (string.IsNullOrEmpty(UserAffected)) {
            var sid = _eventObject.GetValueFromDataDictionary("TargetSid");
            if (!string.IsNullOrEmpty(sid)) {
                try {
                    var identifier = new System.Security.Principal.SecurityIdentifier(sid);
                    UserAffected = identifier.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                } catch {
                    UserAffected = sid;
                }
            }
        }

        var privileges = _eventObject.GetValueFromDataDictionary("PrivilegeList");
        if (!string.IsNullOrEmpty(privileges)) {
            Rights = privileges.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            RightsTranslated = Rights.Select(EventsHelper.TranslatePrivilege).ToList();
        } else {
            Rights = new List<string>();
            RightsTranslated = new List<string>();
        }

        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
    }
}

