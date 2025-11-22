namespace EventViewerX;

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
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int category, int eventId, string? machineName, params string[]? replacementStrings) {
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
    public static void WriteEvent(string source, string log, string message, EventLogEntryType type, int eventId, string? machineName, params string[]? replacementStrings) {
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


    /// <summary>
    /// Creates the log source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="log">The log.</param>
    /// <param name="machineName">Name of the machine.</param>
    /// <returns><c>true</c> when source exists or is created.</returns>
    public static bool CreateLogSource(string source, string log, string? machineName = null) {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(log)) {
            return false;
        }

        string key = BuildSourceKey(source, log, machineName);

        try {
            bool exists = SourceExistsSafe(source, log, machineName);
            if (!exists) {
                if (string.IsNullOrEmpty(machineName) && !HasLocalAdmin()) {
                    if (_sourceDenied.TryAdd(key, true)) {
                        LoggingMessages.Logger.WriteWarning($"Insufficient privileges to create event source '{source}' for log '{log}'. Run as administrator to create it.");
                    }
                    return false;
                }

                LoggingMessages.Logger.WriteVerbose(string.IsNullOrEmpty(machineName)
                    ? $"Creating event source {source} for log {log}."
                    : $"Creating event source {source} for log {log} on machine {machineName}.");

                var data = new EventSourceCreationData(source, log);
                if (!string.IsNullOrEmpty(machineName)) {
                    data.MachineName = machineName;
                }

                EventLog.CreateEventSource(data);
            }

            _sourceCache[key] = true;
            _sourceDenied.TryRemove(key, out _);
            return true;
        } catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 183) {
            _sourceCache[key] = true;
            _sourceDenied.TryRemove(key, out _);
            return true;
        } catch (System.Security.SecurityException ex) {
            if (_sourceDenied.TryAdd(key, true)) {
                LoggingMessages.Logger.WriteWarning($"Unable to create event source '{source}' for log '{log}': {ex.Message}");
            }
        } catch (UnauthorizedAccessException ex) {
            if (_sourceDenied.TryAdd(key, true)) {
                LoggingMessages.Logger.WriteWarning($"Unable to create event source '{source}' for log '{log}': {ex.Message}");
            }
        } catch (System.ArgumentException ex) {
            LoggingMessages.Logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
        } catch (Exception ex) {
            LoggingMessages.Logger.WriteWarning("Couldn't create event log. Error: " + ex.Message);
        }

        return false;
    }
}
