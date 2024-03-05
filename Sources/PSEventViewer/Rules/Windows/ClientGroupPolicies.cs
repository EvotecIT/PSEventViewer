using System;
using System.Collections.Generic;
using System.Text;

namespace EventViewer.Rules.Windows {
    internal class ClientGroupPolicies {
        //Log Name:      Application
        // Source:        Group Policy Files
        // Date:          10.02.2024 16:10:50
        // Event ID:      4098
        // Task Category: (2)
        // Level:         Warning
        // Keywords:      Classic
        // User:          SYSTEM
        // Computer:      EVOMONSTER.ad.evotec.xyz
        // Description:
        // The computer 'Install.ps1' preference item in the 'CopyFiles {76CA620F-5CEB-4316-A0D1-9311E6EE03B9}' Group Policy Object did not apply because it failed with error code '0x80070035 The network path was not found.' This error was suppressed.
        // Event Xml:
        // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
        //   <System>
        //     <Provider Name="Group Policy Files" />
        //     <EventID Qualifiers="34305">4098</EventID>
        //     <Version>0</Version>
        //     <Level>3</Level>
        //     <Task>2</Task>
        //     <Opcode>0</Opcode>
        //     <Keywords>0x80000000000000</Keywords>
        //     <TimeCreated SystemTime="2024-02-10T15:10:50.0074671Z" />
        //     <EventRecordID>22479</EventRecordID>
        //     <Correlation />
        //     <Execution ProcessID="1160" ThreadID="0" />
        //     <Channel>Application</Channel>
        //     <Computer>EVOMONSTER.ad.evotec.xyz</Computer>
        //     <Security UserID="S-1-5-18" />
        //   </System>
        //   <EventData>
        //     <Data>computer</Data>
        //     <Data>Install.ps1</Data>
        //     <Data>CopyFiles {76CA620F-5CEB-4316-A0D1-9311E6EE03B9}</Data>
        //     <Data>0x80070035 The network path was not found.</Data>
        //   </EventData>
        // </Event>




        //Log Name:      System
        // Source:        Microsoft-Windows-GroupPolicy
        // Date:          11.02.2024 12:46:49
        // Event ID:      1085
        // Task Category: None
        // Level:         Warning
        // Keywords:      
        // User:          SYSTEM
        // Computer:      EVOMONSTER.ad.evotec.xyz
        // Description:
        // Windows failed to apply the MDM Policy settings. MDM Policy settings might have its own log file. Please click on the "More information" link.
        // Event Xml:
        // <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
        //   <System>
        //     <Provider Name="Microsoft-Windows-GroupPolicy" Guid="{aea1b4fa-97d1-45f2-a64c-4d69fffd92c9}" />
        //     <EventID>1085</EventID>
        //     <Version>0</Version>
        //     <Level>3</Level>
        //     <Task>0</Task>
        //     <Opcode>1</Opcode>
        //     <Keywords>0x8000000000000000</Keywords>
        //     <TimeCreated SystemTime="2024-02-11T11:46:49.9245620Z" />
        //     <EventRecordID>34081</EventRecordID>
        //     <Correlation ActivityID="{cc36e32c-163a-4f85-872e-e4c5a1a74bcb}" />
        //     <Execution ProcessID="1160" ThreadID="11620" />
        //     <Channel>System</Channel>
        //     <Computer>EVOMONSTER.ad.evotec.xyz</Computer>
        //     <Security UserID="S-1-5-18" />
        //   </System>
        //   <EventData>
        //     <Data Name="SupportInfo1">1</Data>
        //     <Data Name="SupportInfo2">5213</Data>
        //     <Data Name="ProcessingMode">0</Data>
        //     <Data Name="ProcessingTimeInMilliseconds">641</Data>
        //     <Data Name="ErrorCode">2149056522</Data>
        //     <Data Name="ErrorDescription">The device is already enrolled. </Data>
        //     <Data Name="DCName">\\AD1.ad.evotec.xyz</Data>
        //     <Data Name="ExtensionName">MDM Policy</Data>
        //     <Data Name="ExtensionId">{7909AD9E-09EE-4247-BAB9-7029D5F0A278}</Data>
        //   </EventData>
        // </Event>
    }
}
