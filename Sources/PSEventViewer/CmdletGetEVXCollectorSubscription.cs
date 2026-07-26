namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Returns normalized Windows Event Collector subscription configuration.</para>
/// <para type="description">Reads local or remote WEC subscription inventory and returns detached snapshots with normalized XML details and query definitions. Remote access uses the caller's Windows identity.</para>
/// </summary>
/// <example>
///   <summary>List enabled local subscriptions</summary>
///   <code>Get-EVXCollectorSubscription -EnabledOnly</code>
///   <para>Returns only enabled subscriptions from the local collector.</para>
/// </example>
/// <example>
///   <summary>Find subscriptions on a remote collector</summary>
///   <code>Get-EVXCollectorSubscription -Name '*Domain Controllers*' -MachineName WEC01</code>
///   <para>Uses Remote Registry access under the current Windows identity and applies wildcard matching to detached snapshots.</para>
/// </example>
[Cmdlet(VerbsCommon.Get, "EVXCollectorSubscription")]
[OutputType(typeof(CollectorSubscriptionSnapshot))]
public sealed class CmdletGetEVXCollectorSubscription : AsyncPSCmdlet {
    /// <summary>Subscription names or wildcard patterns.</summary>
    [Parameter(Position = 0)]
    public string[] Name { get; set; } = new[] { "*" };

    /// <summary>Collector computers. Omit for the local computer.</summary>
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("ComputerName", "ServerName")]
    public string[] MachineName { get; set; } = Array.Empty<string>();

    /// <summary>Returns only enabled subscriptions.</summary>
    [Parameter]
    public SwitchParameter EnabledOnly { get; set; }

    /// <inheritdoc />
    protected override Task ProcessRecordAsync() {
        WildcardPattern[] patterns = Name
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static value => new WildcardPattern(
                value,
                WildcardOptions.IgnoreCase |
                WildcardOptions.CultureInvariant))
            .ToArray();
        if (patterns.Length == 0) {
            throw new PSArgumentException(
                "Name requires at least one non-empty wildcard pattern.");
        }

        string?[] machines = MachineName.Length == 0
            ? new string?[] { null }
            : EventLogTarget
                .NormalizeMachineNames(MachineName)
                .ToArray();
        foreach (string? machineName in machines) {
            CancelToken.ThrowIfCancellationRequested();
            IReadOnlyList<CollectorSubscriptionSnapshot> snapshots =
                CollectorSubscriptionManager
                    .GetCollectorSubscriptionSnapshots(
                        machineName,
                        enabledOnly: EnabledOnly.IsPresent);
            foreach (CollectorSubscriptionSnapshot snapshot in snapshots) {
                CancelToken.ThrowIfCancellationRequested();
                if (patterns.Any(pattern =>
                        pattern.IsMatch(
                            snapshot.SubscriptionName))) {
                    WriteObject(snapshot);
                }
            }
        }
        return Task.CompletedTask;
    }
}
