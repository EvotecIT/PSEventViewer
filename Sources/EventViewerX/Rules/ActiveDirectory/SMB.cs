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
       // public EventObject EventObject { get; }
        public string Computer;
        public string Action;
        public string ClientAddress;
        public string ClientDNSName = string.Empty;
        public DateTime When;

        // private ctor that performs partial initialization
        private SMBServerAudit(EventObject eventObject) : base(eventObject) {
            //EventObject = eventObject;

            _eventObject = eventObject;
            Type = "ADSMBServerAuditV1";
            Computer = _eventObject.ComputerName;
            Action = _eventObject.MessageSubject;
            ClientAddress = _eventObject.GetValueFromDataDictionary("ClientName");
            When = _eventObject.TimeCreated;
        }

        // static instance creation method to hide the async initialization
        public static SMBServerAudit Create(EventObject eventObject) {
            var smbAudit = new SMBServerAudit(eventObject);
            // smbAudit.ClientDNSName = QueryDnsAsync(smbAudit.ClientAddress).ConfigureAwait(false).GetAwaiter().GetResult();
            smbAudit.ClientDNSName = Task.Run(() => QueryDnsAsync(smbAudit.ClientAddress)).Result;
            return smbAudit;
        }


        private static async Task<string> QueryDnsAsync(string clientAddress) {
            if (string.IsNullOrEmpty(clientAddress)) {
                return null;
            }

            try {
                Settings._logger.WriteVerbose($"Querying DNS for address: {clientAddress}");
                var result = await ClientX.QueryDns(clientAddress, DnsRecordType.PTR);
                var resolvedNames = string.Join(", ", result.AnswersMinimal.Select(answer => answer.Data));
                Settings._logger.WriteVerbose($"Resolved names: {resolvedNames}");
                return resolvedNames;
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Querying DNS for address: {clientAddress} failed: {ex.Message}");
                return null;
            }
        }
    }
}
