<#
.SYNOPSIS
Builds deterministic horizontal authored building-animation atlases.
#>

[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'HighResolutionBuildingAnimations.manifest.json'),
    [string[]]$Sequence,
    [ValidateRange(1, 254)][int]$AlphaThreshold = 16,
    [switch]$ValidateOnly,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.AuthoredAnimationAtlasBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProjectUnknown.Tools.Art
{
    public static class AuthoredAnimationAtlasBuilder
    {
        public static Bitmap OpenArgb(string path)
        {
            using (Image source = Image.FromFile(path))
            {
                Bitmap copy = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(copy))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImageUnscaled(source, 0, 0);
                }

                return copy;
            }
        }

        public static void Build(string[] framePaths, int frameWidth, int frameHeight, string outputPath)
        {
            using (Bitmap output = new Bitmap(
                checked(frameWidth * framePaths.Length),
                frameHeight,
                PixelFormat.Format32bppArgb))
            {
                for (int index = 0; index < framePaths.Length; index++)
                {
                    using (Bitmap frame = OpenArgb(framePaths[index]))
                    {
                        if (frame.Width != frameWidth || frame.Height != frameHeight)
                        {
                            throw new InvalidOperationException(String.Format(
                                "Frame {0} must be {1}x{2}; got {3}x{4}.",
                                index + 1,
                                frameWidth,
                                frameHeight,
                                frame.Width,
                                frame.Height));
                        }
                        for (int y = 0; y < frameHeight; y++)
                        {
                            for (int x = 0; x < frameWidth; x++)
                            {
                                output.SetPixel(index * frameWidth + x, y, frame.GetPixel(x, y));
                            }
                        }
                    }
                }

                output.Save(outputPath, ImageFormat.Png);
            }
        }

        public static long[] Validate(
            string atlasPath,
            string[] framePaths,
            int frameWidth,
            int frameHeight,
            int alphaThreshold)
        {
            using (Bitmap atlas = OpenArgb(atlasPath))
            {
                if (atlas.Width != frameWidth * framePaths.Length || atlas.Height != frameHeight)
                {
                    throw new InvalidOperationException("Animation atlas dimensions changed.");
                }

                long visible = 0;
                long chroma = 0;
                for (int index = 0; index < framePaths.Length; index++)
                {
                    long frameVisible = 0;
                    using (Bitmap frame = OpenArgb(framePaths[index]))
                    {
                        for (int y = 0; y < frameHeight; y++)
                        {
                            for (int x = 0; x < frameWidth; x++)
                            {
                                Color expected = frame.GetPixel(x, y);
                                Color actual = atlas.GetPixel(index * frameWidth + x, y);
                                bool differs = actual.A != expected.A
                                    || (actual.A > 0 && (
                                        Math.Abs(actual.R - expected.R) > 1
                                        || Math.Abs(actual.G - expected.G) > 1
                                        || Math.Abs(actual.B - expected.B) > 1));
                                if (differs)
                                {
                                    throw new InvalidOperationException(String.Format(
                                        "Atlas frame {0} pixel ({1},{2}) differs: expected {3}, got {4}.",
                                        index + 1,
                                        x,
                                        y,
                                        expected.ToArgb(),
                                        actual.ToArgb()));
                                }

                                if (actual.A >= alphaThreshold)
                                {
                                    visible++;
                                    frameVisible++;
                                    if (actual.R >= 238 && actual.B >= 238 && actual.G <= 48)
                                    {
                                        chroma++;
                                    }
                                }
                            }
                        }
                    }

                    if (frameVisible == 0)
                    {
                        throw new InvalidOperationException("Animation frame has no visible pixels: " + (index + 1));
                    }
                }

                if (chroma > 0)
                {
                    throw new InvalidOperationException(
                        "Animation atlas contains visible magenta chroma-key pixels: " + chroma);
                }

                return new[] { visible, chroma };
            }
        }
    }
}
'@
}

