using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// A summary of LDAP binding activity.
/// </summary>
[NamedEvent(NamedEvents.ADLdapBindingSummary, "Directory Service", 2887)]
public class ADLdapBindingSummary : EventObjectSlim {
    public string Computer;
    public string Action;
    public string NumberOfSimpleBindsWithoutSSLTLS;
    public string NumberOfSimpleBindsWithSSLTLS;
    public string NumberOfNegotiateBindsUnprotected;
    public string NumberOfNegotiateBindsProtected;

    public ADLdapBindingSummary(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADLdapBindingSummary";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        NumberOfSimpleBindsWithoutSSLTLS = _eventObject.GetValueFromDataDictionary("NumberOfSimpleBindsWithoutSSLOrTLS");
        NumberOfSimpleBindsWithSSLTLS = _eventObject.GetValueFromDataDictionary("NumberOfSimpleBindsWithSSLOrTLS");
        NumberOfNegotiateBindsUnprotected = _eventObject.GetValueFromDataDictionary("NumberOfNegotiateBindsWithoutSSLOrTLS");
        NumberOfNegotiateBindsProtected = _eventObject.GetValueFromDataDictionary("NumberOfNegotiateBindsWithSSLOrTLS");
    }
}
