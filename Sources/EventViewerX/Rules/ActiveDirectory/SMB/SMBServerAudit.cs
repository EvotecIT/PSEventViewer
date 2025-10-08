using DnsClientX;

using EventViewerX;

namespace EventViewerX.Rules.ActiveDirectory;
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
public class SMBServerAudit : EventRuleBase {
    // public EventObject EventObject { get; }
    public string Computer = string.Empty;
    public string Action = string.Empty;
    public string ClientAddress = string.Empty;
    public string ClientDNSName = string.Empty;
    public DateTime When;
    public override List<int> EventIds => new() { 3000 };
    public override string LogName => "Microsoft-Windows-SMBServer/Audit";
    public override NamedEvents NamedEvent => NamedEvents.ADSMBServerAuditV1;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }

    // public ctor that performs partial initialization
    public SMBServerAudit(EventObject eventObject) : base(eventObject) {
        //EventObject = eventObject;

        _eventObject = eventObject;
        Type = "ADSMBServerAuditV1";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ClientAddress = _eventObject.GetValueFromDataDictionary("ClientName");
        When = _eventObject.TimeCreated;
        ClientDNSName = Task.Run(() => QueryDnsAsync(ClientAddress)).Result ?? string.Empty;
    }


    private static async Task<string?> QueryDnsAsync(string clientAddress) {
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
