namespace EventViewerX.Rules.NPS;

/// <summary>
/// Network Access Authentication Policy
/// 6272: Network Policy Server granted access to a user
/// 6273: Network Policy Server denied access to a user
/// </summary>
public class NetworkAccessAuthenticationPolicy : EventRuleBase {
    /// <inheritdoc />
    public override List<int> EventIds => new() { 6272, 6273 };
    /// <inheritdoc />
    public override string LogName => "Security";
    /// <inheritdoc />
    public override NamedEvents NamedEvent => NamedEvents.NetworkAccessAuthenticationPolicy;

    /// <summary>Accepts any event whose ID/log match the overrides.</summary>
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
    public AuthenticationType AuthenticationType;

    internal static AuthenticationType ParseAuthenticationType(string value) {
        return Enum.TryParse<AuthenticationType>(value, true, out var parsed) ? parsed : AuthenticationType.Unknown;
    }

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

    /// <summary>Initialises an NPS authentication policy wrapper from an event record.</summary>
    public NetworkAccessAuthenticationPolicy(EventObject eventObject) : base(eventObject) {
        Event = eventObject;

        Type = "NetworkAccessAuthenticationPolicy";
        Computer = Event.ComputerName;
        Action = Event.MessageSubject;
        SecurityID = Event.GetDataValueOrEmpty("SubjectUserSid");
        AccountName = Event.GetDataValueOrEmpty(KnownEventField.SubjectUserName);
        AccountDomain = Event.GetDataValueOrEmpty(KnownEventField.SubjectDomainName);

        CalledStationID = Event.GetDataValueOrEmpty("CalledStationID");
        CallingStationID = Event.GetDataValueOrEmpty("CallingStationID");

        NASIPv4Address = Event.GetDataValueOrEmpty("NASIPv4Address");
        NASIPv6Address = Event.GetDataValueOrEmpty("NASIPv6Address");

        NASIdentifier = Event.GetDataValueOrEmpty("NASIdentifier");
        NASPort = Event.GetDataValueOrEmpty("NASPort");
        NASPortType = EventsHelper.GetNasPortType(
            Event.GetDataValueOrEmpty("NASPortType"));


        AuthenticationProvider = Event.GetDataValueOrEmpty("AuthenticationProvider");
        AuthenticationServer = Event.GetDataValueOrEmpty("AuthenticationServer");
        var authType = Event.GetDataValueOrEmpty("AuthenticationType");
        AuthenticationType = ParseAuthenticationType(authType);

        EAPType = Event.GetDataValueOrEmpty("EAPType");

        ClientFriendlyIPAddress = Event.GetDataValueOrEmpty("ClientIPAddress");
        ClientFriendlyName = Event.GetDataValueOrEmpty("ClientName");

        ConnectionRequestPolicyName = Event.GetDataValueOrEmpty("ProxyPolicyName");

        NetworkPolicyName = Event.GetDataValueOrEmpty("NetworkPolicyName");

        Reason = Event.GetDataValueOrEmpty("Reason");
        ReasonCode = Event.GetDataValueOrEmpty("ReasonCode");
        // common fields
        Who = Event.GetDataValueOrEmpty("FullyQualifiedSubjectUserName");
        When = Event.TimeCreated;
    }
}
