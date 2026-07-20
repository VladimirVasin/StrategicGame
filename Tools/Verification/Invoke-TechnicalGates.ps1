[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$violations = [System.Collections.Generic.List[string]]::new()
$textExtensions = @(
    '.asmdef', '.asmref', '.asset', '.cs', '.csproj', '.inputactions', '.json',
    '.md', '.meta', '.ps1', '.shader', '.slnx', '.tsv', '.txt', '.unity', '.uss',
    '.uxml', '.yaml', '.yml'
)
$mojibakeSequences = @(
    [string]::Concat([char]0x00C3, [char]0x00A9),
    [string]::Concat([char]0x00C3, [char]0x00B6),
    [string]::Concat([char]0x00E2, [char]0x20AC, [char]0x2122),
    [string]::Concat([char]0x00E2, [char]0x20AC, [char]0x0153),
    [string]::Concat([char]0x00D0, [char]0x00B0),
    [string]::Concat([char]0x00D0, [char]0x00B1),
    [string]::Concat([char]0x00D1, [char]0x20AC),
    [string]::Concat([char]0x0420, [char]0x045F),
    [string]::Concat([char]0x0420, [char]0x00B0),
    [string]::Concat([char]0x0421, [char]0x201A)
)

function Get-NormalizedProjectRelativePath {
    param([Parameter(Mandatory)][string]$Path)

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path $projectRoot $Path
    }

    try {
        $fullPath = [System.IO.Path]::GetFullPath($candidate)
    }
    catch {
        return $null
    }

    $rootPrefix = $projectRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    return $fullPath.Substring($rootPrefix.Length).Replace('/', '\')
}

function Get-ExpectedCSharpFiles {
    param(
        [Parameter(Mandatory)][string[]]$SourceRoots,
        [switch]$RespectNestedAssemblyDefinitions
    )

    $files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($sourceRoot in $SourceRoots) {
        $absoluteRoot = Join-Path $projectRoot $sourceRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot -PathType Container)) {
            $violations.Add("Expected C# source root '$sourceRoot' does not exist")
            continue
        }

        $nestedAssemblyDirectories = [System.Collections.Generic.List[string]]::new()
        if ($RespectNestedAssemblyDefinitions) {
            Get-ChildItem -LiteralPath $absoluteRoot -Recurse -Filter '*.asmdef' -File |
                Where-Object {
                    -not $_.Directory.FullName.Equals(
                        $absoluteRoot,
                        [System.StringComparison]::OrdinalIgnoreCase)
                } |
                ForEach-Object {
                    $nestedAssemblyDirectories.Add(
                        $_.Directory.FullName.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar)
                }
        }

        Get-ChildItem -LiteralPath $absoluteRoot -Recurse -Filter '*.cs' -File | ForEach-Object {
            $filePath = $_.FullName
            $belongsToNestedAssembly = $false
            foreach ($boundary in $nestedAssemblyDirectories) {
                if ($filePath.StartsWith($boundary, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $belongsToNestedAssembly = $true
                    break
                }
            }

            if (-not $belongsToNestedAssembly) {
                $files.Add($_)
            }
        }
    }

    return $files
}

