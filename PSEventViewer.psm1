# Get public and private function definition files.
$Public = @( Get-ChildItem -Path $PSScriptRoot\Public\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Private = @( Get-ChildItem -Path $PSScriptRoot\Private\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Classes = @( Get-ChildItem -Path $PSScriptRoot\Classes\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
$Enums = @( Get-ChildItem -Path $PSScriptRoot\Enums\*.ps1 -ErrorAction SilentlyContinue -Recurse -File)
# Get all assemblies
$AssemblyFolders = Get-ChildItem -Path $PSScriptRoot\Lib -Directory -ErrorAction SilentlyContinue -File
$AssemblyFolders = Get-ChildItem -Path "C:\Support\GitHub\PSEventViewer\Sources\PSEventViewer.PowerShell\bin\Debug\net472" -File -ErrorAction SilentlyContinue

# to speed up development adding direct path to binaries, instead of the the Lib folder
$Development = $true
$BinaryDev = @(
    if ($PSEdition -eq 'Core') {
        "$PSScriptRoot\Sources\PSEventViewer.PowerShell\bin\Debug\netstandard2.0\PSEventViewer.PowerShell.dll"
    } else {
        "$PSScriptRoot\Sources\PSEventViewer.PowerShell\bin\Debug\net472\PSEventViewer.PowerShell.dll"
    }
)
$BinaryModules = @(
    "PSEventViewer.PowerShell.dll"
)

# Lets find which libraries we need to load
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
} else {
    #Write-Error -Message 'No assemblies found'
}

$Assembly = @(
    if ($Framework -and $PSEdition -eq 'Core') {
        Get-ChildItem -Path $PSScriptRoot\Lib\$Framework\*.dll -ErrorAction SilentlyContinue -Recurse
    }
    if ($FrameworkNet -and $PSEdition -ne 'Core') {
        Get-ChildItem -Path $PSScriptRoot\Lib\$FrameworkNet\*.dll -ErrorAction SilentlyContinue -Recurse
    }
)

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
    Foreach ($Import in @($Assembly)) {
        try {
            Write-Verbose -Message $Import.FullName
            Add-Type -Path $Import.Fullname -ErrorAction Stop
            #  }
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
    Foreach ($Import in @($Classes + $Enums + $Private + $Public)) {
        Try {
            . $Import.Fullname
        } Catch {
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