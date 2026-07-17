using EventViewerX;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PSEventViewer;

/// <summary>Provides shared parameters and completion reporting for PowerShell script-log cmdlets.</summary>
public abstract class PowerShellScriptQueryCmdletBase : AsyncPSCmdlet {
    /// <summary>PowerShell edition whose operational log should be queried.</summary>
    [Parameter(Mandatory = true, Position = 0)]
    public PowerShellEdition Type { get; set; }

    /// <summary>Computer names to query. When omitted, the local machine is used.</summary>
    [Alias("ComputerName")]
    [Parameter]
    public string?[]? MachineName { get; set; }

    /// <summary>Exported EVTX file to query instead of a live operational log.</summary>
    [Parameter]
    public string? EventLogPath { get; set; }

    /// <summary>Only reads events logged after this date.</summary>
    [Parameter]
    public DateTime? DateFrom { get; set; }

    /// <summary>Only reads events logged before this date.</summary>
    [Parameter]
    public DateTime? DateTo { get; set; }

    /// <summary>Maximum native records to scan per computer. Zero scans the complete query.</summary>
    [Parameter]
    [ValidateRange(0, int.MaxValue)]
    public int MaxEventsScanned { get; set; }

    /// <summary>Emits a machine-readable completion record after each computer.</summary>
    [Parameter]
    public SwitchParameter IncludeQueryInfo { get; set; }

    /// <summary>Writes incomplete-query diagnostics and optionally emits the completion record.</summary>
    protected void WriteQueryCompletion(PowerShellScriptQueryExecutionInfo queryInfo) {
        if (queryInfo.MayBeIncomplete) {
            string target = !string.IsNullOrWhiteSpace(queryInfo.EventLogPath)
                ? $"'{queryInfo.EventLogPath}'"
                : string.IsNullOrWhiteSpace(queryInfo.MachineName) ? "the local computer" : queryInfo.MachineName;
            WriteWarning(
                $"PowerShell script results from {target} may be incomplete: " +
                $"scanLimitReached={queryInfo.ScanLimitReached}, " +
                $"outputLimitReached={queryInfo.OutputLimitReached}, " +
                $"evictedIncompleteScripts={queryInfo.EvictedIncompleteScripts}, " +
                $"invalidFragmentMetadataEvents={queryInfo.InvalidFragmentMetadataEvents}, " +
                $"incompleteScriptsReturned={queryInfo.IncompleteScriptsReturned}, " +
                $"failureKind={queryInfo.FailureKind}, " +
                $"failureMessage='{queryInfo.FailureMessage}'.");
        }
        if (IncludeQueryInfo.IsPresent) {
            WriteObject(queryInfo);
        }
    }

    /// <summary>Moves a script-log iterator while isolating only expected failures from the current remote target.</summary>
    protected static bool TryMoveNextRemote<T>(
        IEnumerator<T> enumerator,
        string? machine,
        PowerShellScriptQueryExecutionInfo queryInfo) {

        try {
            return enumerator.MoveNext();
        } catch (Exception ex) when (EventLogRemoteQueryFailureClassifier.TryClassify(machine, ex, out EventLogRemoteQueryFailureKind failureKind)) {
            queryInfo.RecordFailure(failureKind, ex.Message);
            return false;
        }
    }
}
