using System.Management.Automation;
using System.Threading.Tasks;
using EventViewerX.Reports.Live;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Builds bounded statistics from a live Windows event log.</para>
/// <para type="description">Scans event metadata without formatting messages or parsing XML and reports top event IDs, providers, levels, computers, and the observed time range.</para>
/// </summary>
/// <example>
///   <summary>Summarize recent Security events</summary>
///   <code>Get-EVXEventStatistics -LogName Security -MaxEvents 50000</code>
///   <para>Scans up to 50,000 events using the metadata-only projection.</para>
/// </example>
/// <example>
///   <summary>Summarize a remote domain controller</summary>
///   <code>Get-EVXEventStatistics -LogName System -MachineName AD1.ad.evotec.xyz -MaxEvents 10000</code>
///   <para>Returns a typed statistics result or a PowerShell error with the underlying failure category.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "EVXEventStatistics")]
[Alias("Get-EVXStats")]
[OutputType(typeof(LiveStatsQueryResult))]
public sealed class CmdletGetEVXEventStatistics : AsyncPSCmdlet {
    /// <summary>Event log channel to scan.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string LogName { get; set; } = string.Empty;

    /// <summary>Optional target computer. The local computer is used by default.</summary>
    [Parameter]
    [Alias("ComputerName", "ServerName")]
    public string? MachineName { get; set; }

    /// <summary>Optional XPath filter. All events are selected by default.</summary>
    [Parameter]
    public string? XPath { get; set; }

    /// <summary>Maximum number of events to scan. Zero removes the limit.</summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEvents { get; set; } = 10000;

    /// <summary>Reads from oldest to newest.</summary>
    [Parameter]
    public SwitchParameter Oldest { get; set; }

    /// <summary>Optional inclusive lower time bound.</summary>
    [Parameter]
    public DateTime? StartTime { get; set; }

    /// <summary>Optional inclusive upper time bound.</summary>
    [Parameter]
    public DateTime? EndTime { get; set; }

    /// <summary>Number of entries retained for each top-N group.</summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int Top { get; set; } = 10;

    /// <summary>Session and per-read timeout in milliseconds.</summary>
    [Parameter]
    [ValidateRange(1, int.MaxValue)]
    public int SessionTimeoutMs { get; set; } = 5000;

    /// <inheritdoc />
    protected override Task ProcessRecordAsync() {
        var request = new LiveStatsQueryRequest {
            LogName = LogName,
            MachineName = MachineName,
            XPath = XPath,
            MaxEventsScanned = MaxEvents,
            OldestFirst = Oldest,
            StartTimeUtc = StartTime?.ToUniversalTime(),
            EndTimeUtc = EndTime?.ToUniversalTime(),
            TopEventIds = Top,
            TopProviders = Top,
            TopLevels = Top,
            TopComputers = Top,
            SessionTimeoutMs = SessionTimeoutMs
        };

        if (LiveStatsQueryExecutor.TryBuild(request, out LiveStatsQueryResult result, out LiveStatsQueryFailure? failure, CancelToken)) {
            WriteObject(result);
            return Task.CompletedTask;
        }

        LiveStatsQueryFailure queryFailure = failure ?? new LiveStatsQueryFailure {
            Kind = LiveStatsQueryFailureKind.Exception,
            Message = "The event statistics query failed without diagnostic details."
        };
        ErrorCategory category = queryFailure.Kind switch {
            LiveStatsQueryFailureKind.InvalidArgument => ErrorCategory.InvalidArgument,
            LiveStatsQueryFailureKind.InvalidQuery => ErrorCategory.InvalidArgument,
            LiveStatsQueryFailureKind.LogNotFound => ErrorCategory.ObjectNotFound,
            LiveStatsQueryFailureKind.AccessDenied => ErrorCategory.PermissionDenied,
            LiveStatsQueryFailureKind.Timeout => ErrorCategory.OperationTimeout,
            LiveStatsQueryFailureKind.HostUnavailable => ErrorCategory.ResourceUnavailable,
            _ => ErrorCategory.NotSpecified
        };
        WriteError(new ErrorRecord(
            new InvalidOperationException(queryFailure.Message),
            $"EVXEventStatistics.{queryFailure.Kind}",
            category,
            MachineName ?? LogName));
        return Task.CompletedTask;
    }
}
