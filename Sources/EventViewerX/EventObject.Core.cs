using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;

namespace EventViewerX {
    /// <summary>
    /// Detailed representation of a Windows event record.
    /// </summary>
    public partial class EventObject {
        /// <summary>
    /// Time and date when the event was created
    /// </summary>
    public DateTime TimeCreated => _eventRecord.TimeCreated ?? DateTime.MinValue;

        /// <summary>
        /// Event ID
        /// </summary>
        public int Id => _eventRecord.Id;

        /// <summary>
        /// Record ID
        /// </summary>
        public long? RecordId => _eventRecord.RecordId;

        /// <summary>
        /// Log name where the event was logged
        /// </summary>
        public string LogName => _eventRecord.LogName;

    /// <summary>
    /// Log name where the event was queried from
    /// </summary>
    public string ContainerLog { get; set; } = string.Empty;

        /// <summary>
    /// Computer name where the event was logged
    /// </summary>
    public string ComputerName => _eventRecord.MachineName;

        /// <summary>
        /// Human readable event level name.
        /// Falls back to numeric level when the provider omits a display string (e.g., synthetic or test events).
        /// </summary>
        private readonly string _levelDisplayName;

    /// <summary>
    /// Display-friendly level name resolved from the provider metadata; falls back to the numeric level when unavailable.
    /// </summary>
    public string LevelDisplayName => _levelDisplayName;

        /// <summary>
        /// Provider that generated the event.
        /// </summary>
        public string ProviderName => _eventRecord.ProviderName;

        /// <summary>
        /// Additional event qualifiers if present.
        /// </summary>
    public string? Qualifiers => _eventRecord.Qualifiers?.ToString();

        /// <summary>
        /// Opcode of the event record.
        /// </summary>
        public short? Opcode => _eventRecord.Opcode;

        /// <summary>
        /// Identifier of the event provider.
        /// </summary>
        public Guid? ProviderId => _eventRecord.ProviderId;

        /// <summary>
        /// Related activity identifier if any.
        /// </summary>
        public Guid? RelatedActivityId => _eventRecord.RelatedActivityId;

        /// <summary>
        /// Activity identifier for correlation.
        /// </summary>
        public Guid? ActivityId => _eventRecord.ActivityId;

        /// <summary>
    /// Security identifier associated with the event.
    /// </summary>
    public SecurityIdentifier UserId => _eventRecord.UserId;

        /// <summary>
        /// Event bookmark for resuming queries.
        /// </summary>
        public EventBookmark Bookmark => _eventRecord.Bookmark;

        /// <summary>
        /// Human-readable event message rendered by the provider when available.
        /// </summary>
        public string Message => _message ?? string.Empty;

