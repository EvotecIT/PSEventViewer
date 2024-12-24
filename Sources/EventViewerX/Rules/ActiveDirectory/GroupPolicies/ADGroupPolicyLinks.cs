﻿using EventViewerX.Helpers.ActiveDirectory;

namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Represents and processes event data related to Group Policy links.
/// </summary>
/// <remarks>
/// Below is an example of the underlying XML structure for these events:
/// <code>
/// <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
///   <System>
///     <Provider Name="Microsoft-Windows-Security-Auditing" />
///     <!-- ...existing XML elements... -->
///     <EventID>5136</EventID>
///     <Version>0</Version>
///     <Level>0</Version>
///     <Task>14081</Task>
///     <Opcode>0</Opcode>
///     <Keywords>0x8020000000000000</Keywords>
///     <TimeCreated SystemTime="2024-12-23T18:21:28.8564009Z" />
///     <EventRecordID>164206417</EventRecordID>
///     <Correlation ActivityID="{3297f744-34cb-48ae-9913-24eced7ef9c5}" />
///     <Execution ProcessID="712" ThreadID="5136" />
///     <Channel>Security</Channel>
///     <Computer>AD1.ad.evotec.xyz</Computer>
///     <!-- ...existing XML elements... -->
///   </System>
///   <EventData>
///     <!-- ...existing XML elements... -->
///     <Data Name="OpCorrelationID">{8dd5dab8-8766-4449-80a2-2a34f73376ae}</Data>
///     <Data Name="AppCorrelationID">-</Data>
///     <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
///     <Data Name="SubjectUserName">przemyslaw.klys</Data>
///     <Data Name="SubjectDomainName">EVOTEC</Data>
///     <Data Name="SubjectLogonId">0x13936d16</Data>
///     <Data Name="DSName">ad.evotec.xyz</Data>
///     <Data Name="DSType">%%14676</Data>
///     <Data Name="ObjectDN">OU=Tier2_Option4,DC=ad,DC=evotec,DC=xyz</Data>
///     <Data Name="ObjectGUID">{2433070c-472b-49f0-993b-fc4012ca9074}</Data>
///     <Data Name="ObjectClass">organizationalUnit</Data>
///     <Data Name="AttributeLDAPDisplayName">gPLink</Data>
///     <Data Name="AttributeSyntaxOID">2.5.5.12</Data>
///     <Data Name="AttributeValue">[LDAP://cn={E6422062-F0B5-4760-ABCC-4075DA2D4094},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0][LDAP://cn={1D011660-6649-4151-B87E-E24487032776},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0]</Data>
///     <Data Name="OperationType">%%14674</Data>
///     <!-- ...existing XML elements... -->
///   </EventData>
/// </Event>
/// </code>
/// </remarks>
public class ADGroupPolicyLinks : EventObjectSlim {
    public string Computer;
    public string Action;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string DomainName;
    public string LinkedToType;
    public string LinkedTo;
    // public string AttributeLDAPDisplayName;
    // public string AttributeValue;
    public List<string> GroupPolicyNames { get; set; } = new();
    public List<GpoLink> GroupPolicyLink { get; set; } = new();
    public List<GpoLink> GroupPolicyUnlink { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the ADGroupPolicyLinks class using a specified event object.
    /// </summary>
    public ADGroupPolicyLinks(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyLinks";
        Computer = _eventObject.ComputerName;
        //Action = _eventObject.MessageSubject;
        LinkedToType = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        DomainName = _eventObject.GetValueFromDataDictionary("DSName");
        LinkedTo = _eventObject.GetValueFromDataDictionary("ObjectDN");
        // AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
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

    /// <summary>
    /// Parses the specified LDAP string to extract associated GPO links.
    /// </summary>
    private static List<GpoLink> ExtractGpoLinks(string ldapString) {
        var links = new List<GpoLink>();
        if (!string.IsNullOrEmpty(ldapString)) {
            // e.g. [LDAP://cn={E6422062-F0B5-4760-ABCC-4075DA2D4094},cn=policies,...;0]
            var pattern = @"\[LDAP://(?<dn>[^;]+);(?<flag>\d+)\]";
            var matches = System.Text.RegularExpressions.Regex.Matches(ldapString, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches) {
                if (match.Success) {
                    var gpoLink = new GpoLink {
                        DistinguishedName = match.Groups["dn"].Value,
                        IsEnabled = match.Groups["flag"].Value == "0"
                    };
                    // parse out the GUID from the DN
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
}