function Test-CSharpProject {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [string[]]$ExpectedSourceRoots,
        [switch]$RespectNestedAssemblyDefinitions
    )

    $projectName = Split-Path -Leaf $ProjectPath
    try {
        [xml]$projectXml = Get-Content -Raw -Encoding UTF8 -LiteralPath $ProjectPath
    }
    catch {
        $violations.Add("$projectName could not be parsed as XML: $($_.Exception.Message)")
        return
    }

    $includeCounts = [System.Collections.Generic.Dictionary[string, int]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    $compileNodes = @($projectXml.SelectNodes("//*[local-name()='Compile'][@Include]"))
    foreach ($compileNode in $compileNodes) {
        $include = $compileNode.GetAttribute('Include')
        if ([string]::IsNullOrWhiteSpace($include)) {
            continue
        }

        if ($include.IndexOfAny([char[]]'*?') -ge 0 -or $include.Contains('$(')) {
            $violations.Add("$projectName contains a non-explicit Compile Include '$include'")
            continue
        }

        try {
            $fullPath = if ([System.IO.Path]::IsPathRooted($include)) {
                [System.IO.Path]::GetFullPath($include)
            }
            else {
                [System.IO.Path]::GetFullPath((Join-Path $projectRoot $include))
            }
        }
        catch {
            $violations.Add("$projectName contains invalid Compile Include '$include'")
            continue
        }
        $normalizedRelativePath = Get-NormalizedProjectRelativePath $fullPath
        $key = if ($null -ne $normalizedRelativePath) { $normalizedRelativePath } else { $fullPath }
        if ($includeCounts.ContainsKey($key)) {
            $includeCounts[$key]++
        }
        else {
            $includeCounts.Add($key, 1)
        }

        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            $violations.Add("$projectName contains stale Compile Include '$include'")
        }
    }

    foreach ($entry in $includeCounts.GetEnumerator()) {
        if ($entry.Value -gt 1) {
            $displayPath = $entry.Key.Replace('\', '/')
            $violations.Add("$projectName contains Compile Include '$displayPath' $($entry.Value) times")
        }
    }

    $referenceNodes = @($projectXml.SelectNodes("//*[local-name()='Reference']/*[local-name()='HintPath']"))
    foreach ($hintNode in $referenceNodes) {
        $hintPath = $hintNode.InnerText
        if ([string]::IsNullOrWhiteSpace($hintPath) -or $hintPath.Contains('$(')) {
            continue
        }

        try {
            $referencePath = if ([System.IO.Path]::IsPathRooted($hintPath)) {
                [System.IO.Path]::GetFullPath($hintPath)
            }
            else {
                [System.IO.Path]::GetFullPath((Join-Path $projectRoot $hintPath))
            }
        }
        catch {
            $violations.Add("$projectName contains invalid Reference HintPath '$hintPath'")
            continue
        }

        if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
            $violations.Add("$projectName contains stale Reference HintPath '$hintPath'")
        }
    }

    if ($null -eq $ExpectedSourceRoots -or $ExpectedSourceRoots.Count -eq 0) {
        return
    }

    $expectedFiles = @(Get-ExpectedCSharpFiles -SourceRoots $ExpectedSourceRoots -RespectNestedAssemblyDefinitions:$RespectNestedAssemblyDefinitions)
    $expectedPaths = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($expectedFile in $expectedFiles) {
        $relativePath = Get-NormalizedProjectRelativePath $expectedFile.FullName
        [void]$expectedPaths.Add($relativePath)
        if (-not $includeCounts.ContainsKey($relativePath)) {
            $displayPath = $relativePath.Replace('\', '/')
            $violations.Add("$projectName is missing Compile Include '$displayPath'")
        }
    }

    foreach ($entry in $includeCounts.GetEnumerator()) {
        $relativePath = Get-NormalizedProjectRelativePath $entry.Key
        if ($null -eq $relativePath -or
            -not $relativePath.StartsWith('Assets\', [System.StringComparison]::OrdinalIgnoreCase) -or
            -not $relativePath.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if (-not $expectedPaths.Contains($relativePath)) {
            $displayPath = $relativePath.Replace('\', '/')
            $violations.Add("$projectName includes C# source outside its assembly boundary: '$displayPath'")
        }
    }
}

Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.cs' -File | ForEach-Object {
    $lineCount = 0
    foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
        $lineCount++
    }
    if ($lineCount -gt 500) {
        $relative = $_.FullName.Substring($projectRoot.Length + 1)
        $violations.Add("$relative has $lineCount lines (maximum 500)")
    }

    if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) {
        $relative = $_.FullName.Substring($projectRoot.Length + 1)
        $violations.Add("$relative is missing its .meta file")
    }

    $relative = $_.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    if ($relative.StartsWith('Assets/Scripts/Runtime/') -and
        -not $relative.StartsWith('Assets/Scripts/Runtime/Input/')) {
        $source = [System.IO.File]::ReadAllText($_.FullName)
        if ($source.Contains('Keyboard.current') -or
            $source.Contains('Mouse.current') -or
            $source.Contains('KeyControl')) {
            $violations.Add("$relative bypasses StrategyInputRouter")
        }
    }
}

$textFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
$knownTextFiles = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($relativeRoot in @('Assets', 'Packages', 'ProjectSettings', 'Tools', 'ai', '.github')) {
    $absoluteRoot = Join-Path $projectRoot $relativeRoot
    if (-not (Test-Path -LiteralPath $absoluteRoot -PathType Container)) {
        continue
    }

    Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File | Where-Object {
        $textExtensions -contains $_.Extension
    } | ForEach-Object {
        if ($knownTextFiles.Add($_.FullName)) {
            $textFiles.Add($_)
        }
    }
}
Get-ChildItem -LiteralPath $projectRoot -File | Where-Object {
    $textExtensions -contains $_.Extension
} | ForEach-Object {
    if ($knownTextFiles.Add($_.FullName)) {
        $textFiles.Add($_)
    }
}

