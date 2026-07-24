using System.ComponentModel;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Writes a registered manifest/ETW event using its provider schema.</para>
/// <para type="description">Resolves the registered provider and event version, validates the positional payload against the manifest template, converts values according to each declared Windows input type, and writes through the dependency-free EventViewerX native engine.</para>
/// <para type="description">Use Write-EVXEntry for classic Event Log sources. Write-EVXEvent is the manifest-provider counterpart to New-WinEvent and only targets the local computer, matching the Windows ETW registration model.</para>
/// </summary>
/// <example>
///   <summary>Write a PowerShell workflow state event</summary>
///   <prefix>PS> </prefix>
///   <code>Write-EVXEvent -ProviderName Microsoft-Windows-PowerShell -Id 45090 -Payload Workflow, Running</code>
///   <para>Resolves event 45090 from the registered provider, validates its two template fields, and returns the confirmed native write result.</para>
/// </example>
/// <example>
///   <summary>Select an explicit event version</summary>
///   <prefix>PS> </prefix>
///   <code>Write-EVXEvent -ProviderName Contoso-Provider -Id 100 -Version 2 -Payload 42, "Complete"</code>
///   <para>Uses version 2 when the provider declares multiple versions of event ID 100.</para>
/// </example>
[Cmdlet(
    VerbsCommunications.Write,
    "EVXEvent",
    SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ManifestEventWriteResult))]
public sealed class CmdletWriteEVXEvent : PSCmdlet {
    /// <summary>
    /// <para>Name of a registered local manifest event provider.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string ProviderName { get; set; } = null!;

    /// <summary>
    /// <para>Event identifier declared by the provider manifest.</para>
    /// </summary>
    [Alias("EventId")]
    [Parameter(Mandatory = true, Position = 1)]
    [ValidateRange(0, ushort.MaxValue)]
    public int Id { get; set; }

    /// <summary>
    /// <para>Event version. Required when the provider declares multiple versions of the selected identifier.</para>
    /// </summary>
    [Parameter]
    public byte? Version { get; set; }

    /// <summary>
    /// <para>Ordered values for the event template. Values are converted using each manifest field's declared input type.</para>
    /// </summary>
    [AllowEmptyCollection]
    [AllowNull]
    [Parameter(Position = 2)]
    public object?[] Payload { get; set; } = Array.Empty<object?>();

    /// <summary>
    /// Writes the schema-validated event and returns the native result.
    /// </summary>
    protected override void ProcessRecord() {
        string version = Version.HasValue
            ? $" version {Version.Value}"
            : string.Empty;
        string target = $"{ProviderName}/event {Id}{version}";
        if (!ShouldProcess(target, "Write manifest event")) {
            return;
        }

        try {
            ManifestEventWriteResult result =
                ManifestEventWriter.Write(
                    new ManifestEventWriteRequest {
                        ProviderName = ProviderName,
                        Id = Id,
                        Version = Version,
                        Payload = Payload ?? Array.Empty<object?>()
                    });
            if (!result.Success) {
                throw new Win32Exception(
                    checked((int)result.NativeStatus),
                    $"Windows rejected manifest event {Id} from provider " +
                    $"'{ProviderName}'.");
            }
            WriteObject(result);
        } catch (Exception exception) {
            WriteError(
                new ErrorRecord(
                    exception,
                    "EVXManifestEventWriteFailed",
                    ErrorCategory.WriteError,
                    target));
        }
    }
}
