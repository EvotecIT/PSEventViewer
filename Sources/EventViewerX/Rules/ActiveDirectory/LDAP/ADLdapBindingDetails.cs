namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// LDAP binding details.
/// 2889: The security of this directory server can be significantly enhanced by configuring the server to reject SASL (Negotiate, Kerberos, NTLM, or Digest) LDAP binds that do not request signing (integrity verification) and LDAP simple binds that are performed on a cleartext (non-SSL/TLS-encrypted) connection. Even if this directory server is not used by Active Directory, it is recommended that this server be configured to reject such binds. For more details and information on how to make this configuration change to the server, please see http://go.microsoft.com/fwlink/?LinkID=87923.
/// </summary>
public class ADLdapBindingDetails : EventObjectSlim {
    public string Computer;
    public string Action;
    private string IPPort;
    public string AccountName;
    private string BindType;
    public DateTime When;

    public string LogName = "Directory Service";
    //public List<int> EventID = [2889];


    public ADLdapBindingDetails(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "ADLdapBindingDetails";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        IPPort = _eventObject.GetValueFromDataDictionary("NoNameA0");
        AccountName = _eventObject.GetValueFromDataDictionary("NoNameA1");
        BindType = _eventObject.GetValueFromDataDictionary("NoNameA2");
        if (BindType == "0") {
            BindType = "Unsigned";
        } else if (BindType == "1") {
            BindType = "Simple";
        }
        When = _eventObject.TimeCreated;
    }
}