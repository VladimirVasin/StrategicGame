[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode', 'MainMenu', 'MainMenuLaunch', 'QuickSoak', 'Soak')]
    [string]$Kind = 'EditMode',
    [string]$UnityEditorPath,
    [ValidateRange(0, 7200)]
    [int]$TimeoutSeconds = 0
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Get-NormalizedDirectoryPath {
    param([Parameter(Mandatory)][string]$Path)

    try {
        return [System.IO.Path]::GetFullPath($Path.Trim('"')).TrimEnd('\', '/')
    }
    catch {
        return $null
    }
}

function Assert-NoUnityProcessForProject {
    $unityProcesses = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")
    $normalizedProjectRoot = Get-NormalizedDirectoryPath $projectRoot
    foreach ($process in $unityProcesses) {
        $commandLine = $process.CommandLine
        if ([string]::IsNullOrWhiteSpace($commandLine) -or
            $commandLine -match '(?i)AssetImportWorker') {
            continue
        }

        $projectPathMatch = [System.Text.RegularExpressions.Regex]::Match(
            $commandLine,
            '(?i)(?:^|\s)-projectpath(?:\s+|=)(?:"([^"]+)"|([^\s"]+))')
        if (-not $projectPathMatch.Success) {
            continue
        }

        $openProjectPath = if ($projectPathMatch.Groups[1].Success) {
            $projectPathMatch.Groups[1].Value
        }
        else {
            $projectPathMatch.Groups[2].Value
        }
        $normalizedOpenProjectPath = Get-NormalizedDirectoryPath $openProjectPath
        if ($null -ne $normalizedOpenProjectPath -and
            $normalizedOpenProjectPath.Equals(
                $normalizedProjectRoot,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Unity process $($process.ProcessId) already has this project open. Close it before running verification."
        }
    }
}

function Stop-ProcessTree {
    param([Parameter(Mandatory)][int]$ProcessId)

    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Remove-StaleArtifact {
    param([string]$Path)

    if (-not [string]::IsNullOrWhiteSpace($Path) -and
        (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Remove-Item -LiteralPath $Path -Force
    }
}

function Assert-FreshFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][datetime]$StartedAtUtc,
        [Parameter(Mandatory)][string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Description was not created at '$Path'."
    }

    $file = Get-Item -LiteralPath $Path
    if ($file.LastWriteTimeUtc -lt $StartedAtUtc.AddSeconds(-2)) {
        throw "$Description at '$Path' is stale."
    }
}

function Assert-SuccessfulEditModeResults {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][datetime]$StartedAtUtc
    )

    Assert-FreshFile -Path $Path -StartedAtUtc $StartedAtUtc -Description 'EditMode test result XML'
    try {
        [xml]$resultXml = Get-Content -Raw -Encoding UTF8 -LiteralPath $Path
    }
    catch {
        throw "EditMode test result XML could not be parsed: $($_.Exception.Message)"
    }

    $testRun = $resultXml.SelectSingleNode("/*[local-name()='test-run']")
    if ($null -eq $testRun) {
        throw 'EditMode test result XML does not contain a test-run root.'
    }

    $failed = 0
    $total = 0
    $failedIsValid = [int]::TryParse($testRun.GetAttribute('failed'), [ref]$failed)
    $totalIsValid = [int]::TryParse($testRun.GetAttribute('total'), [ref]$total)
    if ($testRun.GetAttribute('result') -ne 'Passed' -or
        -not $failedIsValid -or $failed -ne 0 -or
        -not $totalIsValid -or $total -le 0) {
        throw "EditMode tests did not pass: result='$($testRun.GetAttribute('result'))', total='$($testRun.GetAttribute('total'))', failed='$($testRun.GetAttribute('failed'))'."
    }
}

function Assert-SuccessfulPassMarker {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][datetime]$StartedAtUtc,
        [Parameter(Mandatory)][string]$VerificationKind
    )

    Assert-FreshFile -Path $Path -StartedAtUtc $StartedAtUtc -Description "$VerificationKind PASS marker"
    $result = (Get-Content -Raw -Encoding UTF8 -LiteralPath $Path).Trim()
    if (-not $result.StartsWith('PASS:', [System.StringComparison]::Ordinal)) {
        throw "Unity $VerificationKind verification did not produce a PASS marker: '$result'."
    }
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $versionLine = Get-Content -Encoding UTF8 (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') |
        Where-Object { $_ -like 'm_EditorVersion:*' } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine)) {
        throw 'Unity version could not be read from ProjectSettings/ProjectVersion.txt.'
    }

    $version = ($versionLine -split ':', 2)[1].Trim()
    $UnityEditorPath = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor was not found at '$UnityEditorPath'."
}

