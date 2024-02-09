using PSEventViewer.Rules.ActiveDirectory;

namespace PSEventViewer.Examples {
    internal partial class Examples {
        public static void FindEventsTargetedBasic() {
            foreach (var foundObject in EventSearchingTargeted.FindEvents()) {
                // Check if the foundObject is of type ADComputerChangeDetailed

                // Console.WriteLine("Event ID: {0}", foundObject.EventID + ", " + foundObject.Type + " " + foundObject.GatheredFrom);
                Console.WriteLine("Type: " + foundObject.Type + ", " + foundObject.EventID + " " + foundObject.EventID + " " + foundObject.GatheredFrom);

                if (foundObject is ADComputerChangeDetailed adComputerChange) {
                    // Display the properties of the ADComputerChangeDetailed object
                    Console.WriteLine("[*] Computer: " + adComputerChange.Computer);
                    Console.WriteLine("[*] Action: " + adComputerChange.Action);
                    Console.WriteLine("[*] Operation Type: " + adComputerChange.OperationType);
                    Console.WriteLine("[*] Who: " + adComputerChange.Who);
                    Console.WriteLine("[*] When: " + adComputerChange.When);
                    Console.WriteLine("[*] Object DN: " + adComputerChange.ComputerObject);
                    Console.WriteLine("[*] Field Changed: " + adComputerChange.FieldChanged);
                    Console.WriteLine("[*] Field Value: " + adComputerChange.FieldValue);
                }
            }
        }


        public static void FindEventsTargetedPerType() {
            List<string> MachineName = new List<string> { "AD1", "AD2" };


            // Initialize the logger
            var internalLogger = new InternalLogger(true);

            EventSearchingTargeted eventSearching = new EventSearchingTargeted(internalLogger);
            eventSearching.Verbose = true;

            List<NamedEvents> Type = new List<NamedEvents> { NamedEvents.ADComputerChangeDetailed };
            foreach (var foundObject in EventSearchingTargeted.FindEventsByNamedEventsOld(MachineName, Type)) {
                // Check if the foundObject is of type ADComputerChangeDetailed

                // Console.WriteLine("Event ID: {0}", foundObject.EventID + ", " + foundObject.Type + " " + foundObject.GatheredFrom);
                Console.WriteLine("Type: " + foundObject.Type + ", " + foundObject.EventID + " " + foundObject.EventID + " " + foundObject.GatheredFrom);

                if (foundObject is ADComputerChangeDetailed adComputerChange) {
                    // Display the properties of the ADComputerChangeDetailed object
                    Console.WriteLine("[*] Computer: " + adComputerChange.Computer);
                    Console.WriteLine("[*] Action: " + adComputerChange.Action);
                    Console.WriteLine("[*] Operation Type: " + adComputerChange.OperationType);
                    Console.WriteLine("[*] Who: " + adComputerChange.Who);
                    Console.WriteLine("[*] When: " + adComputerChange.When);
                    Console.WriteLine("[*] Object DN: " + adComputerChange.ComputerObject);
                    Console.WriteLine("[*] Field Changed: " + adComputerChange.FieldChanged);
                    Console.WriteLine("[*] Field Value: " + adComputerChange.FieldValue);
                }
            }
        }
    }
}