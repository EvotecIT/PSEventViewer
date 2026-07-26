using EventViewerX.Rules.Windows;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static async Task FindNetworkMonitorEvents() {
            var query = new NamedEventQuery([
                NamedEvents.NetworkMonitorDriverLoaded,
                NamedEvents.NetworkPromiscuousMode
            ]);
            await foreach (var evt in NamedEventEngine.ReadAsync(query)) {
                var computer = evt switch {
                    NetworkMonitorDriverLoaded driver => driver.Computer,
                    NetworkPromiscuousMode promiscuous => promiscuous.Computer,
                    _ => evt.GatheredFrom
                };

                Console.WriteLine($"Event: {evt.Type} {evt.EventID} on {computer}");
            }
        }
    }
}
