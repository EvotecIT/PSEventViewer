using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSEventViewer.Examples {
    internal partial class Examples {

        public static void QueryWithTasks() {

            var eventSearching = new EventSearching();
            eventSearching.Verbose = true;
            List<string> MachineName = new List<string> { "AD1", "AD2", "AD0" };
            string LogName = "Security";
            List<int> EventId = new List<int>() { 5136 };

            var queryTask = eventSearching.QueryLogParallelTask(MachineName, LogName, EventId);
            queryTask.GetAwaiter().GetResult();
        }
    }
}
