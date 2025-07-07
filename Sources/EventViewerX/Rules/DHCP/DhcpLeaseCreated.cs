namespace EventViewerX.Rules.DHCP;

/// <summary>
/// DHCP lease creation event
/// 10: A new IP address was leased to a client
/// </summary>
public class DhcpLeaseCreated : EventRuleBase {
    public override List<int> EventIds => new() { 10 };
    public override string LogName => "Microsoft-Windows-DHCP Server/Operational";
    public override NamedEvents NamedEvent => NamedEvents.DhcpLeaseCreated;

    public override bool CanHandle(EventObject eventObject) {
        // Always handle if event ID and log name match
        return true;
    }

    /// <summary>
    /// Computer that issued the lease.
    /// </summary>
    public string Computer;

    /// <summary>
    /// IP address leased to the client.
    /// </summary>
    public string IPAddress;

    /// <summary>
    /// MAC address of the client.
    /// </summary>
    public string MacAddress;

    /// <summary>
    /// Time when the lease was created.
    /// </summary>
    public DateTime When;

    public DhcpLeaseCreated(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "DhcpLeaseCreated";
        Computer = _eventObject.ComputerName;
        IPAddress = _eventObject.GetValueFromDataDictionary("IpAddress", "ClientIP");
        MacAddress = _eventObject.GetValueFromDataDictionary("HWAddress", "MacAddress");
        When = _eventObject.TimeCreated;
    }
}
