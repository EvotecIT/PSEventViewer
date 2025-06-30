using EventViewerX;
using EventViewerX.Helpers.ActiveDirectory;
namespace EventViewerX.Rules.ActiveDirectory;

[NamedEvent(NamedEvents.ADGroupPolicyLinks, "Security", 5136, 5137, 5141)]
public class ADGroupPolicyLinks : EventObjectSlim {
    public string Computer;
    public string Action;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string DomainName;
    public string LinkedToType;
    public string LinkedTo;
    public List<string> GroupPolicyNames { get; set; } = new();
    public List<GroupPolicyLinks> GroupPolicyLink { get; set; } = new();
    public List<GroupPolicyLinks> GroupPolicyUnlink { get; set; } = new();

    public ADGroupPolicyLinks(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyLinks";
        Computer = _eventObject.ComputerName;
        LinkedToType = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        DomainName = _eventObject.GetValueFromDataDictionary("DSName");
        LinkedTo = _eventObject.GetValueFromDataDictionary("ObjectDN");
        var attributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
        var gpoLinks = ExtractGpoLinks(attributeValue);
        if (OperationType.Contains("Value Added")) {
            Action = "Group Policies were linked";
            GroupPolicyLink = gpoLinks;
            GroupPolicyNames = gpoLinks.Select(x => x.DisplayName).ToList();
        } else if (OperationType.Contains("Value Deleted")) {
            Action = "Group Policies were unlinked";
            GroupPolicyUnlink = gpoLinks;
            GroupPolicyNames = gpoLinks.Select(x => x.DisplayName).ToList();
        }
    }

    private static List<GroupPolicyLinks> ExtractGpoLinks(string ldapString) {
        var links = new List<GroupPolicyLinks>();
        if (!string.IsNullOrEmpty(ldapString)) {
            var pattern = @"\[LDAP://(?<dn>[^;]+);(?<flag>\d+)\]";
            var matches = System.Text.RegularExpressions.Regex.Matches(ldapString, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches) {
                if (match.Success) {
                    var gpoLink = new GroupPolicyLinks {
                        DistinguishedName = match.Groups["dn"].Value,
                        IsEnabled = match.Groups["flag"].Value == "0"
                    };
                    var guidPattern = @"cn=\{(?<guid>[0-9A-Fa-f-]+)\}";
                    var guidMatch = System.Text.RegularExpressions.Regex.Match(gpoLink.DistinguishedName, guidPattern);
                    if (guidMatch.Success) {
                        gpoLink.Guid = guidMatch.Groups["guid"].Value;
                        var foundGpo = GroupPolicyHelpers.QueryGroupPolicyByDistinguishedName(gpoLink.DistinguishedName);
                        if (foundGpo != null) {
                            gpoLink.DisplayName = foundGpo.GpoName;
                        }
                    }
                    links.Add(gpoLink);
                }
            }
        }
        return links;
    }

    public static EventObjectSlim? TryCreate(EventObject e)
    {
        if ((e.Data.TryGetValue("ObjectClass", out var cls) && (cls == "domainDNS" || cls == "organizationalUnit" || cls == "site")) &&
            e.ValueMatches("AttributeLDAPDisplayName", "gpLink"))
        {
            return new ADGroupPolicyLinks(e);
        }
        return null;
    }
}
