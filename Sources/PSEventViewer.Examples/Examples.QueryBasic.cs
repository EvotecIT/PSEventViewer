using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSEventViewer.Examples {
    internal partial class Examples {

        public static void QueryBasic() {
            EventSearching.QueryLog("Application", [1008, 4098, 1001], "AD1");
            EventSearching.QueryLog("Application", [1001], "AD1");
            EventSearching.QueryLog("Security", new List<int>() { 4627, 4624 }, "AD1");
        }

        public static void QueryBasicWithOutput() {
            foreach (var eventObject in EventSearching.QueryLog("Security", [4932, 4933], "AD1")) {
                Console.WriteLine("Event ID: {0}", eventObject.Id);
                Console.WriteLine("Data count: " + eventObject.Data.Count);
                foreach (var data in eventObject.Data) {
                    Console.WriteLine("[-] Data: {0} - {1}", data.Key, data.Value);
                }
            }
        }

        public static void QueryBasicParallelForEach() {
            foreach (var eventObject in EventSearching.QueryLogsParallelForEach("Security", [4932, 4933], ["AD1"])) {
                Console.WriteLine("Event ID: {0}", eventObject.Id);
                Console.WriteLine("Data count: " + eventObject.Data.Count);
                foreach (var data in eventObject.Data) {
                    Console.WriteLine("[-] Data: {0} - {1}", data.Key, data.Value);
                }
            }
        }
    }
}