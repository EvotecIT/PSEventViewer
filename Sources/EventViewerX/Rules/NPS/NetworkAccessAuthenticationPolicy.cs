namespace EventViewerX.Rules.NPS;

/// <summary>
/// Network Access Authentication Policy
/// 6272: Network Policy Server granted access to a user
/// 6273: Network Policy Server denied access to a user
/// </summary>
public class NetworkAccessAuthenticationPolicy : EventRuleBase {
    public override List<int> EventIds => new() { 6272, 6273 };
    public override string LogName => "Security";
    public override NamedEvents NamedEvent => NamedEvents.NetworkAccessAuthenticationPolicy;

    public override bool CanHandle(EventObject eventObject) {
        // Simple rule - always handle if event ID and log name match
        return true;
    }
    /// <summary>
    /// Computer where the policy event originated.
    /// </summary>
    public string Computer;

    /// <summary>
    /// Brief description of the action.
    /// </summary>
    public string Action;

    /// <summary>
    /// Security identifier of the user.
    /// </summary>
    public string SecurityID;

    /// <summary>
    /// Account name of the user.
    /// </summary>
    public string AccountName;

    /// <summary>
    /// Domain of the user account.
    /// </summary>
    public string AccountDomain;

    /// <summary>
    /// Called station identifier.
    /// </summary>
    public string CalledStationID;

    /// <summary>
    /// Calling station identifier.
    /// </summary>
    public string CallingStationID;

    /// <summary>
    /// IPv4 address of the NAS device.
    /// </summary>
    public string NASIPv4Address;

    /// <summary>
    /// IPv6 address of the NAS device.
    /// </summary>
    public string NASIPv6Address;

    /// <summary>
    /// NAS identifier string.
    /// </summary>
    public string NASIdentifier;

    /// <summary>
    /// Type of the network access server port.
    /// </summary>
    public NasPortType? NASPortType;

    /// <summary>
    /// NAS port number.
    /// </summary>
    public string NASPort;

    /// <summary>
    /// Friendly name of the client.
    /// </summary>
    public string ClientFriendlyName;

    /// <summary>
    /// Client IP address in readable form.
    /// </summary>
    public string ClientFriendlyIPAddress;

    /// <summary>
    /// Connection request policy name.
    /// </summary>
    public string ConnectionRequestPolicyName;

    /// <summary>
    /// Network policy name applied.
    /// </summary>
    public string NetworkPolicyName;

    /// <summary>
    /// Authentication provider used.
    /// </summary>
    public string AuthenticationProvider;

    /// <summary>
    /// Server performing the authentication.
    /// </summary>
    public string AuthenticationServer;

    /// <summary>
    /// Authentication type selected.
    /// </summary>
    public string AuthenticationType;

    /// <summary>
    /// EAP type value if applicable.
    /// </summary>
    public string EAPType;

    /// <summary>
    /// Human readable reason string.
    /// </summary>
    public string Reason;

    /// <summary>
    /// Numeric reason code.
    /// </summary>
    public string ReasonCode;

    /// <summary>
    /// User that triggered the policy event.
    /// </summary>
    public string Who;

    /// <summary>
    /// Time when the policy event occurred.
    /// </summary>
    public DateTime When;

    public NetworkAccessAuthenticationPolicy(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;

        Type = "NetworkAccessAuthenticationPolicy";
        Computer = _eventObject.ComputerName;
        Action = _eventObject.MessageSubject;
        SecurityID = _eventObject.GetValueFromDataDictionary("SubjectUserSid");
        AccountName = _eventObject.GetValueFromDataDictionary("SubjectUserName");
        AccountDomain = _eventObject.GetValueFromDataDictionary("SubjectDomainName");

        CalledStationID = _eventObject.GetValueFromDataDictionary("CalledStationID");
        CallingStationID = _eventObject.GetValueFromDataDictionary("CallingStationID");

        NASIPv4Address = _eventObject.GetValueFromDataDictionary("NASIPv4Address");
        NASIPv6Address = _eventObject.GetValueFromDataDictionary("NASIPv6Address");

        NASIdentifier = _eventObject.GetValueFromDataDictionary("NASIdentifier");
        NASPort = _eventObject.GetValueFromDataDictionary("NASPort");
        NASPortType = EventsHelper.GetNasPortType(
            _eventObject.GetValueFromDataDictionary("NASPortType"));


        AuthenticationProvider = _eventObject.GetValueFromDataDictionary("AuthenticationProvider");
        AuthenticationServer = _eventObject.GetValueFromDataDictionary("AuthenticationServer");
        AuthenticationType = _eventObject.GetValueFromDataDictionary("AuthenticationType");

        EAPType = _eventObject.GetValueFromDataDictionary("EAPType");

        ClientFriendlyIPAddress = _eventObject.GetValueFromDataDictionary("ClientIPAddress");
        ClientFriendlyName = _eventObject.GetValueFromDataDictionary("ClientName");

        ConnectionRequestPolicyName = _eventObject.GetValueFromDataDictionary("ProxyPolicyName");

        NetworkPolicyName = _eventObject.GetValueFromDataDictionary("NetworkPolicyName");

        Reason = _eventObject.GetValueFromDataDictionary("Reason");
        ReasonCode = _eventObject.GetValueFromDataDictionary("ReasonCode");
        // common fields
        Who = _eventObject.GetValueFromDataDictionary("FullyQualifiedSubjectUserName");
        When = _eventObject.TimeCreated;
    }
}
