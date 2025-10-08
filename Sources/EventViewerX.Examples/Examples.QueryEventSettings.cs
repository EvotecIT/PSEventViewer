namespace EventViewerX.Examples {
    internal partial class Examples {

        public static void QueryBasicEventLogList() {

            SearchEvents eventLogSettings = new SearchEvents();
            eventLogSettings.Verbose = true;
            eventLogSettings.Warning = true;
            eventLogSettings.Error = true;

            foreach (var test in SearchEvents.DisplayEventLogs()) {
                Console.WriteLine(test);
            }
        }

        public static void ShowChannelPolicy(string logName = "Application", string? machineName = null) {
            var pol = SearchEvents.GetChannelPolicy(logName, machineName);
            if (pol == null) {
                Console.WriteLine($"No policy for '{logName}'");
                return;
            }
            Console.WriteLine($"{pol.LogName} @ {pol.MachineName ?? Environment.MachineName} | Enabled={pol.IsEnabled} Size={pol.MaximumSizeInBytes} Mode={pol.Mode} Path={pol.LogFilePath}");
        }

        public static void SetChannelPolicyExample(string logName = "Application", string? machineName = null) {
            // Example: increase size for Application log (classic) to 64 MB and set circular
            var ok = SearchEvents.SetChannelPolicy(new ChannelPolicy {
                LogName = logName,
                MachineName = machineName,
                MaximumSizeInBytes = 64L * 1024 * 1024,
                Mode = System.Diagnostics.Eventing.Reader.EventLogMode.Circular
            });
            Console.WriteLine($"Set policy result for '{logName}': {ok}");
        }
    }
}
