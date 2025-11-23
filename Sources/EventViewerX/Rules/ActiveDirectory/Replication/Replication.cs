namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Documentation stub for future Active Directory replication event handling (e.g., lingering objects, GMSA password fetch failures).
/// </summary>
/// <remarks>
/// The class intentionally has no logic yet; the surrounding comments capture representative replication and NTDS LDAP events
/// that will be formalised into rules in a later iteration.
/// </remarks>
internal class Replication {

    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 8:51:44 AM
    // Event ID:      1988
    // Task Category: Replication
    // Level:         Error
    // Keywords:      Classic
    // User:          ANONYMOUS LOGON
    // Computer:      XE-S-evo0002.evoope.evotec.com
    // Description:
    // Active Directory Domain Services Replication encountered the existence of objects in the following partition that have been deleted from the local domain controllers (DCs) Active Directory Domain Services database.  Not all direct or transitive replication partners replicated in the deletion before the tombstone lifetime number of days passed.  Objects that have been deleted and garbage collected from an Active Directory Domain Services partition but still exist in the writable partitions of other DCs in the same domain, or read-only partitions of global catalog servers in other domains in the forest are known as "lingering objects". 
    //  
    //  
    // Source domain controller: 
    // 898d6134-9a50-4c6f-b234-09653eb71de3._msdcs.evotec.com 
    // Object: 
    // CN=Class Store\0ACNF:4f56aca0-9acc-4f19-a492-d54011cc3df9,CN=Machine,CN={6AC1786C-016F-11D2-945F-00C04fB984F9},CN=Policies,CN=System,DC=evotec,DC=com 
    // Object GUID: 
    // 4f56aca0-9acc-4f19-a492-d54011cc3df9  This event is being logged because the source DC contains a lingering object which does not exist on the local DCs Active Directory Domain Services database.  This replication attempt has been blocked.
    //  
    //  The best solution to this problem is to identify and remove all lingering objects in the forest.
    //  
    //  
    // User Action:
    //  
    // Remove Lingering Objects:
    //  
    //  The action plan to recover from this error can be found at http://support.microsoft.com/?id=314282.
    //  
    //  If both the source and destination DCs are Windows Server 2003 DCs, then install the support tools included on the installation CD.  To see which objects would be deleted without actually performing the deletion run "repadmin /removelingeringobjects <Source DC> <Destination DC DSA GUID> <NC> /ADVISORY_MODE". The event logs on the source DC will enumerate all lingering objects.  To remove lingering objects from a source domain controller run "repadmin /removelingeringobjects <Source DC> <Destination DC DSA GUID> <NC>".
    //  
    //  If either source or destination DC is a Windows 2000 Server DC, then more information on how to remove lingering objects on the source DC can be found at http://support.microsoft.com/?id=314282 or from your Microsoft support personnel.
    //  
    //  If you need Active Directory Domain Services replication to function immediately at all costs and don't have time to remove lingering objects, enable loose replication consistency by unsetting the following registry key:
    //  
    // Registry Key:
    // HKLM\System\CurrentControlSet\Services\NTDS\Parameters\Strict Replication Consistency
    //  
    //  Replication errors between DCs sharing a common partition can prevent user and computer accounts, trust relationships, their passwords, security groups, security group memberships and other Active Directory Domain Services configuration data to vary between DCs, affecting the ability to log on, find objects of interest and perform other critical operations. These inconsistencies are resolved once replication errors are resolved.  DCs that fail to inbound replicate deleted objects within tombstone lifetime number of days will remain inconsistent until lingering objects are manually removed by an administrator from each local DC.
    //  
    //  Lingering objects may be prevented by ensuring that all domain controllers in the forest are running Active Directory Domain Services, are connected by a spanning tree connection topology and perform inbound replication before Tombstone Live number of days pass.
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="49152">1988</EventID>
    //     <Version>0</Version>
    //     <Level>2</Level>
    //     <Task>5</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T07:51:44.395358000Z" />
    //     <EventRecordID>104133172</EventRecordID>
    //     <Correlation ActivityID="{d489acd8-a6a9-47f4-999d-f7632ee01924}" />
    //     <Execution ProcessID="776" ThreadID="5416" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>XE-S-evo0002.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-7" />
    //   </System>
    //   <EventData>
    //     <Data>898d6134-9a50-4c6f-b234-09653eb71de3._msdcs.evotec.com</Data>
    //     <Data>CN=Class Store\0ACNF:4f56aca0-9acc-4f19-a492-d54011cc3df9,CN=Machine,CN={6AC1786C-016F-11D2-945F-00C04fB984F9},CN=Policies,CN=System,DC=evotec,DC=com</Data>
    //     <Data>4f56aca0-9acc-4f19-a492-d54011cc3df9</Data>
    //     <Data>Strict Replication Consistency</Data>
    //     <Data>System\CurrentControlSet\Services\NTDS\Parameters</Data>
    //   </EventData>
    // </Event>


    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 8:08:29 PM
    // Event ID:      2947
    // Task Category: Security
    // Level:         Warning
    // Keywords:      Classic
    // User:          evotec\XP-S-evotec0032$
    // Computer:      xp-s-evo0555.evoope.evotec.com
    // Description:
    // An attempt to fetch the password of a group managed service account failed. 
    //  
    // Group Managed Service Account Object: 
    // CN=gmsa-evoo-MDI,CN=Managed Service Accounts,DC=evoope,DC=evotec,DC=com 
    // Caller SID: 
    // S-1-5-21-2905325446-4054960910-2172852593-239380 
    // Caller IP: 
    // 10.176.106.12:56568 
    // Error: 
    // 8995
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="32768">2947</EventID>
    //     <Version>0</Version>
    //     <Level>3</Level>
    //     <Task>2</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T12:08:29.313535700Z" />
    //     <EventRecordID>109720265</EventRecordID>
    //     <Correlation />
    //     <Execution ProcessID="724" ThreadID="7540" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>xp-s-evo0555.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-21-2905325446-4054960910-2172852593-239380" />
    //   </System>
    //   <EventData>
    //     <Data>CN=gmsa-evoo-MDI,CN=Managed Service Accounts,DC=evoope,DC=evotec,DC=com</Data>
    //     <Data>S-1-5-21-2905325446-4054960910-2172852593-239380</Data>
    //     <Data>10.176.106.12:56568</Data>
    //     <Data>8995</Data>
    //   </EventData>
    // </Event>


    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 8:11:36 PM
    // Event ID:      1938
    // Task Category: Replication
    // Level:         Information
    // Keywords:      Classic
    // User:          evoOPE\t0-abc-pk
    // Computer:      xp-s-evo0555.evoope.evotec.com
    // Description:
    // Active Directory Domain Services has begun the verification of lingering objects in advisory mode on the local domain controller. All objects on this domain controller will have their existence verified on the following source domain controller. 
    //  
    // Source domain controller: 
    // 49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com 
    //  
    // Objects that have been deleted and garbage collected on the source domain controller yet still exist on this domain controller will be listed in subsequent event log entries. To permanently delete the lingering objects, restart this procedure without using the advisory mode option.
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="16384">1938</EventID>
    //     <Version>0</Version>
    //     <Level>4</Level>
    //     <Task>5</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T12:11:36.215620800Z" />
    //     <EventRecordID>109720853</EventRecordID>
    //     <Correlation />
    //     <Execution ProcessID="724" ThreadID="5652" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>xp-s-evo0555.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-21-1832937852-2116575123-337272265-1368577" />
    //   </System>
    //   <EventData>
    //     <Data>49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com</Data>
    //   </EventData>
    // </Event>

    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 8:11:36 PM
    // Event ID:      1942
    // Task Category: Replication
    // Level:         Information
    // Keywords:      Classic
    // User:          evoOPE\t0-abc-pk
    // Computer:      xp-s-evo0555.evoope.evotec.com
    // Description:
    // Active Directory Domain Services has completed the verification of lingering objects on the local domain controller in advisory mode. All objects on this domain controller have had their existence verified on the following source domain controller. 
    //  
    // Source domain controller: 
    // 49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com 
    // Number of lingering objects examined and verified: 
    // 0 
    //  
    // Objects that have been deleted and garbage collected on the source domain controller yet still exist on this domain controller have been listed in past event log entries. To permanently delete the lingering objects, restart this procedure without using the advisory mode option.
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="16384">1942</EventID>
    //     <Version>0</Version>
    //     <Level>4</Level>
    //     <Task>5</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T12:11:36.200002100Z" />
    //     <EventRecordID>109720852</EventRecordID>
    //     <Correlation />
    //     <Execution ProcessID="724" ThreadID="5652" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>xp-s-evo0555.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-21-1832937852-2116575123-337272265-1368577" />
    //   </System>
    //   <EventData>
    //     <Data>49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com</Data>
    //     <Data>0</Data>
    //   </EventData>
    // </Event>

    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 8:11:23 PM
    // Event ID:      1944
    // Task Category: Replication
    // Level:         Error
    // Keywords:      Classic
    // User:          evoOPE\t0-abc-pk
    // Computer:      xp-s-evo0555.evoope.evotec.com
    // Description:
    // Active Directory Domain Services was unable to verify the existence of all lingering objects on the local domain controller in advisory mode. However, lingering objects found prior to the process quitting have had their existence verified on the following source domain controller. These objects have been listed in past event log entries. 
    //  
    // Source domain controller: 
    // 49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com 
    // Number of lingering objects identified and verified: 
    // 0 
    //  
    // Additional Data 
    // Error value: 
    // The naming context specified for this replication operation is invalid. 8440
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="49152">1944</EventID>
    //     <Version>0</Version>
    //     <Level>2</Level>
    //     <Task>5</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T12:11:23.762548000Z" />
    //     <EventRecordID>109720848</EventRecordID>
    //     <Correlation />
    //     <Execution ProcessID="724" ThreadID="5652" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>xp-s-evo0555.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-21-1832937852-2116575123-337272265-1368577" />
    //   </System>
    //   <EventData>
    //     <Data>49f3307a-4252-4844-afa4-0f9649b5af8f._msdcs.evotec.com</Data>
    //     <Data>The naming context specified for this replication operation is invalid.</Data>
    //     <Data>8440</Data>
    //     <Data>0</Data>
    //   </EventData>
    // </Event>


