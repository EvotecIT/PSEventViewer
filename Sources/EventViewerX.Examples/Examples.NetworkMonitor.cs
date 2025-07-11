namespace EventViewerX.Examples {
    internal partial class Examples {
        public static async Task FindNetworkMonitorEvents() {
            await foreach (var evt in SearchEvents.FindEventsByNamedEvents([
                NamedEvents.NetworkMonitorDriverLoaded,
                NamedEvents.NetworkPromiscuousMode
            ])) {
                Console.WriteLine($"Event: {evt.Type} {evt.EventID} on {evt.ComputerName}");
            }
        }
    }
}
