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
    // WriteEvent and CreateLogSource methods moved to SearchEvents.WriteEvent.Core.cs
}
