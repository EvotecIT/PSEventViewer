namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// LDAP binding details.
/// 2889: The security of this directory server can be significantly enhanced by configuring the server to reject SASL (Negotiate, Kerberos, NTLM, or Digest) LDAP binds that do not request signing (integrity verification) and LDAP simple binds that are performed on a cleartext (non-SSL/TLS-encrypted) connection. Even if this directory server is not used by Active Directory, it is recommended that this server be configured to reject such binds. For more details and information on how to make this configuration change to the server, please see http://go.microsoft.com/fwlink/?LinkID=87923.
/// </summary>
public class ADLdapBindingDetails : EventRuleBase {
    public override List<int> EventIds => new() { 2889 };
    public override string LogName => "Directory Service";
    public override NamedEvents NamedEvent => NamedEvents.ADLdapBindingDetails;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    public string Computer;
    public string Action;
    public string RemoteEndpoint;
    public string AccountName;
    public string BindType;
    public DateTime When;


    public ADLdapBindingDetails(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "ADLdapBindingDetails";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        RemoteEndpoint = _eventObject.GetValueFromDataDictionary("NoNameA0");
        AccountName = _eventObject.GetValueFromDataDictionary("NoNameA1");
        BindType = TranslateBindType(_eventObject.GetValueFromDataDictionary("NoNameA2"));
        When = _eventObject.TimeCreated;
    }

    private static string TranslateBindType(string raw)
    {
        return raw switch
        {
            "0" => "Unsigned",
            "1" => "Simple",
            _ => string.IsNullOrWhiteSpace(raw) ? "Unknown" : raw
        };
    }
}