$textFiles | ForEach-Object {
    $textFile = $_
    try {
        $bytes = [System.IO.File]::ReadAllBytes($textFile.FullName)
        $utf8 = [System.Text.UTF8Encoding]::new($false, $true)
        $text = $utf8.GetString($bytes)
        $hasMojibake = $text.Contains([char]0xFFFD)
        if (-not $hasMojibake) {
            foreach ($sequence in $mojibakeSequences) {
                if ($text.Contains($sequence)) {
                    $hasMojibake = $true
                    break
                }
            }
        }

        if ($hasMojibake) {
            $relative = $textFile.FullName.Substring($projectRoot.Length + 1)
            $violations.Add("$relative contains a replacement character or a known UTF-8 mojibake sequence")
        }
    }
    catch {
        $relative = $textFile.FullName.Substring($projectRoot.Length + 1)
        $violations.Add("$relative could not be read as valid UTF-8: $($_.Exception.Message)")
    }
}

$unityVerificationScriptPath = Join-Path $projectRoot 'Tools\Verification\Invoke-UnityVerification.ps1'
try {
    $unityVerificationScript = Get-Content -Raw -Encoding UTF8 -LiteralPath $unityVerificationScriptPath
    [void][scriptblock]::Create($unityVerificationScript)
    foreach ($requiredFragment in @(
        "'QuickSoak'",
        'RunQuickSoakSmoke',
        'QuickSoakSmoke.txt')) {
        if (-not $unityVerificationScript.Contains($requiredFragment)) {
            $violations.Add("Invoke-UnityVerification.ps1 is missing QuickSoak contract '$requiredFragment'")
        }
    }
}
catch {
    $violations.Add("Invoke-UnityVerification.ps1 could not be parsed: $($_.Exception.Message)")
}

$technicalWorkflowPath = Join-Path $projectRoot '.github\workflows\technical-gates.yml'
if (-not (Test-Path -LiteralPath $technicalWorkflowPath -PathType Leaf)) {
    $violations.Add('.github/workflows/technical-gates.yml is missing')
}
else {
    $technicalWorkflow = Get-Content -Raw -Encoding UTF8 -LiteralPath $technicalWorkflowPath
    foreach ($requiredFragment in @(
        'schedule:',
        "if: env.RUN_FULL_SOAK != 'true'",
        'Invoke-UnityVerification.ps1 -Kind QuickSoak',
        "if: env.RUN_FULL_SOAK == 'true'",
        'Invoke-UnityVerification.ps1 -Kind Soak',
        'refs/heads/release/',
        'refs/tags/v')) {
        if (-not $technicalWorkflow.Contains($requiredFragment)) {
            $violations.Add("technical-gates.yml is missing tier contract '$requiredFragment'")
        }
    }
}

$assemblyNames = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::Ordinal)
$assemblyDefinitions = @(
    Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.asmdef' -File
)
foreach ($assemblyDefinition in $assemblyDefinitions) {
    $relativePath = $assemblyDefinition.FullName.Substring($projectRoot.Length + 1)
    if (-not (Test-Path -LiteralPath ($assemblyDefinition.FullName + '.meta') -PathType Leaf)) {
        $violations.Add("$relativePath is missing its .meta file")
    }

    try {
        $definition = Get-Content -Raw -Encoding UTF8 -LiteralPath $assemblyDefinition.FullName |
            ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace($definition.name)) {
            $violations.Add("$relativePath has no assembly name")
        }
        elseif (-not $assemblyNames.Add([string]$definition.name)) {
            $violations.Add("$relativePath duplicates assembly name '$($definition.name)'")
        }
    }
    catch {
        $violations.Add("$relativePath could not be parsed as an asmdef: $($_.Exception.Message)")
    }
}

