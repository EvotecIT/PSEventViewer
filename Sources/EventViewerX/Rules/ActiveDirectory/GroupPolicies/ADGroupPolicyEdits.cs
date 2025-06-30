using EventViewerX;
using EventViewerX.Helpers.ActiveDirectory;
namespace EventViewerX.Rules.ActiveDirectory;

[NamedEvent(NamedEvents.ADGroupPolicyEdits, "Security", 5136, 5137, 5141)]
public class ADGroupPolicyEdits : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string GroupPolicyDisplayName;
    public string AttributeLDAPDisplayName;
    public GroupPolicy GroupPolicy { get; set; }

    public ADGroupPolicyEdits(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyEdits";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        GroupPolicyDisplayName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        var dn = _eventObject.GetValueFromDataDictionary("ObjectDN");
        var guidPattern = @"\{(?<guid>[0-9A-Fa-f-]+)\}";
        var match = System.Text.RegularExpressions.Regex.Match(dn, guidPattern);
        if (match.Success) {
            var foundGpo = GroupPolicyHelpers.QueryGroupPolicyByDistinguishedName(dn);
            if (foundGpo != null) {
                GroupPolicy = new GroupPolicy {
                    GpoId = match.Groups["guid"].Value,
                    GpoName = foundGpo.GpoName,
                };
                GroupPolicyDisplayName = foundGpo.GpoName;
            }
        }
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        if (e.Data.TryGetValue("ObjectClass", out var cls) && cls == "groupPolicyContainer"
            && e.Data.TryGetValue("AttributeLDAPDisplayName", out var name)
            && name is string s && s == "versionNumber")
        {
            return new ADGroupPolicyEdits(e);
        }
        return null;
    }
}
