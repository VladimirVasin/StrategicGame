<#
.SYNOPSIS
Builds deterministic authored high-resolution building sprites from a manifest.

.DESCRIPTION
Every source crop, runtime canvas, target visible rectangle, PPU, pivot, legacy
reference, and accepted hash is declared in HighResolutionBuildings.manifest.json.
The builder writes atomically, rejects surviving chroma key pixels, and rejects
byte-exact nearest-neighbor 2x copies of the procedural fallback.
#>

[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'HighResolutionBuildings.manifest.json'),
    [string[]]$Family,
    [ValidateRange(1, 254)]
    [int]$AlphaThreshold = 16,
    [switch]$ValidateOnly,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.AuthoredBuildingAssetBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProjectUnknown.Tools.Art
{
    public static class AuthoredBuildingAssetBuilder
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

        public static void Build(
            Bitmap source,
            Rectangle sourceBounds,
            int outputWidth,
            int outputHeight,
            Rectangle targetBounds,
            string outputPath)
        {
            if (sourceBounds.X < 0 || sourceBounds.Y < 0
                || sourceBounds.Right > source.Width || sourceBounds.Bottom > source.Height)
            {
                throw new InvalidOperationException("Source bounds exceed the master image.");
            }

            if (targetBounds.X < 0 || targetBounds.Y < 0
                || targetBounds.Right > outputWidth || targetBounds.Bottom > outputHeight)
            {
                throw new InvalidOperationException("Target bounds exceed the runtime canvas.");
            }

            using (Bitmap output = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(output))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.SmoothingMode = SmoothingMode.None;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(
                    source,
                    targetBounds,
                    sourceBounds.X,
                    sourceBounds.Y,
                    sourceBounds.Width,
                    sourceBounds.Height,
                    GraphicsUnit.Pixel,
                    attributes);
                output.Save(outputPath, ImageFormat.Png);
            }
        }

        public static long[] Validate(
            string outputPath,
            string legacyPath,
            int expectedWidth,
            int expectedHeight,
            int alphaThreshold,
            int legacyFrame)
        {
            using (Bitmap output = OpenArgb(outputPath))
            using (Bitmap legacy = OpenArgb(legacyPath))
            {
                if (output.Width != expectedWidth || output.Height != expectedHeight)
                {
                    throw new InvalidOperationException(String.Format(
                        "Output must be {0}x{1}; got {2}x{3}.",
                        expectedWidth,
                        expectedHeight,
                        output.Width,
                        output.Height));
                }

                int legacyFrameWidth = expectedWidth / 2;
                int legacyFrameHeight = expectedHeight / 2;
                if (expectedWidth % 2 != 0 || expectedHeight % 2 != 0)
                {
                    throw new InvalidOperationException(
                        "Output canvas must have even dimensions for the 2x legacy comparison.");
                }

                int legacyStartX = 0;
                if (legacyFrame >= 0)
                {
                    if (legacy.Height != legacyFrameHeight
                        || legacy.Width % legacyFrameWidth != 0
                        || legacyFrame >= legacy.Width / legacyFrameWidth)
                    {
                        throw new InvalidOperationException(
                            "Legacy animation atlas does not contain the requested 1x frame.");
                    }

                    legacyStartX = legacyFrame * legacyFrameWidth;
                }
                else if (legacy.Width != legacyFrameWidth || legacy.Height != legacyFrameHeight)
                {
                    throw new InvalidOperationException(
                        "Output canvas must remain exactly 2x the legacy canvas.");
                }

                long visible = 0;
                long chroma = 0;
                long nearestDifferences = 0;
                for (int y = 0; y < output.Height; y++)
                {
                    for (int x = 0; x < output.Width; x++)
                    {
                        Color pixel = output.GetPixel(x, y);
                        if (pixel.A >= alphaThreshold)
                        {
                            visible++;
                            if (pixel.R >= 238 && pixel.B >= 238 && pixel.G <= 48)
                            {
                                chroma++;
                            }
                        }

                        if (pixel.ToArgb()
                            != legacy.GetPixel(legacyStartX + x / 2, y / 2).ToArgb())
                        {
                            nearestDifferences++;
                        }
                    }
                }

                if (visible == 0)
                {
                    throw new InvalidOperationException("Output has no visible pixels.");
                }

                if (chroma > 0)
                {
                    throw new InvalidOperationException(
                        "Output contains visible magenta chroma-key pixels: " + chroma);
                }

                if (nearestDifferences == 0)
                {
                    throw new InvalidOperationException(
                        "Output is an exact nearest-neighbor 2x copy of the legacy fallback.");
                }

                return new[] { visible, nearestDifferences, chroma };
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

function ConvertTo-Rectangle {
    param(
        [Parameter(Mandatory)]$Values,
        [Parameter(Mandatory)][string]$Label
    )

    if ($Values.Count -ne 4) {
        throw "$Label must contain exactly four integers."
    }

    return [System.Drawing.Rectangle]::new(
        [int]$Values[0],
        [int]$Values[1],
        [int]$Values[2],
        [int]$Values[3])
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

$selected = @($manifest.families)
if ($null -ne $Family -and @($Family).Count -gt 0) {
    $requested = @($Family | ForEach-Object { $_.ToLowerInvariant() })
    $selected = @($selected | Where-Object { $requested -contains $_.tool.ToLowerInvariant() })
    if ($selected.Count -ne $requested.Count) {
        throw 'One or more requested families are missing from the manifest.'
    }
}

$seenTools = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $selected) {
    if (-not $seenTools.Add([string]$entry.tool)) {
        throw "Duplicate family in manifest: $($entry.tool)"
    }

    $sourcePath = Resolve-ProjectPath $entry.source "Source for $($entry.tool)"
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Source master does not exist: $sourcePath"
    }

    $sourceHash = Assert-Hash $sourcePath $entry.sourceSha256 "Source $($entry.tool)"
    $source = [ProjectUnknown.Tools.Art.AuthoredBuildingAssetBuilder]::OpenArgb($sourcePath)
    try {
        if ($entry.sourceSize.Count -ne 2 -or
            $source.Width -ne [int]$entry.sourceSize[0] -or
            $source.Height -ne [int]$entry.sourceSize[1]) {
            throw "Source dimensions changed for $($entry.tool): $($source.Width)x$($source.Height)"
        }

        $seenVariants = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase)
        foreach ($variant in @($entry.variants)) {
            if (-not $seenVariants.Add([string]$variant.id)) {
                throw "Duplicate variant $($variant.id) for $($entry.tool)"
            }

            $sourceBounds = ConvertTo-Rectangle $variant.sourceBounds "$($entry.tool) $($variant.id) sourceBounds"
            $targetBounds = ConvertTo-Rectangle $variant.targetBoundsTopLeft "$($entry.tool) $($variant.id) targetBoundsTopLeft"
            if ($variant.outputCanvas.Count -ne 2) {
                throw "$($entry.tool) $($variant.id) outputCanvas must contain width and height."
            }

            $outputWidth = [int]$variant.outputCanvas[0]
            $outputHeight = [int]$variant.outputCanvas[1]
            if ([float]$variant.ppu -le 0 -or $variant.pivotNormalized.Count -ne 2) {
                throw "$($entry.tool) $($variant.id) requires a valid PPU and pivot."
            }

            $legacyPath = Resolve-ProjectPath $variant.legacyReference "$($entry.tool) $($variant.id) legacy reference"
            $outputPath = Resolve-ProjectPath $variant.output "$($entry.tool) $($variant.id) output"
            $legacyFrameProperty = $variant.PSObject.Properties['legacyFrame']
            $legacyFrame = if ($null -ne $legacyFrameProperty) {
                [int]$legacyFrameProperty.Value
            }
            else {
                -1
            }
            if (-not (Test-Path -LiteralPath $legacyPath -PathType Leaf)) {
                throw "Legacy reference does not exist: $legacyPath"
            }

            if ($ValidateOnly) {
                if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
                    throw "Output does not exist for validation: $outputPath"
                }

                $stats = [ProjectUnknown.Tools.Art.AuthoredBuildingAssetBuilder]::Validate(
                    $outputPath,
                    $legacyPath,
                    $outputWidth,
                    $outputHeight,
                    $AlphaThreshold,
                    $legacyFrame)
                $hash = Assert-Hash $outputPath $variant.expectedSha256 "Output $($entry.tool) $($variant.id)"
                Write-Host "PASS $($entry.tool)/$($variant.id) hash=$hash visible=$($stats[0]) detailDelta=$($stats[1])"
                continue
            }

            if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
                throw "Output already exists: $outputPath. Pass -Force to replace it."
            }

            [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
            $temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
            try {
                [ProjectUnknown.Tools.Art.AuthoredBuildingAssetBuilder]::Build(
                    $source,
                    $sourceBounds,
                    $outputWidth,
                    $outputHeight,
                    $targetBounds,
                    $temporaryPath)
                $stats = [ProjectUnknown.Tools.Art.AuthoredBuildingAssetBuilder]::Validate(
                    $temporaryPath,
                    $legacyPath,
                    $outputWidth,
                    $outputHeight,
                    $AlphaThreshold,
                    $legacyFrame)
                $hash = Assert-Hash $temporaryPath $variant.expectedSha256 "Output $($entry.tool) $($variant.id)"
                Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
                Write-Host "Built $($entry.tool)/$($variant.id) -> $outputPath"
                Write-Host "  hash=$hash sourceHash=$sourceHash visible=$($stats[0]) detailDelta=$($stats[1])"
            }
            finally {
                if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
                    Remove-Item -LiteralPath $temporaryPath -Force
                }
            }
        }
    }
    finally {
        $source.Dispose()
    }
}
