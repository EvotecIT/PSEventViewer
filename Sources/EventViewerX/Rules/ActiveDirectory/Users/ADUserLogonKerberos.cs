namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon Kerberos
/// 4768: A Kerberos authentication ticket (TGT) was requested
/// </summary>
public class ADUserLogonKerberos : EventObjectSlim {
    public string Computer;
    public string Action;
    public string ObjectAffected;
    public string IpAddress;
    public string IpPort;
    public string TicketOptions;
    public string Status;
    public string TicketEncryptionType;
    public string PreAuthType;
    public DateTime When;

    public ADUserLogonKerberos(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "ADUserLogonKerberos";

        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        ObjectAffected = _eventObject.GetValueFromDataDictionary("TargetUserName", "TargetDomainName", "\\", reverseOrder: true);
        IpAddress = _eventObject.GetValueFromDataDictionary("IpAddress");
        IpPort = _eventObject.GetValueFromDataDictionary("IpPort");
        TicketOptions = _eventObject.GetValueFromDataDictionary("TicketOptions");
        Status = _eventObject.GetValueFromDataDictionary("Status");
        TicketEncryptionType = _eventObject.GetValueFromDataDictionary("TicketEncryptionType");
        PreAuthType = _eventObject.GetValueFromDataDictionary("PreAuthType");

        When = _eventObject.TimeCreated;

        if (IpAddress == "::1") {
            IpAddress = "Localhost";
        }
    }
}
