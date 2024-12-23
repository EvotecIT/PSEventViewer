﻿namespace EventViewerX.Rules.ActiveDirectory;

public class ADGroupPolicyLinks : EventObjectSlim {
    public string Computer;
    public string Action;
    //public string ObjectClass;
    public string OperationType;
    public string Who;
    public DateTime When;
    public string DomainName;
    public string OrganizationalUnit;
    public string AttributeLDAPDisplayName;
    public string AttributeValue;

    public ADGroupPolicyLinks(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADGroupPolicyLinks";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        //ObjectClass = _eventObject.GetValueFromDataDictionary("ObjectClass");
        OperationType = ConvertFromOperationType(_eventObject.Data["OperationType"]);
        Who = _eventObject.GetValueFromDataDictionary("SubjectUserName", "SubjectDomainName", "\\", reverseOrder: true);
        When = _eventObject.TimeCreated;
        DomainName = _eventObject.GetValueFromDataDictionary("DSName");
        OrganizationalUnit = _eventObject.GetValueFromDataDictionary("ObjectDN");
        AttributeLDAPDisplayName = _eventObject.GetValueFromDataDictionary("AttributeLDAPDisplayName");
        AttributeValue = _eventObject.GetValueFromDataDictionary("AttributeValue");

// AttributeValue           : [LDAP://cn={E6422062-F0B5-4760-ABCC-4075DA2D4094},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0][LDAP://cn={1D011660-6649-4151-B87E-E24487032776},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0]

    }
}


// - <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
// - <System>
//   <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
//   <EventID>5136</EventID>
//   <Version>0</Version>
//   <Level>0</Level>
//   <Task>14081</Task>
//   <Opcode>0</Opcode>
//   <Keywords>0x8020000000000000</Keywords>
//   <TimeCreated SystemTime="2024-12-23T18:21:28.8564009Z" />
//   <EventRecordID>164206417</EventRecordID>
//   <Correlation ActivityID="{3297f744-34cb-48ae-9913-24eced7ef9c5}" />
//   <Execution ProcessID="712" ThreadID="5136" />
//   <Channel>Security</Channel>
//   <Computer>AD1.ad.evotec.xyz</Computer>
//   <Security />
//   </System>
// - <EventData>
//   <Data Name="OpCorrelationID">{8dd5dab8-8766-4449-80a2-2a34f73376ae}</Data>
//   <Data Name="AppCorrelationID">-</Data>
//   <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
//   <Data Name="SubjectUserName">przemyslaw.klys</Data>
//   <Data Name="SubjectDomainName">EVOTEC</Data>
//   <Data Name="SubjectLogonId">0x13936d16</Data>
//   <Data Name="DSName">ad.evotec.xyz</Data>
//   <Data Name="DSType">%%14676</Data>
//   <Data Name="ObjectDN">OU=Tier2_Option4,DC=ad,DC=evotec,DC=xyz</Data>
//   <Data Name="ObjectGUID">{2433070c-472b-49f0-993b-fc4012ca9074}</Data>
//   <Data Name="ObjectClass">organizationalUnit</Data>
//   <Data Name="AttributeLDAPDisplayName">gPLink</Data>
//   <Data Name="AttributeSyntaxOID">2.5.5.12</Data>
//   <Data Name="AttributeValue">[LDAP://cn={E6422062-F0B5-4760-ABCC-4075DA2D4094},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0][LDAP://cn={1D011660-6649-4151-B87E-E24487032776},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0]</Data>
//   <Data Name="OperationType">%%14674</Data>
//   </EventData>
//   </Event>
// - <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
// - <System>
//   <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
//   <EventID>5136</EventID>
//   <Version>0</Version>
//   <Level>0</Level>
//   <Task>14081</Task>
//   <Opcode>0</Opcode>
//   <Keywords>0x8020000000000000</Keywords>
//   <TimeCreated SystemTime="2024-12-23T18:21:28.8562186Z" />
//   <EventRecordID>164206416</EventRecordID>
//   <Correlation ActivityID="{b921cf62-493f-44f9-a9a2-b8b6e2eb1a16}" />
//   <Execution ProcessID="712" ThreadID="8100" />
//   <Channel>Security</Channel>
//   <Computer>AD1.ad.evotec.xyz</Computer>
//   <Security />
//   </System>
// - <EventData>
//   <Data Name="OpCorrelationID">{8dd5dab8-8766-4449-80a2-2a34f73376ae}</Data>
//   <Data Name="AppCorrelationID">-</Data>
//   <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
//   <Data Name="SubjectUserName">przemyslaw.klys</Data>
//   <Data Name="SubjectDomainName">EVOTEC</Data>
//   <Data Name="SubjectLogonId">0x13936d16</Data>
//   <Data Name="DSName">ad.evotec.xyz</Data>
//   <Data Name="DSType">%%14676</Data>
//   <Data Name="ObjectDN">OU=Tier2_Option4,DC=ad,DC=evotec,DC=xyz</Data>
//   <Data Name="ObjectGUID">{2433070c-472b-49f0-993b-fc4012ca9074}</Data>
//   <Data Name="ObjectClass">organizationalUnit</Data>
//   <Data Name="AttributeLDAPDisplayName">gPLink</Data>
//   <Data Name="AttributeSyntaxOID">2.5.5.12</Data>
//   <Data Name="AttributeValue">[LDAP://cn={1D011660-6649-4151-B87E-E24487032776},cn=policies,cn=system,DC=ad,DC=evotec,DC=xyz;0]</Data>
//   <Data Name="OperationType">%%14675</Data>
//   </EventData>
//   </Event>