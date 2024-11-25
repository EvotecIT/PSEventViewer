using DnsClientX;

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
        public string ClientAddress;
        public string ClientDNSName;
        public DateTime When;

        public SMBServerAudit(EventObject eventObject) : base(eventObject) {
            _eventObject = eventObject;
            Type = "ADSMBServerAuditV1";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ClientAddress = _eventObject.GetValueFromDataDictionary("ClientName");
            When = _eventObject.TimeCreated;

            InitializeClientDNSNameAsync().Wait();
        }

        private static async Task<string> QueryDnsAsync(string clientAddress) {
            if (string.IsNullOrEmpty(clientAddress)) {
                return null;
            }
            var result = await ClientX.QueryDns(clientAddress, DnsRecordType.PTR);
            return string.Join(", ", result.AnswersMinimal.Select(answer => answer.Data));
        }

        private async Task InitializeClientDNSNameAsync() {
            ClientDNSName = await QueryDnsAsync(ClientAddress);
        }
    }
}
