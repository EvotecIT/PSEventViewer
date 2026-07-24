namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Enables or disables an existing local Windows Event Collector subscription.</para>
/// <para type="description">Uses the supported Windows Event Collector service API, saves the subscription, and verifies the persisted value. Remote registry mutation and wholesale XML replacement are intentionally not exposed.</para>
/// </summary>
/// <example>
///   <summary>Disable a subscription on the current collector</summary>
///   <code>Set-EVXCollectorSubscription -Name 'Domain Controllers' -Enabled $false</code>
///   <para>Returns before and after snapshots plus whether the persisted state changed.</para>
/// </example>
[Cmdlet(
    VerbsCommon.Set,
    "EVXCollectorSubscription",
    SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.High)]
[OutputType(
    typeof(
        CollectorSubscriptionUpdateResult))]
public sealed class CmdletSetEVXCollectorSubscription :
    PSCmdlet {

    /// <summary>Exact local collector subscription name.</summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipelineByPropertyName = true)]
    [Alias("SubscriptionName")]
    public string Name { get; set; } =
        null!;

    /// <summary>Desired enabled state.</summary>
    [Parameter(Mandatory = true)]
    public bool Enabled { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord() {
        string name = Name.Trim();
        if (name.Length == 0) {
            throw new PSArgumentException(
                "Name cannot be empty.",
                nameof(Name));
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
}
