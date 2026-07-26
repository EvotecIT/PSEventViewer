using EventViewerX.Providers;

namespace PSEventViewer;

/// <summary>
/// <para type="synopsis">Converts a friendly hashtable or custom object into a validated provider definition.</para>
/// <para type="description">Accepts concise PowerShell aliases such as ProviderName, ProviderGuid, Version, Message, and ordered field hashtables while retaining the complete typed EventViewerX provider schema for advanced channels, levels, tasks, opcodes, keywords, maps, localization, and versioned events.</para>
/// </summary>
/// <example>
///   <summary>Create a provider definition from a concise hashtable</summary>
///   <prefix>PS> </prefix>
///   <code>$definition = @{ ProviderName = 'Contoso.Scanner'; ProviderGuid = [guid]::NewGuid(); Version = '1.0.0'; Events = @{ Name = 'ScanCompleted'; Id = 1000; Message = 'Scan of {ComputerName} found {FindingCount} issues.'; Fields = [ordered]@{ ComputerName = 'String'; FindingCount = 'UInt32' } } } | ConvertTo-EVXProviderDefinition</code>
///   <para>Creates the default Contoso.Scanner/Operational channel and returns a strongly typed EventProviderDefinition.</para>
/// </example>
[Cmdlet(VerbsData.ConvertTo, "EVXProviderDefinition")]
[OutputType(typeof(EventProviderDefinition))]
public sealed class CmdletConvertToEVXProviderDefinition : PSCmdlet {
    /// <summary>
    /// <para>Typed definition, hashtable, or custom object to convert.</para>
    /// </summary>
    [Parameter(
        Mandatory = true,
        ValueFromPipeline = true,
        Position = 0)]
    [ValidateNotNull]
    public object InputObject { get; set; } = null!;

    /// <summary>Converts and validates the supplied definition.</summary>
    protected override void ProcessRecord() {
        try {
            WriteObject(
                PowerShellEventProviderDefinitionAdapter.Convert(
                    InputObject));
        } catch (Exception exception) {
            WriteError(new ErrorRecord(
                exception,
                "EVXProviderDefinitionInvalid",
                ErrorCategory.InvalidData,
                InputObject));
        }
    }
}
