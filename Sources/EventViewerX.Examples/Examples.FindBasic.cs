using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Windows;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static async Task FindEventsTargetedBasic() {

            await foreach (var foundObject in SearchEvents.FindEventsByNamedEvents([NamedEvents.OSCrash, NamedEvents.OSStartupShutdownCrash])) {

                Console.WriteLine("Event ID: {0}", foundObject.EventID + ", " + foundObject.Type + " " + foundObject.GatheredFrom);
                Console.WriteLine("Type: " + foundObject.Type + ", " + foundObject.EventID + " " + foundObject.EventID + " " + foundObject.GatheredFrom);

                if (foundObject is OSCrash osCrash) {
                    //Display the properties of the ADComputerChangeDetailed object
                    Console.WriteLine("[*] Computer: " + osCrash.Computer);
                    Console.WriteLine("[*] Who: " + osCrash.Who);
                    Console.WriteLine("[*] When: " + osCrash.When);
                } else if (foundObject is OSStartupShutdownCrash osStartupShutdownCrash) {
                    //Display the properties of the ADComputerChangeDetailed object
                    Console.WriteLine("[*] Computer: " + osStartupShutdownCrash.Computer);
                    Console.WriteLine("[*] Who: " + osStartupShutdownCrash.Action);
                    Console.WriteLine("[*] When: " + osStartupShutdownCrash.When);
                }
            }
        }

        public static async Task FindEventsTargetedPerType() {
            List<string> MachineName = new List<string> { "AD1", "AD2", "AD0" };

            // Initialize the logger
            var internalLogger = new InternalLogger(true);

            SearchEvents eventSearching = new SearchEvents(internalLogger);
            eventSearching.Verbose = true;

            List<NamedEvents> Type = new List<NamedEvents> { NamedEvents.ADLdapBindingDetails, NamedEvents.ADLdapBindingSummary };
            await foreach (var foundObject in SearchEvents.FindEventsByNamedEvents(Type, MachineName)) {
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