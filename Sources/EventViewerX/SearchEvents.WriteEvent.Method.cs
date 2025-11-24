using System.Diagnostics;
using System.Linq;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    /// <summary>
    /// Writes an event to the specified Windows Event Log, creating the source if needed.
    /// </summary>
    /// <param name="source">Event source name; created if missing.</param>
    /// <param name="log">Target log (e.g., Application, System).</param>
    /// <param name="message">Message text.</param>
    /// <param name="type">Entry type (Information, Warning, Error).</param>
    /// <param name="category">Category (Int16 range) associated with the entry.</param>
    /// <param name="eventId">Event identifier.</param>
    /// <param name="machineName">Optional remote machine; null for local.</param>
    /// <param name="rawData">Optional raw data payload.</param>
    /// <param name="replacementStrings">Optional replacement strings inserted into the message.</param>
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int category, int eventId, string? machineName, byte[]? rawData, params string[]? replacementStrings) {
        if (category is < short.MinValue or > short.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(category), category, $"Category must fit into Int16 range ({short.MinValue} - {short.MaxValue}).");
        }

        // Check if the event source exists. If not, create it.
        var sourceExists = CreateLogSource(source, log, machineName);
        if (sourceExists) {
            // Create an EventInstance object for the event log entry.
            EventInstance eventInstance = new EventInstance(eventId, category, type);

            // Write an entry to the event log.
            using (EventLog eventLog = new EventLog(log)) {
                eventLog.Source = source;
                if (!String.IsNullOrEmpty(machineName)) {
                    eventLog.MachineName = machineName;
                }

                try {
                    if (rawData == null && (replacementStrings == null || replacementStrings.Length == 0)) {
                        // If rawData and replacementStrings are not provided, write a simple message to the event log.
                        LoggingMessages.Logger.WriteVerbose($"Writing event '{eventId}' of type '{type}' of category '{category}' to EventLog: '{message}'");
                        eventLog.WriteEntry(message, type, eventId, (short)category);
                    } else {
                        var joinedMessage = replacementStrings == null
                            ? new[] { message }
                            : replacementStrings.Prepend(message).ToArray();
                        // If rawData and/or replacementStrings are provided, write them to the event log.
                        LoggingMessages.Logger.WriteVerbose($"Writing event '{eventId}' of type '{type}' of category '{category}' to EventLog: '{joinedMessage}'");
                        eventLog.WriteEvent(eventInstance, rawData, joinedMessage);
                    }
                } catch (System.Security.SecurityException ex) {
                    // Handle the security exception
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                } catch (System.ArgumentException ex) {
                    // Handle argument exception
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                } catch (Exception ex) {
                    // Handle any other type of exception
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                }
            }
        } else {
            LoggingMessages.Logger.WriteWarning($"Event source {source} does not exist. Write to event log unsuccessful.");
        }
    }
}