try {
    $manifestPath = Join-Path $projectRoot 'Packages\manifest.json'
    $lockPath = Join-Path $projectRoot 'Packages\packages-lock.json'
    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json
    $packageLock = Get-Content -Raw -Encoding UTF8 -LiteralPath $lockPath | ConvertFrom-Json
    $reachablePackages = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    $packageQueue = [System.Collections.Generic.Queue[string]]::new()
    foreach ($packageName in $manifest.dependencies.psobject.Properties.Name) {
        $packageQueue.Enqueue($packageName)
    }

    while ($packageQueue.Count -gt 0) {
        $packageName = $packageQueue.Dequeue()
        if (-not $reachablePackages.Add($packageName)) {
            continue
        }

        $packageNode = $packageLock.dependencies.$packageName
        if ($null -eq $packageNode) {
            $violations.Add("packages-lock.json is missing package '$packageName'")
            continue
        }

        foreach ($dependencyName in $packageNode.dependencies.psobject.Properties.Name) {
            $packageQueue.Enqueue($dependencyName)
        }
    }

    foreach ($lockedPackage in $packageLock.dependencies.psobject.Properties.Name) {
        if (-not $reachablePackages.Contains($lockedPackage)) {
            $violations.Add("packages-lock.json contains unreachable package '$lockedPackage'")
        }
    }
}
catch {
    $violations.Add("Package manifest/lock validation failed: $($_.Exception.Message)")
}

$projectSpecifications = @{
    'Assembly-CSharp.csproj' = @{
        Roots = @('Assets\Scripts\Runtime')
        RespectNestedAsmdefs = $false
    }
    'Assembly-CSharp-Editor.csproj' = @{
        Roots = @('Assets\Editor', 'Assets\Tests\EditMode')
        RespectNestedAsmdefs = $false
    }
    'ProjectUnknown.Runtime.csproj' = @{
        Roots = @('Assets\Scripts\Runtime')
        RespectNestedAsmdefs = $true
    }
    'ProjectUnknown.Editor.csproj' = @{
        Roots = @('Assets\Editor')
        RespectNestedAsmdefs = $true
    }
    'ProjectUnknown.EditModeTests.csproj' = @{
        Roots = @('Assets\Tests\EditMode')
        RespectNestedAsmdefs = $true
    }
}

$projectPaths = [System.Collections.Generic.List[string]]::new()
$knownProjectPaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($projectName in @(
    'Assembly-CSharp.csproj',
    'Assembly-CSharp-Editor.csproj',
    'ProjectUnknown.Runtime.csproj',
    'ProjectUnknown.Editor.csproj',
    'ProjectUnknown.EditModeTests.csproj')) {
    $projectPath = Join-Path $projectRoot $projectName
    if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
        $fullPath = [System.IO.Path]::GetFullPath($projectPath)
        if ($knownProjectPaths.Add($fullPath)) {
            $projectPaths.Add($fullPath)
        }
    }
}
Get-ChildItem -LiteralPath $projectRoot -Filter 'ProjectUnknown*.csproj' -File |
    Sort-Object Name |
    ForEach-Object {
        if ($knownProjectPaths.Add($_.FullName)) {
            $projectPaths.Add($_.FullName)
        }
    }

foreach ($projectPath in $projectPaths) {
    $projectName = Split-Path -Leaf $projectPath
    $specification = $projectSpecifications[$projectName]
    if ($null -ne $specification) {
        Test-CSharpProject -ProjectPath $projectPath -ExpectedSourceRoots $specification.Roots -RespectNestedAssemblyDefinitions:$specification.RespectNestedAsmdefs
    }
    else {
        Test-CSharpProject -ProjectPath $projectPath
    }
}

if ($violations.Count -gt 0) {
    Write-Output "FAIL: technical quality gates found $($violations.Count) violation(s):"
    foreach ($violation in $violations) {
        Write-Output " - $violation"
    }

    throw "Technical quality gates failed with $($violations.Count) violation(s)."
}

if (-not $SkipBuild) {
    $preferredDotnetPath = 'C:\Program Files\dotnet\dotnet.exe'
    $dotnetPath = if (Test-Path -LiteralPath $preferredDotnetPath -PathType Leaf) {
        $preferredDotnetPath
    }
    else {
        (Get-Command dotnet -ErrorAction Stop).Source
    }

    foreach ($projectPath in $projectPaths) {
        $projectName = Split-Path -Leaf $projectPath
        Write-Output "Building $projectName"
        & $dotnetPath build $projectPath --no-dependencies -v:minimal
        if ($LASTEXITCODE -ne 0) {
            throw "$projectName build failed with exit code $LASTEXITCODE."
        }
    }
}

Write-Output 'PASS: technical quality gates'
