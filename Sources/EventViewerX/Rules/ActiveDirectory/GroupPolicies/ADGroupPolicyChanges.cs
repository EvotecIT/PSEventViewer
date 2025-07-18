﻿namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Summary information for group policy change events.
/// </summary>
public class ADGroupPolicyChanges : EventRuleBase {
    /// <summary>Computer where the change occurred.</summary>
    public string Computer;
    /// <summary>Description of the action.</summary>
    public string Action;
    /// <summary>Class of the object modified.</summary>
    public string ObjectClass;
    /// <summary>Operation type value.</summary>
    public string OperationType;
    /// <summary>User performing the action.</summary>
    public string Who;
    /// <summary>Timestamp of the event.</summary>
    public DateTime When;
    /// <summary>Distinguished name of the GPO.</summary>
    public string GpoName;
    /// <summary>LDAP display name of the attribute.</summary>
    public string AttributeLDAPDisplayName;
    /// <summary>Value of the attribute.</summary>
    public string AttributeValue;
    public override List<int> EventIds => new() { 5136, 5137, 5141 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.ADGroupPolicyChanges;

    public override bool CanHandle(EventObject eventObject) {
        // Check if this is a group policy container or container object
        return eventObject.Data.TryGetValue("ObjectClass", out var objectClass) &&
               (objectClass == "groupPolicyContainer" || objectClass == "container");
    }

    public ADGroupPolicyChanges(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyChanges";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        // OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        GpoName = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");
    }
}

//- <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
// - <System>
//   <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
//   <EventID>5137</EventID>
//   <Version>0</Version>
//   <Level>0</Level>
//   <Task>14081</Task>
//   <Opcode>0</Opcode>
//   <Keywords>0x8020000000000000</Keywords>
//   <TimeCreated SystemTime="2024-12-26T18:30:31.5594768Z" />
//   <EventRecordID>164758673</EventRecordID>
//   <Correlation ActivityID="{3d47ddee-909d-4aef-a324-cca46a47725d}" />
//   <Execution ProcessID="712" ThreadID="4720" />
//   <Channel>Security</Channel>
//   <Computer>AD1.ad.evotec.xyz</Computer>
//   <Security />
//   </System>
// - <EventData>
//   <Data Name="OpCorrelationID">{8e498f88-21d5-4eb1-902a-01c8672e6e0c}</Data>
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
//   </EventData>
//   </Event>
