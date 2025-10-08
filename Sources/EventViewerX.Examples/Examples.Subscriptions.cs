namespace EventViewerX.Examples {
    internal partial class Examples {
        public static void ShowCollectorSubscriptions(string? machineName = null) {
            foreach (var sub in SearchEvents.GetCollectorSubscriptions(machineName)) {
                Console.WriteLine($"{sub.Name} Enabled={sub.Enabled} ContentFormat={sub.ContentFormat} Delivery={sub.DeliveryMode} Queries={sub.Queries.Count}");
            }
        }

        public static void EnableSubscriptionExample(string name, bool enabled, string? machineName = null) {
            var ok = SearchEvents.SetCollectorSubscriptionEnabled(name, enabled, machineName);
            Console.WriteLine($"Enable '{name}' -> {enabled}: {ok}");
        }

        public static void SetSubscriptionXmlExample(string name, string xml, string? machineName = null) {
            var ok = SearchEvents.SetCollectorSubscriptionXml(name, xml, machineName);
            Console.WriteLine($"Set XML '{name}': {ok}");
        }
    }
}

