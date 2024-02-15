using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PSEventViewer {
    public partial class SearchEvents : Settings {
        public static void WriteToEventLog(string source, string log, string message, EventLogEntryType type, int eventId, string machineName, params string[] replacementStrings) {
            WriteToEventLog(source, log, message, type, eventId, machineName, null, replacementStrings);
        }

        public static void WriteToEventLog(string source, string log, string message, EventLogEntryType type, int eventId, string machineName, byte[] rawData, params string[] replacementStrings) {
            // Check if the event source exists. If not, create it.
            var sourceExists = SourceExistsCreate(source, log, machineName);
            if (sourceExists) {

                // Create an EventInstance object for the event log entry.
                EventInstance eventInstance = new EventInstance(eventId, 0, type);

                // Write an entry to the event log.
                using (EventLog eventLog = new EventLog(log, machineName, source)) {
                    if (rawData == null && (replacementStrings == null || replacementStrings.Length == 0)) {
                        // If rawData and replacementStrings are not provided, write a simple message to the event log.
                        eventLog.WriteEntry(message, type);
                    } else {
                        // If rawData and/or replacementStrings are provided, write them to the event log.
                        eventLog.WriteEvent(eventInstance, rawData, replacementStrings);
                    }
                }
            }
        }

        private static bool SourceExistsCreate(string source, string log, string machineName) {
            try {
                // Your code that may throw an exception
                if (!EventLog.SourceExists(source, machineName)) {
                    EventLog.CreateEventSource(new EventSourceCreationData(source, log) { MachineName = machineName });
                }
                return true;
            } catch (System.Security.SecurityException ex) {
                // Handle the security exception
                _logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
            } catch (System.ArgumentException ex) {
                // Handle argument exception
                _logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
            } catch (Exception ex) {
                // Handle any other type of exception
                _logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
            }

            return false;
        }
    }
}
