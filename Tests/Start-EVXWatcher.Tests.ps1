Describe 'Start-EVXWatcher - Parameter validation' {
    It 'Fails when NumberOfThreads is less than 1' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 0 } | Should -Throw
    }
    It 'Fails when NumberOfThreads is greater than 1024' {
        { Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -Action {} -NumberOfThreads 2000 } | Should -Throw
    }
}

Describe 'Start-EVXWatcher - PowerShell event dispatch' {
    It 'queues detached events into the PowerShell action runspace and preserves $_' {
        $EventObject = Get-EVXEvent -LogName System -MaxEvents 1 -ReadMode Metadata | Select-Object -First 1
        if (-not $EventObject) {
            Set-ItResult -Skipped -Because 'The System event log contained no readable events.'
            return
        }

        $Assembly = [PSEventViewer.CmdletStartEVXWatcher].Assembly
        $BridgeType = $Assembly.GetType('PSEventViewer.PowerShellWatcherEventBridge', $true)
        $Bridge = [Activator]::CreateInstance($BridgeType, $true)
        $Flags = [Reflection.BindingFlags]'Static,NonPublic'
        $ActionBridge = $BridgeType.GetProperty('ActionScript', $Flags).GetValue($null)
        $Publish = $BridgeType.GetMethod('Publish', [Reflection.BindingFlags]'Instance,NonPublic')
        $Marker = Join-Path $TestDrive 'watcher-action.txt'
        $EscapedMarker = $Marker.Replace("'", "''")
        $UserAction = [ScriptBlock]::Create("Set-Content -LiteralPath '$EscapedMarker' -Value `$_.Id")
        $SourceIdentifier = 'PSEventViewer.Tests.' + [Guid]::NewGuid().ToString('N')
        $Subscriber = Register-ObjectEvent -InputObject $Bridge -EventName EventReceived -SourceIdentifier $SourceIdentifier -MessageData $UserAction -Action $ActionBridge

        try {
            $null = $Publish.Invoke($Bridge, @($EventObject.PSObject.BaseObject))
            $Deadline = [DateTime]::UtcNow.AddSeconds(10)
            while (-not (Test-Path -LiteralPath $Marker) -and [DateTime]::UtcNow -lt $Deadline) {
                Start-Sleep -Milliseconds 50
            }

            Test-Path -LiteralPath $Marker | Should -BeTrue
            Get-Content -LiteralPath $Marker | Should -Be ([string] $EventObject.Id)
        } finally {
            Unregister-Event -SourceIdentifier $SourceIdentifier -ErrorAction SilentlyContinue
            if ($Subscriber.Action) {
                Remove-Job -Job $Subscriber.Action -Force -ErrorAction SilentlyContinue
            }
        }
    }

    It 'reuses a named watcher only with an explicit stable action identity' {
        $Name = 'PSEventViewer.Tests.' + [Guid]::NewGuid().ToString('N')
        $BeforeCount = @(Get-EventSubscriber -Force | Where-Object SourceIdentifier -Like 'PSEventViewer.Watcher.*').Count
        $First = $null
        try {
            $First = Start-EVXWatcher -Name $Name -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {} -ActionIdentity 'tests:stable-action'
            $Second = Start-EVXWatcher -Name $Name -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {} -ActionIdentity 'tests:stable-action'

            $Second.Id | Should -Be $First.Id
            @(Get-EventSubscriber -Force | Where-Object SourceIdentifier -Like 'PSEventViewer.Watcher.*').Count |
                Should -Be ($BeforeCount + 1)
        } finally {
            if ($First) {
                Stop-EVXWatcher -Id $First.Id -ErrorAction SilentlyContinue
            }
        }
    }

    It 'rejects a recreated action delegate when no stable identity is supplied' {
        $Name = 'PSEventViewer.Tests.' + [Guid]::NewGuid().ToString('N')
        $First = $null
        try {
            $First = Start-EVXWatcher -Name $Name -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {}
            { Start-EVXWatcher -Name $Name -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {} } |
                Should -Throw
        } finally {
            if ($First) {
                Stop-EVXWatcher -Id $First.Id -ErrorAction SilentlyContinue
            }
        }
    }

    It 'stops watchers and removes bridge subscribers when the module is removed' {
        $Module = Get-Module PSEventViewer
        $ModulePath = $Module.Path
        $ExternalIds = [Collections.Generic.List[int]]::new()
        $ExternalIds.Add(1)
        $ExternalNamedEvents = [Collections.Generic.List[EventViewerX.NamedEvents]]::new()
        $ExternalAction = [Action[EventViewerX.EventObject]] { param($EventObject) }
        $ExternalWatcher = [EventViewerX.WatcherManager]::StartWatcher(
            ('EventViewerX.External.' + [Guid]::NewGuid().ToString('N')),
            $env:COMPUTERNAME,
            'Application',
            $ExternalIds,
            $ExternalNamedEvents,
            $ExternalAction,
            $false,
            $false,
            0,
            $null)
        $Watcher = Start-EVXWatcher -Name ('PSEventViewer.Tests.' + [Guid]::NewGuid().ToString('N')) -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {}

        try {
            Remove-Module PSEventViewer -Force

            $Watcher.EndTime | Should -Not -BeNullOrEmpty
            $ExternalWatcher.EndTime | Should -BeNullOrEmpty
            @(Get-EventSubscriber -Force | Where-Object SourceIdentifier -Like 'PSEventViewer.Watcher.*') | Should -BeNullOrEmpty
        } finally {
            [EventViewerX.WatcherManager]::StopWatcher($ExternalWatcher.Id) | Out-Null
            Import-Module -Name $ModulePath -Force
        }
    }

    It 'does not stop a watcher owned by another runspace module instance' {
        $Module = Get-Module PSEventViewer
        $ModulePath = $Module.Path
        $EscapedModulePath = $ModulePath.Replace("'", "''")
        $OtherRunspace = [RunspaceFactory]::CreateRunspace()
        $OtherPowerShell = [PowerShell]::Create()
        $MainWatcher = $null
        $OtherWatcher = $null
        try {
            $OtherRunspace.Open()
            $OtherPowerShell.Runspace = $OtherRunspace
            $OtherName = 'PSEventViewer.OtherRunspace.' + [Guid]::NewGuid().ToString('N')
            $OtherScript = "Import-Module -Name '$EscapedModulePath' -Force; Start-EVXWatcher -Name '$OtherName' -MachineName '$env:COMPUTERNAME' -LogName Application -EventId 1 -Action {}"
            $OtherResult = $OtherPowerShell.AddScript($OtherScript).Invoke()
            if ($OtherPowerShell.HadErrors) {
                throw ($OtherPowerShell.Streams.Error | Select-Object -First 1)
            }
            $OtherWatcher = $OtherResult[0]
            $MainWatcher = Start-EVXWatcher -Name ('PSEventViewer.MainRunspace.' + [Guid]::NewGuid().ToString('N')) -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {}

            Remove-Module PSEventViewer -Force

            $MainWatcher.EndTime | Should -Not -BeNullOrEmpty
            $OtherWatcher.EndTime | Should -BeNullOrEmpty

            $OtherPowerShell.Commands.Clear()
            $null = $OtherPowerShell.AddScript('Remove-Module PSEventViewer -Force').Invoke()
            $OtherWatcher.EndTime | Should -Not -BeNullOrEmpty
        } finally {
            if ($MainWatcher -and -not $MainWatcher.EndTime) {
                [EventViewerX.WatcherManager]::StopWatcher($MainWatcher.Id) | Out-Null
            }
            if ($OtherWatcher -and -not $OtherWatcher.EndTime) {
                [EventViewerX.WatcherManager]::StopWatcher($OtherWatcher.Id) | Out-Null
            }
            $OtherPowerShell.Dispose()
            $OtherRunspace.Dispose()
            Import-Module -Name $ModulePath -Force
        }
    }

    It 'keeps module ownership independent from user-mutable global variables' {
        $Module = Get-Module PSEventViewer
        $ModulePath = $Module.Path
        $Watcher = Start-EVXWatcher -Name ('PSEventViewer.MutableVariable.' + [Guid]::NewGuid().ToString('N')) -MachineName $env:COMPUTERNAME -LogName Application -EventId 1 -Action {}
        try {
            Set-Variable -Name PSEventViewer_WatcherOwnerId -Scope Global -Value ([Guid]::NewGuid()) -Force
            Remove-Module PSEventViewer -Force

            $Watcher.EndTime | Should -Not -BeNullOrEmpty
        } finally {
            Remove-Variable -Name PSEventViewer_WatcherOwnerId -Scope Global -Force -ErrorAction SilentlyContinue
            if ($Watcher -and -not $Watcher.EndTime) {
                [EventViewerX.WatcherManager]::StopWatcher($Watcher.Id) | Out-Null
            }
            Import-Module -Name $ModulePath -Force
        }
    }
}
