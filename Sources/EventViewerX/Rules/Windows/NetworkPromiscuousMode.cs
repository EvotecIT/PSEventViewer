namespace EventViewerX.Rules.Windows;

/// <summary>
/// Network adapter entered promiscuous mode
/// 104: NDIS indicates adapter promiscuous mode
/// </summary>
public class NetworkPromiscuousMode : EventRuleBase {
    public override List<int> EventIds => new() { 10400, 10401 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.NetworkPromiscuousMode;

    public override bool CanHandle(EventObject eventObject) {
        return eventObject.ProviderName.Equals("Ndis", StringComparison.OrdinalIgnoreCase) &&
               (eventObject.Id == 10400 || eventObject.Id == 10401);
    }

    public string Computer;
    public string Adapter;
    public DateTime When;

    public NetworkPromiscuousMode(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "NetworkPromiscuousMode";
        Computer = _eventObject.ComputerName;
        Adapter = _eventObject.GetValueFromDataDictionary("AdapterName", "Name");
        When = _eventObject.TimeCreated;
    }
}
