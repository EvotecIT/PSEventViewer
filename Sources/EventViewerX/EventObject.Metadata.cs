using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Security.Principal;

namespace EventViewerX;

public partial class EventObject {
    private static readonly string[] MetadataPropertyPaths = {
        "Event/System/EventID",
        "Event/System/EventID/@Qualifiers",
        "Event/System/EventRecordID",
        "Event/System/TimeCreated/@SystemTime",
        "Event/System/Provider/@Name",
        "Event/System/Provider/@Guid",
        "Event/System/Computer",
        "Event/System/Channel",
        "Event/System/Level",
        "Event/System/Keywords",
        "Event/System/Task",
        "Event/System/Opcode",
        "Event/System/Execution/@ProcessID",
        "Event/System/Execution/@ThreadID",
        "Event/System/Correlation/@ActivityID",
        "Event/System/Correlation/@RelatedActivityID",
        "Event/System/Security/@UserID",
        "Event/System/Version"
    };

    internal static EventLogPropertySelector CreateMetadataPropertySelector() {
        return new EventLogPropertySelector(MetadataPropertyPaths);
    }

    internal static EventObject CreateMetadata(
        EventRecord eventRecord,
        EventLogPropertySelector selector,
        string queriedMachine,
        string containerLog) {

        if (eventRecord == null) {
            throw new ArgumentNullException(nameof(eventRecord));
        }
        if (selector == null) {
            throw new ArgumentNullException(nameof(selector));
        }
        if (eventRecord is not EventLogRecord logRecord) {
            return new EventObject(eventRecord, queriedMachine, EventReadMode.Metadata);
        }

        IList<object> values;
        try {
            values = logRecord.GetPropertyValues(selector);
        } catch (EventLogException ex) {
            Settings._logger.WriteVerbose(
                "Falling back to direct metadata projection after EventLogPropertySelector failed. ({0})",
                ex.Message);
            return new EventObject(eventRecord, queriedMachine, EventReadMode.Metadata);
        }

        try {
            return new EventObject(values, bookmark: null, queriedMachine, containerLog);
        } finally {
            eventRecord.Dispose();
        }
    }

    private EventObject(
        IList<object> values,
        EventBookmark? bookmark,
        string queriedMachine,
        string containerLog) {

        if (values == null) {
            throw new ArgumentNullException(nameof(values));
        }
        if (values.Count < MetadataPropertyPaths.Length) {
            throw new ArgumentException("The metadata property selector returned an incomplete event.", nameof(values));
        }

        ReadMode = EventReadMode.Metadata;
        QueriedMachine = queriedMachine ?? string.Empty;
        _message = string.Empty;
        Id = ToInt32(values[0]) ?? 0;
        Qualifiers = values[1]?.ToString();
        RecordId = ToInt64(values[2]);
        TimeCreated = ToDateTime(values[3]);
        ProviderName = values[4]?.ToString() ?? string.Empty;
        ProviderId = ToGuid(values[5]);
        MachineName = values[6]?.ToString() ?? string.Empty;
        LogName = values[7]?.ToString() ?? string.Empty;
        Level = ToByte(values[8]);
        Keywords = ToInt64Bits(values[9]);
        Task = ToInt32(values[10]);
        Opcode = ToInt16(values[11]);
        ProcessId = ToInt32(values[12]);
        ThreadId = ToInt32(values[13]);
        ActivityId = ToGuid(values[14]);
        RelatedActivityId = ToGuid(values[15]);
        UserId = values[16] as SecurityIdentifier;
        Version = ToByte(values[17]);
        Bookmark = bookmark;
        Properties = Array.Empty<EventProperty>();
        TaskDisplayName = string.Empty;
        OpcodeDisplayName = string.Empty;
        KeywordsDisplayNames = Array.Empty<string>();
        LevelDisplayName = LevelToDisplayName(Level);
        ContainerLog = containerLog ?? string.Empty;
        GatheredFrom = string.IsNullOrEmpty(QueriedMachine) ? Environment.MachineName : QueriedMachine;
        GatheredLogName = LogName;
    }

    private static byte? ToByte(object? value) {
        return value == null ? null : Convert.ToByte(value, CultureInfo.InvariantCulture);
    }

    private static short? ToInt16(object? value) {
        return value == null ? null : Convert.ToInt16(value, CultureInfo.InvariantCulture);
    }

    private static int? ToInt32(object? value) {
        if (value == null) {
            return null;
        }
        return value is uint unsigned
            ? unchecked((int)unsigned)
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static long? ToInt64(object? value) {
        if (value == null) {
            return null;
        }
        return value is ulong unsigned
            ? unchecked((long)unsigned)
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static long? ToInt64Bits(object? value) {
        return ToInt64(value);
    }

    private static DateTime ToDateTime(object? value) {
        if (value is DateTime dateTime) {
            return dateTime;
        }
        return value == null
            ? DateTime.MinValue
            : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    private static Guid? ToGuid(object? value) {
        if (value is Guid guid) {
            return guid;
        }
        return value != null && Guid.TryParse(value.ToString(), out Guid parsed)
            ? parsed
            : null;
    }
}