    //Log Name:      Directory Service
    // Source:        Microsoft-Windows-ActiveDirectory_DomainService
    // Date:          2/12/2024 1:34:13 PM
    // Event ID:      1942
    // Task Category: Replication
    // Level:         Information
    // Keywords:      Classic
    // User:          evoOPE\t0-abc-pk
    // Computer:      XE-S-evo0001.evoope.evotec.com
    // Description:
    // Active Directory Domain Services has completed the verification of lingering objects on the local domain controller in advisory mode. All objects on this domain controller have had their existence verified on the following source domain controller. 
    //  
    // Source domain controller: 
    // 898d6134-9a50-4c6f-b234-09653eb71de3._msdcs.evotec.com 
    // Number of lingering objects examined and verified: 
    // 38 
    //  
    // Objects that have been deleted and garbage collected on the source domain controller yet still exist on this domain controller have been listed in past event log entries. To permanently delete the lingering objects, restart this procedure without using the advisory mode option.
    // Event Xml:
    // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
    //   <System>
    //     <Provider Name="Microsoft-Windows-ActiveDirectory_DomainService" Guid="{0e8478c5-3605-4e8c-8497-1e730c959516}" EventSourceName="NTDS LDAP" />
    //     <EventID Qualifiers="16384">1942</EventID>
    //     <Version>0</Version>
    //     <Level>4</Level>
    //     <Task>5</Task>
    //     <Opcode>0</Opcode>
    //     <Keywords>0x8080000000000000</Keywords>
    //     <TimeCreated SystemTime="2024-02-12T12:34:13.585351600Z" />
    //     <EventRecordID>62910598</EventRecordID>
    //     <Correlation />
    //     <Execution ProcessID="984" ThreadID="11460" />
    //     <Channel>Directory Service</Channel>
    //     <Computer>XE-S-evo0001.evoope.evotec.com</Computer>
    //     <Security UserID="S-1-5-21-1832937852-2116575123-337272265-1368577" />
    //   </System>
    //   <EventData>
    //     <Data>898d6134-9a50-4c6f-b234-09653eb71de3._msdcs.evotec.com</Data>
    //     <Data>38</Data>
    //   </EventData>
    // </Event>
}
