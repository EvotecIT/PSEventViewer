using EventViewerX;
namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon Kerberos
/// 4768: A Kerberos authentication ticket (TGT) was requested
/// </summary>
[NamedEvent(NamedEvents.ADUserLogonKerberos, "Security", 4768)]
public class ADUserLogonKerberos : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string TicketEncryptionType;
    public string IpAddress;
    public string LogonGuid;
    public string TargetDomainName;
    public string TargetUserName;
    public string SubjectDomainName;
    public string SubjectUserName;
    public DateTime When;

    public ADUserLogonKerberos(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogonKerberos";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        TicketEncryptionType = _eventObject.GetValueFromDataDictionary("TicketEncryptionType");
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        LogonGuid = _eventObject.GetValueFromDataDictionary("LogonGuid");
        TargetDomainName = _eventObject.GetValueFromDataDictionary("TargetDomainName");
        TargetUserName = _eventObject.GetValueFromDataDictionary("TargetUserName");
        SubjectDomainName = _eventObject.GetValueFromDataDictionary("SubjectDomainName");
        SubjectUserName = _eventObject.GetValueFromDataDictionary("SubjectUserName");
        When = _eventObject.TimeCreated;
    }
}
