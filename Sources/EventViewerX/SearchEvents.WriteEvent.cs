﻿namespace EventViewerX;

public partial class SearchEvents : Settings {

    /// <summary>
    /// Writes to EventLog, with optional replacement strings and no raw data.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="log">The log.</param>
    /// <param name="message">The message.</param>
    /// <param name="type">The type.</param>
    /// <param name="category">The category.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <param name="replacementStrings">The replacement strings.</param>
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int category, int eventId, string machineName, params string[] replacementStrings) {
        WriteEvent(source, log, message, type, category, eventId, machineName, null, replacementStrings);
    }

    /// <summary>
    /// Writes to EventLog, with optional replacement strings and no raw data, and with default category of 0.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="log"></param>
    /// <param name="message"></param>
    /// <param name="type"></param>
    /// <param name="eventId"></param>
    /// <param name="machineName"></param>
    /// <param name="replacementStrings"></param>
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int eventId, string machineName, params string[] replacementStrings) {
        WriteEvent(source, log, message, type, 0, eventId, machineName, null, replacementStrings);
    }

    /// <summary>
    /// Writes to EventLog, with replacement strings and/or raw data.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="log">The log.</param>
    /// <param name="message">The message.</param>
    /// <param name="type">The type.</param>
    /// <param name="category">The category.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <param name="rawData">The raw data.</param>
    /// <param name="replacementStrings">The replacement strings.</param>
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int category, int eventId, string machineName, byte[] rawData, params string[] replacementStrings) {
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
                if (rawData == null && (replacementStrings == null || replacementStrings.Length == 0)) {
                    // If rawData and replacementStrings are not provided, write a simple message to the event log.
                    eventLog.WriteEntry(message, type, eventId, (short)category);
                } else {
                    var joinedMessage = replacementStrings.Prepend(message).ToArray();
                    // If rawData and/or replacementStrings are provided, write them to the event log.
                    eventLog.WriteEvent(eventInstance, rawData, joinedMessage);
                }
            }

        }
    }

    /// <summary>
    /// Creates the log source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="log">The log.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <returns></returns>
    public static bool CreateLogSource(string source, string log, string machineName = null) {
        try {
            if (string.IsNullOrEmpty(machineName)) {
                if (!EventLog.SourceExists(source)) {
                    EventLog.CreateEventSource(source, log);
                }
            } else {
                if (!EventLog.SourceExists(source, machineName)) {
                    EventLog.CreateEventSource(new EventSourceCreationData(source, log) { MachineName = machineName });
                }
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