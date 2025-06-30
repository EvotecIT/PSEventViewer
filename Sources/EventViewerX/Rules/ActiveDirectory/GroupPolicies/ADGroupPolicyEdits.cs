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
    //public string AttributeValue;
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
                    //DistinguishedName = dn,
                    //IsEnabled = true
                };
                GroupPolicyDisplayName = foundGpo.GpoName;
            }
        }
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        //AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
    }
}

//- <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
// - <System>
//   <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
//   <EventID>5136</EventID>
//   <Version>0</Version>
//   <Level>0</Level>
//   <Task>14081</Task>
//   <Opcode>0</Opcode>
//   <Keywords>0x8020000000000000</Keywords>
//   <TimeCreated SystemTime="2024-12-26T18:30:31.5587483Z" />
//   <EventRecordID>164758670</EventRecordID>
//   <Correlation ActivityID="{7818f1df-8631-4326-bfed-1795848cb8ec}" />
//   <Execution ProcessID="712" ThreadID="4720" />
//   <Channel>Security</Channel>
//   <Computer>AD1.ad.evotec.xyz</Computer>
//   <Security />
//   </System>
// - <EventData>
//   <Data Name="OpCorrelationID">{d3c9f198-2c49-4d0f-bc37-465d5fbe9d80}</Data>
//   <Data Name="AppCorrelationID">-</Data>
//   <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
//   <Data Name="SubjectUserName">przemyslaw.klys</Data>
//   <Data Name="SubjectDomainName">EVOTEC</Data>
//   <Data Name="SubjectLogonId">0x2ac85901</Data>
//   <Data Name="DSName">ad.evotec.xyz</Data>
//   <Data Name="DSType">%%14676</Data>
//   <Data Name="ObjectDN">CN={FB6A0E91-F93D-4428-B29D-2FDCC3A95425},CN=Policies,CN=System,DC=ad,DC=evotec,DC=xyz</Data>
//   <Data Name="ObjectGUID">{9b263379-4310-4585-9eb3-ee688590d3f0}</Data>
//   <Data Name="ObjectClass">groupPolicyContainer</Data>
//   <Data Name="AttributeLDAPDisplayName">versionNumber</Data>
//   <Data Name="AttributeSyntaxOID">2.5.5.9</Data>
//   <Data Name="AttributeValue">0</Data>
//   <Data Name="OperationType">%%14674</Data>
//   </EventData>
//   </Event>
