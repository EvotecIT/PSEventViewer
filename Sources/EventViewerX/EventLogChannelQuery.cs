using System;
using System.Globalization;

namespace EventViewerX;

/// <summary>Defines a low-level streaming query against a local or remote Windows event channel.</summary>
public sealed class EventLogChannelQuery {
    /// <summary>Creates a channel query.</summary>
    /// <param name="logName">Windows event channel name, for example System or Security.</param>
    public EventLogChannelQuery(string logName) {
        if (string.IsNullOrWhiteSpace(logName)) {
            throw new ArgumentException("Event log name cannot be null or empty.", nameof(logName));
        }
        LogName = logName;
    }

    /// <summary>Windows event channel name.</summary>
    public string LogName { get; }

    /// <summary>Remote computer name. A null or empty value targets the local computer.</summary>
    public string? MachineName { get; set; }

    /// <summary>XPath expression applied by the Windows event query engine.</summary>
    public string XPath { get; set; } = "*";

    /// <summary>Whether records are returned from oldest to newest.</summary>
    public bool Oldest { get; set; }

    /// <summary>Amount of event data materialized for each record.</summary>
    public EventReadMode ReadMode { get; set; } = EventReadMode.Full;

    /// <summary>
    /// Culture requested for provider messages and display names.
    /// A null value uses <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public CultureInfo? MessageCulture { get; set; }

    /// <summary>Maximum number of records returned. Zero streams every match.</summary>
    public int MaxEvents { get; set; }

    /// <summary>Maximum time for the RPC probe, worker slot, and remote session establishment.</summary>
    public int RemoteConnectionTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Maximum time without remote read progress. Zero keeps the read unbounded.
    /// </summary>
    public int RemoteReadTimeoutMilliseconds { get; set; }

    /// <summary>Maximum detached event snapshots buffered between the remote native worker and caller.</summary>
    public int BufferCapacity { get; set; } = 64;

    /// <summary>RPC endpoint mapper port probed before starting a remote native query.</summary>
    public int RpcEndpointPort { get; set; } = 135;
}
