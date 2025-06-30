using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// LDAP binding details.
/// </summary>
[NamedEvent(NamedEvents.ADLdapBindingDetails, "Directory Service", 2889)]
public class ADLdapBindingDetails : EventObjectSlim {
    public string Computer;
    public string Action;
    private string IPPort;
    public string IP;
    public string Port;
    public string CredentialType;
    public string AuthenticationPackage;

    public ADLdapBindingDetails(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADLdapBindingDetails";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        IPPort = _eventObject.GetValueFromDataDictionary("IPAddress");
        if (IPPort?.Contains(":") == true) {
            var parts = IPPort.Split(':');
            IP = parts[0];
            Port = parts[1];
        }
        CredentialType = _eventObject.GetValueFromDataDictionary("CredentialType");
        AuthenticationPackage = _eventObject.GetValueFromDataDictionary("AuthenticationPackage");
    }
}
