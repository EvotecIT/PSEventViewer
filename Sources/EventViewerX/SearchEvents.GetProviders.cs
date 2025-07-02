namespace EventViewerX {
    /// <summary>
    /// Placeholder for future provider related methods.
    /// </summary>
    public partial class SearchEvents : Settings {

        //public static void GetProviderList() {
        //    // Create a new event log session
        //    using (EventLogSession session = new EventLogSession()) {
        //        // Get the names of all providers
        //        var providerNames = session.GetProviderNames();

        //        // Get all event logs and store them in a dictionary
        //        var logDictionary = EventLog.GetEventLogs().ToDictionary(log => log.LogDisplayName, log => log.Log, StringComparer.OrdinalIgnoreCase);

        //        // For each provider, get its metadata and print its name, ID, and corresponding event log
        //        foreach (var providerName in providerNames) {
        //            try {
        //                using (var providerMetadata = new ProviderMetadata(providerName)) {
        //                    string logName = logDictionary.ContainsKey(providerName) ? logDictionary[providerName] : "Not available";
        //                    Console.WriteLine($"Provider Name: {providerMetadata.Name}, Provider ID: {providerMetadata.Id}, Log: {logName}");
        //                }
        //            } catch (EventLogException) {
        //                // Some providers may not support querying for metadata
        //                // In that case, just print the provider name
        //                Console.WriteLine($"Provider Name: {providerName}, Provider ID: Not available");
        //            }
        //        }
        //    }
        //}
    }
}