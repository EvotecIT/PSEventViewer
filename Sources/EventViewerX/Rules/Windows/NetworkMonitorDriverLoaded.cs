namespace EventViewerX.Rules.Windows;

/// <summary>
/// Network monitor driver loaded
/// 6: Filter Manager driver loaded
/// </summary>
public class NetworkMonitorDriverLoaded : EventRuleBase {
    public override List<int> EventIds => new() { 6, 7035, 7045 };
    public override string LogName => "System";
    public override NamedEvents NamedEvent => NamedEvents.NetworkMonitorDriverLoaded;

    private static readonly string[] _driverNames = ["npcap", "npf", "netmon"];

    public override bool CanHandle(EventObject eventObject) {
        if (eventObject.ProviderName.Equals("Service Control Manager", StringComparison.OrdinalIgnoreCase) &&
            (eventObject.Id == 7035 || eventObject.Id == 7045)) {
            var msg = eventObject.MessageSubject ?? string.Empty;
            return _driverNames.Any(name => msg.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (eventObject.ProviderName.Equals("Microsoft-Windows-FilterManager", StringComparison.OrdinalIgnoreCase) &&
            eventObject.Id == 6) {
            var drv = eventObject.GetValueFromDataDictionary("DriverName", "FilterName", "ImageName") ?? string.Empty;
            return _driverNames.Any(name => drv.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return false;
    }

    public string Computer;
    public string DriverName;
    public DateTime When;

    public NetworkMonitorDriverLoaded(EventObject eventObject) : base(eventObject) {
        _eventObject = eventObject;
        Type = "NetworkMonitorDriverLoaded";
        Computer = _eventObject.ComputerName;
        DriverName = _eventObject.GetValueFromDataDictionary("DriverName", "FilterName", "ImageName") ??
                     _driverNames.FirstOrDefault(n => (_eventObject.MessageSubject ?? string.Empty)
                         .IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) ?? string.Empty;
        When = _eventObject.TimeCreated;
    }
}
