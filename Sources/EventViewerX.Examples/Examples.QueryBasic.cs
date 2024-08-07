﻿namespace EventViewerX.Examples {
    internal partial class Examples {

        public static void QueryBasic() {
            SearchEvents.QueryLog("Application", [1008, 4098, 1001], "AD1");
            SearchEvents.QueryLog("Application", [1001], "AD1");
            SearchEvents.QueryLog("Security", new List<int>() { 4627, 4624 }, "AD1");
        }

        public static void QueryBasicForwardedEvents() {
            var list = SearchEvents.QueryLog("ForwardedEvents", [4722, 4738]);
            foreach (var test in list) {
                Console.WriteLine(test.Id + " " + test.QueriedMachine + " " + test.MachineName + " " + test.ContainerLog + " " + test.LogName);
            }
            Console.WriteLine("Count: " + list.Count());
        }

        public static void QueryBasicWithOutput() {
            foreach (var eventObject in SearchEvents.QueryLog("Security", [4932, 4933], "AD1")) {
                Console.WriteLine("Event ID: {0}", eventObject.Id);
                Console.WriteLine("Data count: " + eventObject.Data.Count);
                foreach (var data in eventObject.Data) {
                    Console.WriteLine("[-] Data: {0} - {1}", data.Key, data.Value);
                }
            }
        }

        public static void QueryBasicParallelForEach() {
            foreach (var eventObject in SearchEvents.QueryLogsParallelForEach("Security", [4932, 4933], ["AD1"])) {
                Console.WriteLine("Event ID: {0}", eventObject.Id);
                Console.WriteLine("Data count: " + eventObject.Data.Count);
                foreach (var data in eventObject.Data) {
                    Console.WriteLine("[-] Data: {0} - {1}", data.Key, data.Value);
                }
            }
        }
    }
}