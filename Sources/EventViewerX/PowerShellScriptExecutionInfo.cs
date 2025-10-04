using System.Diagnostics.Eventing.Reader;

namespace EventViewerX;

/// <summary>
/// Represents details of a PowerShell engine start event.
/// </summary>
public class PowerShellScriptExecutionInfo {
    private static int _executionCount;

    /// <summary>
    /// Gets the sequential index of this execution.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Resets internal state used to track executions.
    /// </summary>
    public static void ResetState() {
        Interlocked.Exchange(ref _executionCount, 0);
    }

    /// <summary>
    /// Underlying event record containing script execution details.
    /// </summary>
    public EventRecord EventRecord { get; }

    /// <summary>
    /// Parsed data values extracted from the event.
    /// </summary>
    public IDictionary<string, string?> Data { get; }

    internal PowerShellScriptExecutionInfo(EventRecord record, IDictionary<string, string?> data) {
        EventRecord = record;
        Data = data;
        Index = Interlocked.Increment(ref _executionCount);
    }
}
