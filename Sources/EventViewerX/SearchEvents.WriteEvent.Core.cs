namespace EventViewerX;

public partial class SearchEvents : Settings {
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int category, int eventId, string machineName, byte[] rawData, params string[] replacementStrings) {
        if (category is < short.MinValue or > short.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(category), category, $"Category must fit into Int16 range ({short.MinValue} - {short.MaxValue}).");
        }

        var sourceExists = CreateLogSource(source, log, machineName);
        if (sourceExists) {
            EventInstance eventInstance = new EventInstance(eventId, category, type);

            using (EventLog eventLog = new EventLog(log)) {
                eventLog.Source = source;
                if (!String.IsNullOrEmpty(machineName)) {
                    eventLog.MachineName = machineName;
                }

                try {
                    if (rawData == null && (replacementStrings == null || replacementStrings.Length == 0)) {
                        LoggingMessages.Logger.WriteVerbose($"Writing event '{eventId}' of type '{type}' of category '{category}' to EventLog: '{message}'");
                        eventLog.WriteEntry(message, type, eventId, (short)category);
                    } else {
                        var joinedMessage = replacementStrings == null
                            ? new[] { message }
                            : replacementStrings.Prepend(message).ToArray();
                        LoggingMessages.Logger.WriteVerbose($"Writing event '{eventId}' of type '{type}' of category '{category}' to EventLog: '{joinedMessage}'");
                        eventLog.WriteEvent(eventInstance, rawData, joinedMessage);
                    }
                } catch (System.Security.SecurityException ex) {
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                } catch (System.ArgumentException ex) {
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                } catch (Exception ex) {
                    LoggingMessages.Logger.WriteWarning("Couldn't write to event log. Error: " + ex.Message);
                }
            }
        } else {
            LoggingMessages.Logger.WriteWarning($"Event source {source} does not exist. Write to event log unsuccessful.");
        }
    }

    public static bool CreateLogSource(string source, string log, string machineName = null) {
        try {
            if (string.IsNullOrEmpty(machineName)) {
                if (!EventLog.SourceExists(source)) {
                    LoggingMessages.Logger.WriteVerbose($"Creating event source {source}.");
                    EventLog.CreateEventSource(source, log);
                }
            } else {
                if (!EventLog.SourceExists(source, machineName)) {
                    LoggingMessages.Logger.WriteVerbose($"Creating event source {source} on machine {machineName}.");
                    EventLog.CreateEventSource(new EventSourceCreationData(source, log) { MachineName = machineName });
                }
            }
            return true;
        } catch (System.Security.SecurityException ex) {
            LoggingMessages.Logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
        } catch (System.ArgumentException ex) {
            LoggingMessages.Logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
        } catch (Exception ex) {
            LoggingMessages.Logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
        }

        return false;
    }
}