        /// <summary>
        /// Message split into lines using CRLF/LF boundaries. Indices are stable for callers that need positional parsing.
        /// </summary>
        public IReadOnlyList<string> MessageLines { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Display name of the task.
        /// </summary>
        public string TaskDisplayName => _eventRecord.TaskDisplayName;

        /// <summary>
        /// Display name of the opcode.
        /// </summary>
        public string OpcodeDisplayName => _eventRecord.OpcodeDisplayName;

        /// <summary>
        /// Keyword display names associated with the event.
        /// </summary>
        public IEnumerable<string> KeywordsDisplayNames => _eventRecord.KeywordsDisplayNames;

        /// <summary>
        /// Keyword flags associated with the event.
        /// </summary>
        public long? Keywords => _eventRecord.Keywords;

        /// <summary>
        /// Numeric level of the event.
        /// </summary>
        public byte? Level => _eventRecord.Level;

        /// <summary>
        /// Version of the event record.
        /// </summary>
        public byte? Version => _eventRecord.Version;

        /// <summary>
        /// Task identifier if available.
        /// </summary>
        public int? Task => _eventRecord.Task;

        /// <summary>
        /// Identifier of the process that logged the event.
        /// </summary>
        public int? ProcessId => _eventRecord.ProcessId;

        /// <summary>
        /// Identifier of the thread that logged the event.
        /// </summary>
        public int? ThreadId => _eventRecord.ThreadId;

        /// <summary>
        /// Computer name where the event was logged
        /// </summary>
        public string MachineName => _eventRecord.MachineName;

        /// <summary>
        /// Properties available in the event record
        /// </summary>
        public IList<EventProperty> Properties => _eventRecord.Properties;

        /// <summary>
    /// Data available in XML converted to a dictionary
    /// </summary>
    public Dictionary<string, string> Data { get; private set; } = new Dictionary<string, string>();

        /// <summary>
    /// NIC identifiers extracted from event data
    /// </summary>
    public List<string> NicIdentifiers { get; private set; } = new List<string>();

        /// <summary>
    /// Data available in the message converted to a dictionary
    /// </summary>
    public Dictionary<string, string> MessageData { get; private set; } = new Dictionary<string, string>();

    /// <summary>
    /// First line of the formatted message.
    /// </summary>
    public string MessageSubject { get; set; } = string.Empty;

    /// <summary>
    /// Attachments extracted from the event if present
    /// </summary>
    public IReadOnlyList<byte[]> Attachments { get; private set; } = Array.Empty<byte[]>();

    /// <summary>
    /// Data available in XML format
    /// </summary>
    public string XMLData { get; set; } = string.Empty;

    /// <summary>
    /// Machine from which the event was queried.
    /// </summary>
    public string QueriedMachine { get; set; } = string.Empty;

    /// <summary>
    /// Source from which the event was gathered (computer name or file path).
    /// </summary>
    public string GatheredFrom { get; set; } = string.Empty;

    /// <summary>
    /// Log name that contained the event.
    /// </summary>
    public string GatheredLogName { get; set; } = string.Empty;

        /// <summary>
        /// Original event record
        /// </summary>
        public readonly EventRecord _eventRecord;

        private readonly string _message;

        /// <summary>
        /// Creates a rich event wrapper around a raw <see cref="EventRecord"/> and annotates it with the queried machine name.
        /// </summary>
        /// <param name="eventRecord">Underlying Windows event record.</param>
        /// <param name="queriedMachine">Computer name or file path the event was read from.</param>
        public EventObject(EventRecord eventRecord, string queriedMachine) {
            QueriedMachine = queriedMachine;
            _eventRecord = eventRecord;

            try {
                _levelDisplayName = eventRecord.LevelDisplayName;
            } catch (EventLogNotFoundException) {
                // Some offline .evtx files reference providers that are not installed on the host.
                // When the metadata DLL is missing, EventLogReader throws while resolving the display name.
                _levelDisplayName = string.Empty;
            } catch (EventLogException) {
                _levelDisplayName = string.Empty;
            }

            if (string.IsNullOrEmpty(_levelDisplayName)) {
                _levelDisplayName = LevelToDisplayName(eventRecord.Level);
            }

            if (_eventRecord is EventLogRecord eventLogRecord) {
                ContainerLog = eventLogRecord.ContainerLog;
            } else {
                ContainerLog = eventRecord.LogName ?? string.Empty;
            }

            if (queriedMachine != null && (queriedMachine.EndsWith(".evtx", StringComparison.OrdinalIgnoreCase) || queriedMachine.Contains("\\"))) {
                GatheredFrom = queriedMachine;
            } else {
                GatheredFrom = queriedMachine ?? Environment.MachineName;
            }
            GatheredLogName = eventRecord.LogName ?? string.Empty;

            _message = SafeFormatDescription(eventRecord);
            MessageLines = SplitMessageLines(_message);
            XMLData = SafeToXml(eventRecord);
            Data = ParseXML<Dictionary<string, string>>(XMLData);
            MessageData = ParseMessage<Dictionary<string, string>>(_message);
            NicIdentifiers = ExtractNicIdentifiers();
            Attachments = ExtractAttachments(XMLData);
        }

        private static string SafeFormatDescription(EventRecord eventRecord) {
            try {
                return eventRecord.FormatDescription() ?? string.Empty;
            } catch (EventLogNotFoundException ex) {
                Settings._logger.WriteWarning("Failed to format event description due to missing provider metadata. ({0})",
                    ex.Message);
                return string.Empty;
            } catch (EventLogException ex) {
                Settings._logger.WriteWarning("Failed to format event description. ({0})", ex.Message);
                return string.Empty;
            } catch (Exception ex) {
                Settings._logger.WriteWarning("Unexpected error while formatting event description. ({0})", ex.Message);
                return string.Empty;
            }
        }

        private static string SafeToXml(EventRecord eventRecord) {
            try {
                return eventRecord.ToXml() ?? string.Empty;
            } catch (EventLogException ex) {
                Settings._logger.WriteWarning("Failed to read event XML payload. ({0})", ex.Message);
                return string.Empty;
            } catch (Exception ex) {
                Settings._logger.WriteWarning("Unexpected error while reading event XML payload. ({0})", ex.Message);
                return string.Empty;
            }
        }

        private static string LevelToDisplayName(byte? level)
        {
            return level switch {
                1 => "Critical",
                2 => "Error",
                3 => "Warning",
                4 => "Information",
                5 => "Verbose",
                _ => level?.ToString() ?? string.Empty
            };
        }
    }
}

