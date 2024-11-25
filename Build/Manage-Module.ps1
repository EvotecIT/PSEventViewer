Clear-Host

Import-Module C:\Support\GitHub\PSPublishModule\PSPublishModule.psd1 -Force

Build-Module -ModuleName 'PSEventViewer' {
    # Usual defaults as per standard module
    $Manifest = [ordered] @{
        # Version number of this module.
        ModuleVersion        = '2.X.0'
        # Supported PSEditions
        CompatiblePSEditions = @('Desktop', 'Core')
        # ID used to uniquely identify this module
        GUID                 = '5df72a79-cdf6-4add-b38d-bcacf26fb7bc'
        # Author of this module
        Author               = 'Przemyslaw Klys'
        # Company or vendor of this module
        CompanyName          = 'Evotec'
        # Copyright statement for this module
        Copyright            = "(c) 2011 - $((Get-Date).Year) Przemyslaw Klys @ Evotec. All rights reserved."
        # Description of the functionality provided by this module
        Description          = 'Simple module allowing parsing of event logs. Has its own quirks...'
        # Tags applied to this module. These help with module discovery in online galleries.
        Tags                 = @('Events', 'Viewer', 'Windows', 'XML', 'XPATH', 'EVTX')

        IconUri              = 'https://evotec.xyz/wp-content/uploads/2018/10/PSEventViewer.png'

        ProjectUri           = 'https://github.com/EvotecIT/PSEventViewer'

        PowerShellVersion    = '5.1'
    }
    New-ConfigurationManifest @Manifest #-CmdletsToExport 'Find-WinEvent', 'Write-WinEvent', 'Start-EventWatching'

    # Add standard module dependencies (directly, but can be used with loop as well)
    New-ConfigurationModule -Type RequiredModule -Name 'PSSharedGoods' -Guid 'Auto' -Version 'Latest'
    # Add external module dependencies, using loop for simplicity
    New-ConfigurationModule -Type ExternalModule -Name 'Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Management', 'Microsoft.PowerShell.Diagnostics'
    # Add approved modules, that can be used as a dependency, but only when specific function from those modules is used
    # And on that time only that function and dependant functions will be copied over
    # Keep in mind it has it's limits when "copying" functions such as it should not depend on DLLs or other external files
    New-ConfigurationModule -Type ApprovedModule -Name 'PSSharedGoods', 'PSWriteColor', 'Connectimo', 'PSUnifi', 'PSWebToolbox', 'PSMyPassword'

    New-ConfigurationModuleSkip -IgnoreModuleName 'ActiveDirectory' -IgnoreFunctionName @(
        'Get-EventsInternal'
        'Initialize-XPathFilter'
        'Join-XPathFilter'
    )

    $ConfigurationFormat = [ordered] @{
        RemoveComments                              = $false

        PlaceOpenBraceEnable                        = $true
        PlaceOpenBraceOnSameLine                    = $true
        PlaceOpenBraceNewLineAfter                  = $true
        PlaceOpenBraceIgnoreOneLineBlock            = $false

        PlaceCloseBraceEnable                       = $true
        PlaceCloseBraceNewLineAfter                 = $false
        PlaceCloseBraceIgnoreOneLineBlock           = $false
        PlaceCloseBraceNoEmptyLineBefore            = $true

        UseConsistentIndentationEnable              = $true
        UseConsistentIndentationKind                = 'space'
        UseConsistentIndentationPipelineIndentation = 'IncreaseIndentationAfterEveryPipeline'
        UseConsistentIndentationIndentationSize     = 4

        UseConsistentWhitespaceEnable               = $true
        UseConsistentWhitespaceCheckInnerBrace      = $true
        UseConsistentWhitespaceCheckOpenBrace       = $true
        UseConsistentWhitespaceCheckOpenParen       = $true
        UseConsistentWhitespaceCheckOperator        = $true
        UseConsistentWhitespaceCheckPipe            = $true
        UseConsistentWhitespaceCheckSeparator       = $true

        AlignAssignmentStatementEnable              = $true
        AlignAssignmentStatementCheckHashtable      = $true

        UseCorrectCasingEnable                      = $true
    }
    # format PSD1 and PSM1 files when merging into a single file
    # enable formatting is not required as Configuration is provided
    New-ConfigurationFormat -ApplyTo 'OnMergePSM1', 'OnMergePSD1' -Sort None @ConfigurationFormat
    # format PSD1 and PSM1 files within the module
    # enable formatting is required to make sure that formatting is applied (with default settings)
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'DefaultPSM1' -EnableFormatting -Sort None
    # when creating PSD1 use special style without comments and with only required parameters
    New-ConfigurationFormat -ApplyTo 'DefaultPSD1', 'OnMergePSD1' -PSD1Style 'Minimal'

    # configuration for documentation, at the same time it enables documentation processing
    New-ConfigurationDocumentation -Enable:$false -StartClean -UpdateWhenNew -PathReadme 'Docs\Readme.md' -Path 'Docs'

    # New-ConfigurationImportModule -ImportSelf

    # $newConfigurationBuildSplat = @{
    #     Enable                            = $true
    #     SignModule                        = $true
    #     MergeModuleOnBuild                = $true
    #     MergeFunctionsFromApprovedModules = $true
    #     CertificateThumbprint             = '483292C9E317AA13B07BB7A96AE9D1A5ED9E7703'
    #     ResolveBinaryConflicts            = $false
    #     ResolveBinaryConflictsName        = 'PSEventViewer.PowerShell'
    #     NETProjectName                    = 'PSEventViewer.PowerShell'
    #     NETBinaryModule                   = 'PSEventViewer.PowerShell.dll'
    #     NETConfiguration                  = 'Release'
    #     NETFramework                      = 'netstandard2.0', 'net472'
    #     DotSourceLibraries                = $true
    # }

    $newConfigurationBuildSplat = @{
        Enable                            = $true
        SignModule                        = $true
        MergeModuleOnBuild                = $true
        MergeFunctionsFromApprovedModules = $true
        CertificateThumbprint             = '483292C9E317AA13B07BB7A96AE9D1A5ED9E7703'
        ResolveBinaryConflicts            = $true
        ResolveBinaryConflictsName        = 'PSEventViewer'
        NETProjectName                    = 'PSEventViewer'
        NETConfiguration                  = 'Release'
        NETFramework                      = 'net8.0', 'net472'
        NETSearchClass                    = "PSEventViewer.CmdletFindEvent"
        NETHandleAssemblyWithSameName     = $true
        #NETMergeLibraryDebugging          = $true
        DotSourceLibraries                = $true
        DotSourceClasses                  = $true
        DeleteTargetModuleBeforeBuild     = $true
    }

    New-ConfigurationBuild @newConfigurationBuildSplat

    New-ConfigurationArtefact -Type Unpacked -Enable -Path "$PSScriptRoot\..\Artefacts\Unpacked" -RequiredModulesPath "$PSScriptRoot\..\Artefacts\Unpacked\Modules"
    New-ConfigurationArtefact -Type Packed -Enable -Path "$PSScriptRoot\..\Artefacts\Packed" -IncludeTagName

    # global options for publishing to github/psgallery
    #New-ConfigurationPublish -Type PowerShellGallery -FilePath 'C:\Support\Important\PowerShellGalleryAPI.txt' -Enabled:$false
    #New-ConfigurationPublish -Type GitHub -FilePath 'C:\Support\Important\GitHubAPI.txt' -UserName 'EvotecIT' -Enabled:$false
}