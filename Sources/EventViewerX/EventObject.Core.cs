using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Principal;

namespace EventViewerX;

/// <summary>
/// Managed snapshot of a Windows event record.
/// </summary>
/// <remarks>
/// The constructor takes ownership of the supplied <see cref="EventRecord"/> and disposes it after
/// copying the requested data. This keeps large and long-running queries from retaining native event handles.
/// </remarks>
public partial class EventObject {
    private readonly string _message;

    /// <summary>Time and date when the event was created.</summary>
    public DateTime TimeCreated { get; }

    /// <summary>Event identifier.</summary>
    public int Id { get; }

    /// <summary>Record identifier.</summary>
    public long? RecordId { get; }

    /// <summary>Log name reported by the event.</summary>
    public string LogName { get; }

    /// <summary>Log name that contained the event.</summary>
    public string ContainerLog { get; set; } = string.Empty;

    /// <summary>Computer that created the event.</summary>
    public string ComputerName => MachineName;

    /// <summary>Display-friendly event level.</summary>
    public string LevelDisplayName { get; }

    /// <summary>Provider that generated the event.</summary>
    public string ProviderName { get; }

    /// <summary>Additional event qualifiers, when present.</summary>
    public string? Qualifiers { get; }

    /// <summary>Event opcode.</summary>
    public short? Opcode { get; }

    /// <summary>Provider identifier.</summary>
    public Guid? ProviderId { get; }

    /// <summary>Related activity identifier.</summary>
    public Guid? RelatedActivityId { get; }

    /// <summary>Activity identifier.</summary>
    public Guid? ActivityId { get; }

    /// <summary>Security identifier associated with the event.</summary>
    public SecurityIdentifier? UserId { get; }

    /// <summary>Bookmark that can be used to resume a query.</summary>
    public EventBookmark? Bookmark { get; }

    /// <summary>Provider-formatted event message, when requested by <see cref="ReadMode"/>.</summary>
    public string Message => _message;

    /// <summary>Message split into CRLF/LF-delimited lines.</summary>
    public IReadOnlyList<string> MessageLines { get; private set; } = Array.Empty<string>();

    /// <summary>Display name of the task.</summary>
    public string TaskDisplayName { get; }

    /// <summary>Display name of the opcode.</summary>
    public string OpcodeDisplayName { get; }

    /// <summary>Keyword display names associated with the event.</summary>
    public IEnumerable<string> KeywordsDisplayNames { get; }

    /// <summary>Keyword flags associated with the event.</summary>
    public long? Keywords { get; }

    /// <summary>Numeric event level.</summary>
    public byte? Level { get; }

    /// <summary>Event version.</summary>
    public byte? Version { get; }

    /// <summary>Task identifier.</summary>
    public int? Task { get; }

    /// <summary>Process identifier.</summary>
    public int? ProcessId { get; }

    /// <summary>Thread identifier.</summary>
    public int? ThreadId { get; }

    /// <summary>Computer that created the event.</summary>
    public string MachineName { get; }

    /// <summary>Event property values copied from the native record.</summary>
    public IList<EventProperty> Properties { get; }

