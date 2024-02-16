using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace PSEventViewer.Examples {
    internal partial class Examples {

        public static void WriteBasic() {

            SearchEvents search = new SearchEvents();
            search.Verbose = true;
            search.Warning = true;
            search.Error = true;

            SearchEvents.WriteToEventLog("MySource", "Application", "This is a test message", EventLogEntryType.Information, 101, "AD1", "Replacement string 1", "Replacement string 2");
        }

        public static void WriteToDifferentLog() {
            SearchEvents.CreateLogSource("MyApplication", "MyCustomLog");
            SearchEvents.WriteToEventLog("MySource", "MyApplication", "This is a test message", EventLogEntryType.Information, 101, null, "Replacement string 1", "Replacement string 2");
        }

        public static void WriteAdvanced() {

        }
    }
}