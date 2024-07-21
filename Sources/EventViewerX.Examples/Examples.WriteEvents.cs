using System.Diagnostics;

namespace EventViewerX.Examples {
    internal partial class Examples {

        public static void WriteBasic() {

            SearchEvents search = new SearchEvents();
            search.Verbose = true;
            search.Warning = true;
            search.Error = true;

            SearchEvents.WriteEvent("MySource", "Application", "This is a test message", EventLogEntryType.Information, 101, "AD1", "Replacement string 1", "Replacement string 2");
        }

        public static void WriteToDifferentLog() {
            SearchEvents.CreateLogSource("MyApplication", "MyCustomLog");
            SearchEvents.WriteEvent("MySource", "MyApplication", "This is a test message", EventLogEntryType.Information, 101, null, "Replacement string 1", "Replacement string 2");
        }

        public static void WriteAdvanced() {
            SearchEvents search = new SearchEvents();
            search.Verbose = true;
            search.Warning = true;
            search.Error = true;

            // SearchEvents.WriteEvent("System", "RetailDemo", "is is a test message");
            // SearchEvents.WriteEvent("System", "WinLogon", "is is a test message");
            SearchEvents.WriteEventEx(
                "log", "serviceName", "Authentication started.",
                2, 0, 1, 16, 4, 1, 0x4000200000010000);

        }
    }
}