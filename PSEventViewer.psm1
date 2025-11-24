# to speed up development adding direct path to binaries, instead of the the Lib folder
$Development = $true
$DevelopmentPath = "$PSScriptRoot\Sources\PSEventViewer\bin\Debug"
$DevelopmentFolderCore = "net8.0"
$DevelopmentFolderDefault = "net472"
$BinaryModules = @(
    "PSEventViewer.dll"
)

# Get public and private function definition files.
$Public = @( Get-ChildItem -Path $PSScriptRoot\Public\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Private = @( Get-ChildItem -Path $PSScriptRoot\Private\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Classes = @( Get-ChildItem -Path $PSScriptRoot\Classes\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Enums = @( Get-ChildItem -Path $PSScriptRoot\Enums\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
# Get all assemblies
$AssemblyFolders = Get-ChildItem -Path $PSScriptRoot\Lib -Directory -ErrorAction SilentlyContinue -File

# Lets find which libraries we need to load
if ($Development) {
    $Framework = 'Core'
    $FrameworkNet = 'Default'
} else {
    $Default = $false
    $Core = $false
    $Standard = $false
    foreach ($A in $AssemblyFolders.Name) {
        if ($A -eq 'Default') {
            $Default = $true
        } elseif ($A -eq 'Core') {
            $Core = $true
        } elseif ($A -eq 'Standard') {
            $Standard = $true
        }
    }
    if ($Standard -and $Core -and $Default) {
        $FrameworkNet = 'Default'
        $Framework = 'Standard'
    } elseif ($Standard -and $Core) {
        $Framework = 'Standard'
        $FrameworkNet = 'Standard'
    } elseif ($Core -and $Default) {
        $Framework = 'Core'
        $FrameworkNet = 'Default'
    } elseif ($Standard -and $Default) {
        $Framework = 'Standard'
        $FrameworkNet = 'Default'
    } elseif ($Standard) {
        $Framework = 'Standard'
        $FrameworkNet = 'Standard'
    } elseif ($Core) {
        $Framework = 'Core'
        $FrameworkNet = ''
    } elseif ($Default) {
        $Framework = ''
        $FrameworkNet = 'Default'
    }
}


$BinaryDev = @(
    foreach ($BinaryModule in $BinaryModules) {
        if ($PSEdition -eq 'Core') {
            $Variable = Resolve-Path "$DevelopmentPath\$DevelopmentFolderCore\$BinaryModule"
            $DevelopmentAssemblyFolder = Resolve-Path "$DevelopmentPath\$DevelopmentFolderCore"
        } else {
            $Variable = Resolve-Path "$DevelopmentPath\$DevelopmentFolderDefault\$BinaryModule"
            $DevelopmentAssemblyFolder = Resolve-Path "$DevelopmentPath\$DevelopmentFolderDefault"
        }
        $Variable
        Write-Warning "Development mode: Using binaries from $Variable"
    }
)

# if ($Development) {
#     # Preload BCL helper assemblies to avoid version mismatches inside the VSCode PowerShell host (Desktop 5.1).
#     $PreloadAssemblies = @(
#         'Microsoft.Bcl.AsyncInterfaces.dll',
#         'System.Threading.Tasks.Extensions.dll',
#         'System.Memory.dll',
#         'System.Buffers.dll',
#         'System.Numerics.Vectors.dll',
#         'System.Runtime.CompilerServices.Unsafe.dll'
#     )
#     foreach ($Preload in $PreloadAssemblies) {
#         $PreloadPath = Join-Path $DevelopmentAssemblyFolder.Path $Preload
#         if (Test-Path $PreloadPath) {
#             try {
#                 [System.Reflection.Assembly]::LoadFrom($PreloadPath) | Out-Null
#             } catch {
#                 Write-Verbose ("Failed to preload {0}: {1}" -f $PreloadPath, $_.Exception.Message)
#             }
#         }
#     }
# }

if ($Development) {
    $Assembly = Get-ChildItem -Path "$($DevelopmentAssemblyFolder.Path)\*.dll" -ErrorAction SilentlyContinue -File
} else {
    $Assembly = @(
        if ($Framework -and $PSEdition -eq 'Core') {
            Get-ChildItem -Path $PSScriptRoot\Lib\$Framework\*.dll -ErrorAction SilentlyContinue #-Recurse
        }
        if ($FrameworkNet -and $PSEdition -ne 'Core') {
            Get-ChildItem -Path $PSScriptRoot\Lib\$FrameworkNet\*.dll -ErrorAction SilentlyContinue #-Recurse
        }
    )
}

$FoundErrors = @(
    if ($Development) {
        foreach ($BinaryModule in $BinaryDev) {
            try {
                Import-Module -Name $BinaryModule -Force -ErrorAction Stop
            } catch {
                Write-Warning "Failed to import module $($BinaryModule): $($_.Exception.Message)"
                $true
            }
        }
    } else {
        foreach ($BinaryModule in $BinaryModules) {
            try {
                if ($Framework -and $PSEdition -eq 'Core') {
                    Import-Module -Name "$PSScriptRoot\Lib\$Framework\$BinaryModule" -Force -ErrorAction Stop
                }
                if ($FrameworkNet -and $PSEdition -ne 'Core') {
                    Import-Module -Name "$PSScriptRoot\Lib\$FrameworkNet\$BinaryModule" -Force -ErrorAction Stop
                }
            } catch {
                Write-Warning "Failed to import module $($BinaryModule): $($_.Exception.Message)"
                $true
            }
        }
    }
    foreach ($Import in @($Assembly)) {
        try {
            # Write-Warning -Message $Import.FullName
            Add-Type -Path $Import.Fullname -ErrorAction Stop
        } catch [System.Reflection.ReflectionTypeLoadException] {
            Write-Warning "Processing $($Import.Name) Exception: $($_.Exception.Message)"
            $LoaderExceptions = $($_.Exception.LoaderExceptions) | Sort-Object -Unique
            foreach ($E in $LoaderExceptions) {
                Write-Warning "Processing $($Import.Name) LoaderExceptions: $($E.Message)"
            }
            $true
            #Write-Error -Message "StackTrace: $($_.Exception.StackTrace)"
        } catch {
            Write-Warning "Processing $($Import.Name) Exception: $($_.Exception.Message)"
            $LoaderExceptions = $($_.Exception.LoaderExceptions) | Sort-Object -Unique
            foreach ($E in $LoaderExceptions) {
                Write-Warning "Processing $($Import.Name) LoaderExceptions: $($E.Message)"
            }
            $true
            #Write-Error -Message "StackTrace: $($_.Exception.StackTrace)"
        }
    }
    #Dot source the files
    foreach ($Import in @($Classes + $Enums + $Private + $Public)) {
        try {
            . $Import.Fullname
        } catch {
            Write-Error -Message "Failed to import functions from $($import.Fullname): $_"
            $true
        }
    }
)

if ($FoundErrors.Count -gt 0) {
    $ModuleName = (Get-ChildItem $PSScriptRoot\*.psd1).BaseName
    Write-Warning "Importing module $ModuleName failed. Fix errors before continuing."
    break
}

Export-ModuleMember -Function '*' -Alias '*' -Cmdlet '*'