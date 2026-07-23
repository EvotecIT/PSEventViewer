Describe 'Export-EVXEvent direct streaming contract' {
    BeforeAll {
        $SourcePath = [System.IO.Path]::Combine(
            $PSScriptRoot,
            'Logs',
            'NamedFilterExamples.evtx'
        )
        $OutputDirectory = [System.IO.Path]::Combine(
            [System.IO.Path]::GetTempPath(),
            "PSEventViewer-Export-$([guid]::NewGuid().ToString('N'))"
        )
        [System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
    }

    AfterAll {
        if ([System.IO.Directory]::Exists($OutputDirectory)) {
            [System.IO.Directory]::Delete($OutputDirectory, $true)
        }
    }

    It 'exports complete English JSON Lines without a PowerShell object pipeline' {
        $OutputPath = [System.IO.Path]::Combine($OutputDirectory, 'events.jsonl')

        $Result = Export-EVXEvent `
            -Path $SourcePath `
            -OutputPath $OutputPath `
            -Format JsonLines `
            -ReadMode Full `
            -MessageCulture en-US `
            -Oldest `
            -MaxEvents 5

        $Lines = [System.IO.File]::ReadAllLines($OutputPath)
        $First = $Lines[0] | ConvertFrom-Json
        $Result.EventCount | Should -Be 5
        $Lines.Count | Should -Be 5
        $First.messageCulture | Should -Be 'en-US'
        $First.xml | Should -Not -BeNullOrEmpty
        $Result.Sha256 | Should -Match '^[0-9A-F]{64}$'
    }

    It 'uses the metadata projection for high-throughput CSV export' {
        $OutputPath = [System.IO.Path]::Combine($OutputDirectory, 'metadata.csv')

        $Result = Export-EVXEvent `
            -Path $SourcePath `
            -OutputPath $OutputPath `
            -Format Csv `
            -ReadMode Metadata `
            -Oldest `
            -MaxEvents 4

        $Rows = Import-Csv -LiteralPath $OutputPath
        $Result.EventCount | Should -Be 4
        $Rows.Count | Should -Be 4
        $Rows[0].ProviderName | Should -Not -BeNullOrEmpty
        $Rows[0].Message | Should -BeNullOrEmpty
        $Rows[0].Xml | Should -BeNullOrEmpty
    }

    It 'does not create output under WhatIf' {
        $OutputPath = [System.IO.Path]::Combine($OutputDirectory, 'whatif.jsonl')

        Export-EVXEvent `
            -Path $SourcePath `
            -OutputPath $OutputPath `
            -WhatIf

        [System.IO.File]::Exists($OutputPath) | Should -BeFalse
    }
}
