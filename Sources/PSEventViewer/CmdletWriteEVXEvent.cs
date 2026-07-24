using System.ComponentModel;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Writes a registered manifest/ETW event using positional, named, or typed schema values.</para>
/// <para type="description">Resolves and caches the exact registered event schema, validates every value, converts values according to native Windows types, and writes through the dependency-free EventViewerX engine. Named hashtable order does not matter.</para>
/// <para type="description">EventName is available for providers installed through an EventViewerX .evxprovider package. ProviderName plus Id works with any registered manifest provider. Use Write-EVXEntry for classic Event Log sources.</para>
/// </summary>
/// <example>
///   <summary>Write a package-managed event by friendly name</summary>
///   <prefix>PS> </prefix>
///   <code>Write-EVXEvent -ProviderName Contoso.Scanner -EventName ScanCompleted -Data @{ FindingCount = 7; ComputerName = $env:COMPUTERNAME }</code>
///   <para>Maps values to the manifest's canonical order by field name and writes the event.</para>
/// </example>
/// <example>
///   <summary>Write any registered provider by identifier</summary>
///   <prefix>PS> </prefix>
///   <code>Write-EVXEvent -ProviderName Microsoft-Windows-PowerShell -Id 45090 -Payload Workflow, Running</code>
///   <para>Uses the positional compatibility surface for an existing Windows provider.</para>
/// </example>
[Cmdlet(
    VerbsCommunications.Write,
    "EVXEvent",
    DefaultParameterSetName = "ByIdPayload",
    SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ManifestEventWriteResult))]
public sealed class CmdletWriteEVXEvent : PSCmdlet {
    private ResolvedManifestEventWriter? _namedWriter;

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
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ByIdPayload")]
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ByIdData")]
    [ValidateRange(0, ushort.MaxValue)]
    public int Id { get; set; }

    /// <summary>
    /// <para>Friendly event name from an EventViewerX-managed provider package.</para>
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 1,
        ParameterSetName = "ByNameData")]
    [ValidateNotNullOrEmpty]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// <para>Event version. Required when the selected identity has multiple schema versions.</para>
    /// </summary>
    [Parameter]
    public byte? Version { get; set; }

    /// <summary>
    /// <para>Ordered values for the compatibility surface. Prefer Data for custom providers.</para>
    /// </summary>
    [AllowEmptyCollection]
    [AllowNull]
    [Parameter(
        Position = 2,
        ParameterSetName = "ByIdPayload")]
    public object?[] Payload { get; set; } =
        Array.Empty<object?>();

    /// <summary>
    /// <para>Hashtable of values keyed by manifest field name. Key order is ignored. Accepts pipeline input for efficient repeated writes with one cached native registration.</para>
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        Position = 2,
        ParameterSetName = "ByIdData")]
    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        Position = 2,
        ParameterSetName = "ByNameData")]
    [ValidateNotNull]
    public IDictionary Data { get; set; } = null!;

    /// <summary>Writes the schema-validated event and returns the native result.</summary>
    protected override void ProcessRecord() {
        bool named = ParameterSetName != "ByIdPayload";
        string identity = ParameterSetName == "ByNameData"
            ? EventName
            : Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        string version = Version.HasValue
            ? $" version {Version.Value}"
            : string.Empty;
        string target =
            $"{ProviderName}/event {identity}{version}";
        if (!ShouldProcess(target, "Write manifest event")) {
            return;
        }

        try {
            ManifestEventWriteResult result;
            if (named) {
                _namedWriter ??=
                    ParameterSetName == "ByNameData"
                        ? ResolvedManifestEventWriter.Open(
                            ProviderName,
                            EventName,
                            Version)
                        : ResolvedManifestEventWriter.Open(
                            ProviderName,
                            Id,
                            Version);
                result = _namedWriter.Write(
                    ToNamedValues(Data));
            } else {
                result = ManifestEventWriter.Write(
                    new ManifestEventWriteRequest {
                        ProviderName = ProviderName,
                        Id = Id,
                        Version = Version,
                        Payload = Payload ??
                                  Array.Empty<object?>()
                    });
            }
            if (!result.Success) {
                throw new Win32Exception(
                    checked((int)result.NativeStatus),
                    $"Windows rejected manifest event {identity} from provider '{ProviderName}'.");
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

    /// <summary>Releases the cached provider registration after pipeline input completes.</summary>
    protected override void EndProcessing() {
        _namedWriter?.Dispose();
        _namedWriter = null;
    }

    /// <summary>Releases the cached provider registration when the pipeline stops.</summary>
    protected override void StopProcessing() {
        _namedWriter?.Dispose();
        _namedWriter = null;
    }

    private static IReadOnlyDictionary<string, object?> ToNamedValues(
        IDictionary values) {

        var result = new Dictionary<string, object?>(
            StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in values) {
            string name = entry.Key as string ??
                          string.Empty;
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(
                    "Data keys must be non-empty strings.",
                    nameof(values));
            }
            if (result.ContainsKey(name)) {
                throw new ArgumentException(
                    $"Data field '{name}' was supplied more than once.",
                    nameof(values));
            }
            result.Add(name, entry.Value);
        }
        return result;
    }
}
