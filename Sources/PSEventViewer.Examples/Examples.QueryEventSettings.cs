using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventViewer;

namespace PSEventViewer.Examples {
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
    }
}
