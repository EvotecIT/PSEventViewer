namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Applies a typed local WEC subscription definition or changes its enabled state.</para>
/// <para type="description">Definition input creates or updates a subscription through the Windows inbox collector utility. The state set uses the supported WEC API. Both paths verify persisted state. Definition apply is cancellable and time-bounded; failed apply is rolled back and reports explicitly when rollback cannot establish a known persisted state.</para>
/// </summary>
/// <example>
///   <summary>Disable a subscription on the current collector</summary>
///   <code>Set-EVXCollectorSubscription -Name 'Domain Controllers' -Enabled $false</code>
///   <para>Returns before and after snapshots plus whether the persisted state changed.</para>
/// </example>
/// <example>
///   <summary>Create or update a typed collector subscription</summary>
///   <code>New-EVXCollectorSubscription -Name FailedLogons -SourceComputer DC01,DC02 -LogName Security -EventId 4625 | Set-EVXCollectorSubscription</code>
///   <para>Applies the typed definition transactionally and verifies the persisted Windows configuration.</para>
/// </example>
/// <example>
///   <summary>Remove a collector subscription</summary>
///   <code>Set-EVXCollectorSubscription -Name FailedLogons -Remove</code>
///   <para>Deletes the local subscription through the inbox collector utility and verifies that it is absent.</para>
/// </example>
[Cmdlet(
    VerbsCommon.Set,
    "EVXCollectorSubscription",
    SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.High)]
[OutputType(
    typeof(CollectorSubscriptionUpdateResult),
    typeof(CollectorSubscriptionRemovalResult),
    typeof(CollectorSubscriptionSnapshot))]
public sealed class CmdletSetEVXCollectorSubscription :
    PSCmdlet {
    private readonly CancellationTokenSource _stopping = new();

    /// <summary>Exact local collector subscription name.</summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "Enabled")]
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "Remove")]
    [Alias("SubscriptionName")]
    public string Name { get; set; } =
        null!;

    /// <summary>Desired enabled state.</summary>
    [Parameter(Mandatory = true, ParameterSetName = "Enabled")]
    public bool Enabled { get; set; }

    /// <summary>Removes the named local collector subscription.</summary>
    [Parameter(Mandatory = true, ParameterSetName = "Remove")]
    public SwitchParameter Remove { get; set; }

    /// <summary>Typed subscription definition produced by New-EVXCollectorSubscription.</summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Definition")]
    public CollectorSubscriptionDefinition? Definition { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        if (ParameterSetName == "Definition") {
            string subscriptionName = Definition!.SubscriptionId.Trim();
            if (!ShouldProcess(subscriptionName, "Create or update Windows Event Collector subscription")) {
                return;
            }
            WriteObject(
                CollectorSubscriptionManager.ApplyCollectorSubscription(
                    Definition,
                    _stopping.Token));
            return;
        }

        string name = Name.Trim();
        if (name.Length == 0) {
            throw new PSArgumentException(
                "Name cannot be empty.",
                nameof(Name));
        }
        if (ParameterSetName == "Remove") {
            if (!ShouldProcess(
                    name,
                    "Remove Windows Event Collector subscription")) {
                return;
            }
            WriteObject(
                CollectorSubscriptionManager
                    .RemoveCollectorSubscription(
                        name,
                        _stopping.Token));
            return;
        }
        if (!ShouldProcess(
                name,
                $"Set Windows Event Collector subscription Enabled={Enabled}")) {
            return;
        }
        WriteObject(
            CollectorSubscriptionManager
                .SetCollectorSubscriptionEnabled(
                    name,
                    Enabled));
    }

    /// <summary>Cancels an in-flight collector utility process when the pipeline stops.</summary>
    protected override void StopProcessing() {
        _stopping.Cancel();
    }
}
