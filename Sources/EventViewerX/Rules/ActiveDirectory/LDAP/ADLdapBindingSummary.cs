namespace EventViewerX.Rules.ActiveDirectory;

// <summary>
/// A summary of LDAP binding activity.
/// 2887(0x0B57) - The security of this directory server can be significantly enhanced by configuring the server to reject SASL (Negotiate, Kerberos, NTLM, or Digest) LDAP binds that do not request signing (integrity verification) and LDAP simple binds that are performed on a cleartext (non-SSL/TLS-encrypted) connection. Even if this directory server is not used by Active Directory, it is recommended that this server be configured to reject such binds. For more details and information on how to make this configuration change to the server, please see http://go.microsoft.com/fwlink/?LinkID=87923.
/// </summary>
public class ADLdapBindingSummary : EventRuleBase {
    public override List<int> EventIds => new() { 2887 };
    public override string LogName => "Directory Service";
    public override NamedEvents NamedEvent => NamedEvents.ADLdapBindingSummary;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string NumberOfSimpleBindsWithoutSSLTLS;
    public string NumberOfNegotiateKerberosNtlmDigestBindsPerformedWithoutSigning;
    public string TaskDisplayName;
    public string LevelDisplayName;
    public DateTime When;

    public ADLdapBindingSummary(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "ADLdapBindingSummary";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        NumberOfSimpleBindsWithoutSSLTLS = _eventObject.GetValueFromDataDictionary("NoNameA0");
        NumberOfNegotiateKerberosNtlmDigestBindsPerformedWithoutSigning = _eventObject.GetValueFromDataDictionary("NoNameA1");
        When = _eventObject.TimeCreated;
        LevelDisplayName = eventObject.LevelDisplayName;
        TaskDisplayName = eventObject.TaskDisplayName;
    }
}
