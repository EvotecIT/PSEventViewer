namespace EventViewerX.Rules.ActiveDirectory;

/// <summary>
/// Active Directory User Logon Kerberos
/// 4768: A Kerberos authentication ticket (TGT) was requested
/// </summary>
public class ADUserLogonKerberos : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 4768 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.ADUserLogonKerberos;

    /// <summary>Accepts any 4768 Kerberos TGT request events.</summary>
    public override bool CanHandle(EventObject eventObject) {
        return true;
    }
    /// <summary>Domain controller processing the TGT request.</summary>
    public string Computer;
    /// <summary>Action from the message subject.</summary>
    public string Action;
    /// <summary>Account requesting the ticket.</summary>
    public string ObjectAffected;
    /// <summary>Source IP address.</summary>
    public string IpAddress;
    /// <summary>Source port.</summary>
    public string IpPort;
    /// <summary>Ticket options bitmask as string.</summary>
    public string TicketOptions;
    /// <summary>Status code string returned by KDC.</summary>
    public string Status;
    /// <summary>Encryption type string.</summary>
    public string TicketEncryptionType;
    /// <summary>Pre-authentication type string.</summary>
    public string PreAuthType;
    /// <summary>Timestamp of the request.</summary>
    public DateTime When;

    /// <summary>Initialises a Kerberos logon wrapper from an event record.</summary>
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