Assert-NoUnityProcessForProject

$logsDirectory = Join-Path $projectRoot 'Logs'
[void](New-Item -ItemType Directory -Path $logsDirectory -Force)
$logPath = Join-Path $logsDirectory "$Kind-CI.log"
$editModeResultPath = Join-Path $logsDirectory 'EditMode-results.xml'
$legacyEditModePassPath = Join-Path $logsDirectory 'EditModeVerification.txt'
$smokeConfiguration = switch ($Kind) {
    'PlayMode' {
        @{
            Method = 'ProjectUnknown.Strategy.EditorTests.StrategyVerificationRunner.RunPlayMode'
            PassFile = 'PlayModeSmoke.txt'
        }
    }
    'MainMenu' {
        @{
            Method = 'ProjectUnknown.Strategy.EditorTests.StrategyVerificationRunner.RunMainMenuSmoke'
            PassFile = 'MainMenuSmoke.txt'
        }
    }
    'MainMenuLaunch' {
        @{
            Method = 'ProjectUnknown.Strategy.EditorTests.StrategyVerificationRunner.RunMainMenuLaunchSmoke'
            PassFile = 'MainMenuLaunchSmoke.txt'
        }
    }
    'Soak' {
        @{
            Method = 'ProjectUnknown.Strategy.EditorTests.StrategyVerificationRunner.RunSoakSmoke'
            PassFile = 'SoakSmoke.txt'
        }
    }
    'QuickSoak' {
        @{
            Method = 'ProjectUnknown.Strategy.EditorTests.StrategyVerificationRunner.RunQuickSoakSmoke'
            PassFile = 'QuickSoakSmoke.txt'
        }
    }
    default { $null }
}
$passPath = if ($null -ne $smokeConfiguration) {
    Join-Path $logsDirectory $smokeConfiguration.PassFile
}
else {
    $null
}

Remove-StaleArtifact $logPath
if ($Kind -eq 'EditMode') {
    Remove-StaleArtifact $editModeResultPath
    Remove-StaleArtifact $legacyEditModePassPath
}
else {
    Remove-StaleArtifact $passPath
}
$startedAtUtc = [datetime]::UtcNow

if ($Kind -eq 'EditMode') {
    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', $projectRoot,
        '-runTests',
        '-testPlatform', 'EditMode',
        '-assemblyNames', 'ProjectUnknown.EditModeTests',
        '-testResults', $editModeResultPath,
        '-logFile', $logPath
    )
}
else {
    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', $projectRoot,
        '-executeMethod', $smokeConfiguration.Method,
        '-logFile', $logPath
    )
    if ($Kind -in @('MainMenuLaunch', 'QuickSoak', 'Soak')) {
        $unityArguments += @('-strategyBenchmarkSeed', '74123')
    }
}

$processArguments = @(
    foreach ($argument in $unityArguments) {
        $text = [string]$argument
        if ($text.IndexOfAny([char[]]' `t"') -ge 0) {
            '"' + $text.Replace('"', '\"') + '"'
        }
        else {
            $text
        }
    }
)
$startProcessArguments = @{
    FilePath = $UnityEditorPath
    ArgumentList = $processArguments
    WindowStyle = 'Hidden'
    PassThru = $true
}
$unityProcess = Start-Process @startProcessArguments
$effectiveTimeoutSeconds = if ($TimeoutSeconds -gt 0) {
    $TimeoutSeconds
}
elseif ($Kind -eq 'Soak') {
    1800
}
elseif ($Kind -eq 'EditMode') {
    900
}
else {
    600
}
if (-not $unityProcess.WaitForExit($effectiveTimeoutSeconds * 1000)) {
    Stop-ProcessTree -ProcessId $unityProcess.Id
    throw "Unity $Kind verification exceeded the $effectiveTimeoutSeconds second hard timeout. See $logPath."
}

$unityExitCode = $unityProcess.ExitCode
if ($unityExitCode -ne 0) {
    throw "Unity $Kind verification failed with exit code $unityExitCode. See $logPath."
}

if ($Kind -eq 'EditMode') {
    Assert-SuccessfulEditModeResults -Path $editModeResultPath -StartedAtUtc $startedAtUtc
}
else {
    Assert-SuccessfulPassMarker -Path $passPath -StartedAtUtc $startedAtUtc -VerificationKind $Kind
}

Write-Output "PASS: Unity $Kind verification"
