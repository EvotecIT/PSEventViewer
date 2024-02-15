using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSEventViewer.Examples {
    internal partial class Examples {

        public static void QueryParallelSpeed() {
            var eventSearching = new SearchEvents {
                Verbose = true,
                Warning = true,
                Error = true,
                Debug = true,
                NumberOfThreads = 1,
            };

            var machineNames = new List<string> { "AD1", "AD2", "AD3" }; // Add your machine names here
            var eventIds = new List<int> { 4932, 4933 }; // Add your event IDs here

            Parallel.ForEach(machineNames, machine => {
                foreach (var eventObject in SearchEvents.QueryLog("Security", eventIds, machine)) {

                }
            });
        }

        public static void QueryParallelCompare() {
            var eventSearching = new SearchEvents {
                Verbose = true,
                Warning = true,
                Error = true,
                Debug = true
            };

            var machineNames = new List<string> { "AD1", "AD2", "AD3" }; // Add your machine names here
            var eventIds = new List<int> { 4932, 4933 }; // Add your event IDs here

            var stopwatch = Stopwatch.StartNew();
            int eventCount1 = 0;
            Parallel.ForEach(machineNames, machine => {
                foreach (var eventObject in SearchEvents.QueryLog("Security", eventIds, machine)) {
                    eventCount1++;
                }
            });
            stopwatch.Stop();
            Console.WriteLine($"Parallel.ForEach method took {stopwatch.ElapsedMilliseconds} ms and returned {eventCount1} events.");

            stopwatch.Restart();
            int eventCount2 = 0;
            foreach (var eventObject in SearchEvents.QueryLogsParallel("Security", eventIds, machineNames)) {
                eventCount2++;
            }
            stopwatch.Stop();
            Console.WriteLine($"QueryLogsParallel method took {stopwatch.ElapsedMilliseconds} ms and returned {eventCount2} events.");

            stopwatch.Restart();
            int eventCount3 = 0;
            foreach (var eventObject in SearchEvents.QueryLogsParallelForEach("Security", eventIds, machineNames)) {
                eventCount3++;
            }
            stopwatch.Stop();
            Console.WriteLine($"QueryBasicParallelForEach method took {stopwatch.ElapsedMilliseconds} ms and returned {eventCount3} events.");

            if (eventCount1 == eventCount2 && eventCount2 == eventCount3) {
                Console.WriteLine("All methods returned the same number of events.");
            } else {
                Console.WriteLine("The methods returned a different number of events.");
            }
        }
    }
}
