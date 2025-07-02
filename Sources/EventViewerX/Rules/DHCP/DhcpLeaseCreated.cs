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

    public string Computer;
    public string IPAddress;
    public string MacAddress;
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