    /// <summary>Structured event data parsed from XML.</summary>
    public Dictionary<string, string> Data { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>NIC-related identifiers extracted from structured event data.</summary>
    public List<string> NicIdentifiers { get; private set; } = new();

    /// <summary>Key/value pairs parsed from the formatted message.</summary>
    public Dictionary<string, string> MessageData { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>First non-empty line of the formatted message.</summary>
    public string MessageSubject { get; set; } = string.Empty;

    /// <summary>Binary attachments extracted from structured event data.</summary>
    public IReadOnlyList<byte[]> Attachments { get; private set; } = Array.Empty<byte[]>();

    /// <summary>Raw event XML, when requested by <see cref="ReadMode"/>.</summary>
    public string XMLData { get; set; } = string.Empty;

    /// <summary>Machine name or file path from which the event was queried.</summary>
    public string QueriedMachine { get; set; } = string.Empty;

    /// <summary>Computer name or file path from which the event was gathered.</summary>
    public string GatheredFrom { get; set; } = string.Empty;

    /// <summary>Log name from which the event was gathered.</summary>
    public string GatheredLogName { get; set; } = string.Empty;

    /// <summary>Amount of provider data materialized for this snapshot.</summary>
    public EventReadMode ReadMode { get; }

    /// <summary>
    /// Creates an event snapshot and releases the supplied native event record.
    /// </summary>
    /// <param name="eventRecord">Event record whose ownership is transferred to this constructor.</param>
    /// <param name="queriedMachine">Computer name or file path from which the event was read.</param>
    /// <param name="readMode">Amount of provider data to materialize.</param>
    public EventObject(EventRecord eventRecord, string queriedMachine, EventReadMode readMode = EventReadMode.Full) {
        if (eventRecord == null) {
            throw new ArgumentNullException(nameof(eventRecord));
        }

        ReadMode = readMode;
        QueriedMachine = queriedMachine ?? string.Empty;
        _message = string.Empty;

        try {
            TimeCreated = eventRecord.TimeCreated ?? DateTime.MinValue;
            Id = eventRecord.Id;
            RecordId = eventRecord.RecordId;
            LogName = eventRecord.LogName ?? string.Empty;
            MachineName = eventRecord.MachineName ?? string.Empty;
            ProviderName = eventRecord.ProviderName ?? string.Empty;
            Qualifiers = eventRecord.Qualifiers?.ToString();
            Opcode = eventRecord.Opcode;
            ProviderId = eventRecord.ProviderId;
            RelatedActivityId = eventRecord.RelatedActivityId;
            ActivityId = eventRecord.ActivityId;
            UserId = eventRecord.UserId;
            Bookmark = eventRecord.Bookmark;
            Keywords = eventRecord.Keywords;
            Level = eventRecord.Level;
            Version = eventRecord.Version;
            Task = eventRecord.Task;
            ProcessId = eventRecord.ProcessId;
            ThreadId = eventRecord.ThreadId;
            Properties = readMode == EventReadMode.StructuredData || readMode == EventReadMode.Full
                ? SnapshotProperties(eventRecord)
                : Array.Empty<EventProperty>();
            bool includeProviderDisplayNames = readMode == EventReadMode.Message || readMode == EventReadMode.Full;
            TaskDisplayName = includeProviderDisplayNames
                ? SafeReadDisplayName(() => eventRecord.TaskDisplayName)
                : string.Empty;
            OpcodeDisplayName = includeProviderDisplayNames
                ? SafeReadDisplayName(() => eventRecord.OpcodeDisplayName)
                : string.Empty;
            KeywordsDisplayNames = includeProviderDisplayNames
                ? SafeReadKeywordDisplayNames(eventRecord)
                : Array.Empty<string>();

            string levelDisplayName = includeProviderDisplayNames
                ? SafeReadDisplayName(() => eventRecord.LevelDisplayName)
                : string.Empty;
            LevelDisplayName = string.IsNullOrEmpty(levelDisplayName)
                ? LevelToDisplayName(Level)
                : levelDisplayName;

            ContainerLog = eventRecord is EventLogRecord eventLogRecord
                ? eventLogRecord.ContainerLog ?? string.Empty
                : LogName;
            GatheredFrom = string.IsNullOrEmpty(QueriedMachine) ? Environment.MachineName : QueriedMachine;
            GatheredLogName = LogName;

            if (readMode == EventReadMode.Message || readMode == EventReadMode.Full) {
                _message = SafeFormatDescription(eventRecord);
                MessageLines = SplitMessageLines(_message);
                MessageData = ParseMessage<Dictionary<string, string>>(_message);
            }

            if (readMode == EventReadMode.StructuredData || readMode == EventReadMode.Full) {
                XMLData = SafeToXml(eventRecord);
                ParseXmlPayload(XMLData, out Dictionary<string, string> data, out List<byte[]> attachments);
                Data = data;
                NicIdentifiers = ExtractNicIdentifiers();
                Attachments = attachments;
            }
        } finally {
            eventRecord.Dispose();
        }
    }

    private static IList<EventProperty> SnapshotProperties(EventRecord eventRecord) {
        try {
            return eventRecord.Properties?.ToArray() ?? Array.Empty<EventProperty>();
        } catch (EventLogException ex) {
            Settings._logger.WriteVerbose("Failed to snapshot event properties. ({0})", ex.Message);
            return Array.Empty<EventProperty>();
        }
    }

    private static string SafeReadDisplayName(Func<string?> readValue) {
        try {
            return readValue() ?? string.Empty;
        } catch (EventLogException) {
            return string.Empty;
        }
    }

    private static IEnumerable<string> SafeReadKeywordDisplayNames(EventRecord eventRecord) {
        try {
            return eventRecord.KeywordsDisplayNames?.ToArray() ?? Array.Empty<string>();
        } catch (EventLogException) {
            return Array.Empty<string>();
        }
    }

    private static string SafeFormatDescription(EventRecord eventRecord) {
        try {
            return eventRecord.FormatDescription() ?? string.Empty;
        } catch (EventLogNotFoundException ex) {
            Settings._logger.WriteWarning("Failed to format event description due to missing provider metadata. ({0})", ex.Message);
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

    private static string LevelToDisplayName(byte? level) {
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
