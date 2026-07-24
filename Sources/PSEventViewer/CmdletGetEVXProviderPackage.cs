using EventViewerX.Providers;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Inspects a portable provider package or lists EventViewerX-managed installations.</para>
/// <para type="description">Package inspection verifies declared hashes and any detached signature before returning its typed definition. Without Path, the command returns the active machine-wide EventViewerX provider catalog.</para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "EVXProviderPackage")]
[OutputType(
    typeof(EventProviderPackage),
    typeof(InstalledEventProviderPackage))]
public sealed class CmdletGetEVXProviderPackage : PSCmdlet {
    /// <summary>
    /// <para>Optional .evxprovider package to verify and inspect.</para>
    /// </summary>
    [Parameter(
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true,
        Position = 0,
        ParameterSetName = "Package")]
    [Alias("FullName", "OutputPath", "PackagePath")]
    public string Path { get; set; } = string.Empty;

    /// <summary>Inspects one file or lists installed providers.</summary>
    protected override void ProcessRecord() {
        try {
            if (string.IsNullOrWhiteSpace(Path)) {
                WriteObject(
                    EventProviderPackageManager.GetInstalled(),
                    enumerateCollection: true);
            } else {
                WriteObject(
                    EventProviderPackageReader.Open(
                        SessionState.Path
                            .GetUnresolvedProviderPathFromPSPath(
                                Path)));
            }
        } catch (Exception exception) {
            WriteError(new ErrorRecord(
                exception,
                "EVXProviderPackageReadFailed",
                ErrorCategory.InvalidData,
                Path));
        }
    }
}
