using EventViewerX.Rules.Windows;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static async Task FindNetworkMonitorEvents() {
            await foreach (var evt in SearchEvents.FindEventsByNamedEvents([
                NamedEvents.NetworkMonitorDriverLoaded,
                NamedEvents.NetworkPromiscuousMode
            ])) {
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