function Resolve-ProjectPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path $script:ProjectRoot $Path
    }

    $fullPath = [System.IO.Path]::GetFullPath($candidate)
    $rootPrefix = $script:ProjectRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must remain inside the project root: $fullPath"
    }

    return $fullPath
}

function Assert-Hash {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$Expected,
        [Parameter(Mandatory)][string]$Label
    )

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if (-not [string]::IsNullOrWhiteSpace($Expected) -and
        -not $actual.Equals($Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label SHA-256 changed: expected $Expected, got $actual"
    }

    return $actual
}

$ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$resolvedManifest = [System.IO.Path]::GetFullPath($ManifestPath)
if (-not (Test-Path -LiteralPath $resolvedManifest -PathType Leaf)) {
    throw "Manifest does not exist: $resolvedManifest"
}

$manifest = Get-Content -LiteralPath $resolvedManifest -Encoding UTF8 -Raw | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1) {
    throw "Unsupported manifest schemaVersion: $($manifest.schemaVersion)"
}

$selected = @($manifest.sequences)
if ($null -ne $Sequence -and @($Sequence).Count -gt 0) {
    $requested = @($Sequence | ForEach-Object { $_.ToLowerInvariant() })
    $selected = @($selected | Where-Object { $requested -contains $_.id.ToLowerInvariant() })
    if ($selected.Count -ne $requested.Count) {
        throw 'One or more requested animation sequences are missing from the manifest.'
    }
}

$seen = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $selected) {
    if (-not $seen.Add([string]$entry.id)) {
        throw "Duplicate animation sequence in manifest: $($entry.id)"
    }

    if ($entry.frameSize.Count -ne 2 -or [int]$entry.frameCount -ne @($entry.frames).Count) {
        throw "$($entry.id) has an invalid frame contract."
    }

    if ([float]$entry.ppu -le 0 -or $entry.pivotNormalized.Count -ne 2) {
        throw "$($entry.id) requires a valid PPU and pivot."
    }

    $frameWidth = [int]$entry.frameSize[0]
    $frameHeight = [int]$entry.frameSize[1]
    $framePaths = [System.Collections.Generic.List[string]]::new()
    foreach ($frame in @($entry.frames)) {
        $framePath = Resolve-ProjectPath $frame.source "$($entry.id) frame"
        if (-not (Test-Path -LiteralPath $framePath -PathType Leaf)) {
            throw "Animation frame does not exist: $framePath"
        }

        Assert-Hash $framePath $frame.sha256 "$($entry.id) frame" | Out-Null
        $framePaths.Add($framePath)
    }

    $outputPath = Resolve-ProjectPath $entry.output "$($entry.id) output"
    if ($ValidateOnly) {
        if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
            throw "Animation atlas does not exist: $outputPath"
        }

        $stats = [ProjectUnknown.Tools.Art.AuthoredAnimationAtlasBuilder]::Validate(
            $outputPath,
            $framePaths.ToArray(),
            $frameWidth,
            $frameHeight,
            $AlphaThreshold)
        $hash = Assert-Hash $outputPath $entry.expectedSha256 "$($entry.id) atlas"
        Write-Host "PASS $($entry.id) hash=$hash visible=$($stats[0])"
        continue
    }

    if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
        throw "Animation atlas already exists: $outputPath. Pass -Force to replace it."
    }

    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
    $temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
    try {
        [ProjectUnknown.Tools.Art.AuthoredAnimationAtlasBuilder]::Build(
            $framePaths.ToArray(),
            $frameWidth,
            $frameHeight,
            $temporaryPath)
        $stats = [ProjectUnknown.Tools.Art.AuthoredAnimationAtlasBuilder]::Validate(
            $temporaryPath,
            $framePaths.ToArray(),
            $frameWidth,
            $frameHeight,
            $AlphaThreshold)
        $hash = Assert-Hash $temporaryPath $entry.expectedSha256 "$($entry.id) atlas"
        Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
        Write-Host "Built $($entry.id) -> $outputPath"
        Write-Host "  hash=$hash visible=$($stats[0]) frames=$($entry.frameCount)"
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}
