
using System;

namespace EventViewerX.Rules.ActiveDirectory {
    /// <summary>
    /// SMB Server Audit
    /// 3000: SMB1 access
    ///
    /// Before running the script, you need to enable the audit policy on the server.
    /// Set-SmbServerConfiguration -AuditSmb1Access $true
    ///
    /// Alternatively via registry:
    /// HKLM\SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters
    /// AuditSmb1Access => REG_DWORD => 1
    /// </summary>
    public class SMBServerAudit : EventObjectSlim {
        public string Computer;
        public string Action;
        public string ClientName;
        public string ClientAddress;
        public DateTime When;

        public SMBServerAudit(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADSMBServerAuditV1";

            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ClientAddress = _eventObject.GetValueFromDataDictionary("Client Address");
            When = _eventObject.TimeCreated;
        }
    }
}