using EventViewerX.Rules.ActiveDirectory;
using EventViewerX.Rules.Windows;

namespace EventViewerX.Examples {
    internal partial class Examples {
        public static async Task FindEventsTargetedBasic() {

            await foreach (var foundObject in SearchEvents.FindEventsByNamedEvents([
                NamedEvents.OSCrash,
                NamedEvents.OSStartup,
                NamedEvents.OSShutdown,
                NamedEvents.OSUncleanShutdown,
                NamedEvents.OSStartupSecurity,
                NamedEvents.OSCrashOnAuditFailRecovery,
                NamedEvents.OSBugCheck])) {

                Console.WriteLine("Event ID: {0}", foundObject.EventID + ", " + foundObject.Type + " " + foundObject.GatheredFrom);
                Console.WriteLine("Type: " + foundObject.Type + ", " + foundObject.EventID + " " + foundObject.EventID + " " + foundObject.GatheredFrom);

                if (foundObject is OSCrash osCrash) {
                    //Display the properties of the ADComputerChangeDetailed object
                    Console.WriteLine("[*] Computer: " + osCrash.Computer);
                    Console.WriteLine("[*] Who: " + osCrash.Who);
                    Console.WriteLine("[*] When: " + osCrash.When);
                } else if (foundObject is OSStartup osStartup) {
                    Console.WriteLine("[*] Computer: " + osStartup.Computer);
                    Console.WriteLine("[*] Action: " + osStartup.Action);
                    Console.WriteLine("[*] When: " + osStartup.When);
                } else if (foundObject is OSShutdown osShutdown) {
                    Console.WriteLine("[*] Computer: " + osShutdown.Computer);
                    Console.WriteLine("[*] Action: " + osShutdown.Action);
                    Console.WriteLine("[*] When: " + osShutdown.When);
                } else if (foundObject is OSUncleanShutdown unclean) {
                    Console.WriteLine("[*] Computer: " + unclean.Computer);
                    Console.WriteLine("[*] Action: " + unclean.Action);
                    Console.WriteLine("[*] When: " + unclean.When);
                } else if (foundObject is OSStartupSecurity startSec) {
                    Console.WriteLine("[*] Computer: " + startSec.Computer);
                    Console.WriteLine("[*] Action: " + startSec.Action);
                    Console.WriteLine("[*] When: " + startSec.When);
                } else if (foundObject is OSCrashOnAuditFailRecovery recovery) {
                    Console.WriteLine("[*] Computer: " + recovery.Computer);
                    Console.WriteLine("[*] Action: " + recovery.Action);
                    Console.WriteLine("[*] When: " + recovery.When);
                } else if (foundObject is OSBugCheck bugCheck) {
                    Console.WriteLine("[*] Computer: " + bugCheck.Computer);
                    Console.WriteLine("[*] Bugcheck: " + bugCheck.BugCheckCode);
                    Console.WriteLine("[*] When: " + bugCheck.When);
                }
            }
        }

        public static async Task FindEventsTargetedPerType() {
            List<string?> MachineName = new List<string?> { "AD1", "AD2", "AD0" };

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
