<#
.SYNOPSIS
Builds variable-size 2x authored construction atlases from frozen 1x sources.

.DESCRIPTION
Stages 0-5 are independently high-quality resampled to avoid atlas bleed and
nearest-neighbor block duplication. Stage 6 uses the accepted final building
sprite at matching world scale. Source and output hashes are manifest-owned.
#>

[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'HighResolutionConstruction.manifest.json'),
    [string[]]$Family,
    [ValidateRange(1, 254)]
    [int]$AlphaThreshold = 16,
    [switch]$ValidateOnly,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.AuthoredConstructionAtlasBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProjectUnknown.Tools.Art
{
    public static class AuthoredConstructionAtlasBuilder
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

        public static Point Build(
            string sourcePath,
            string finalPath,
            int sourceFrameWidth,
            int sourceFrameHeight,
            int frameCount,
            float sourcePpu,
            float sourcePivotX,
            float sourcePivotY,
            int outputFrameWidth,
            int outputFrameHeight,
            float outputPpu,
            float outputPivotX,
            float outputPivotY,
            float finalPpu,
            float finalPivotX,
            float finalPivotY,
            string outputPath)
        {
            using (Bitmap source = OpenArgb(sourcePath))
            using (Bitmap final = OpenArgb(finalPath))
            using (Bitmap output = new Bitmap(
                outputFrameWidth * frameCount,
                outputFrameHeight,
                PixelFormat.Format32bppArgb))
            {
                if (source.Width != sourceFrameWidth * frameCount
                    || source.Height != sourceFrameHeight)
                {
                    throw new InvalidOperationException("Frozen source atlas dimensions changed.");
                }

                float constructionScale = outputPpu / sourcePpu;
                int scaledSourceWidth = (int)Math.Round(sourceFrameWidth * constructionScale);
                int scaledSourceHeight = (int)Math.Round(sourceFrameHeight * constructionScale);
                float scaledSourcePivotX = sourcePivotX * constructionScale;
                float scaledSourcePivotY = sourcePivotY * constructionScale;
                int sourcePlacementX = (int)Math.Round(outputPivotX - scaledSourcePivotX);
                int sourcePlacementY = (int)Math.Round(
                    outputFrameHeight - outputPivotY
                    - (scaledSourceHeight - scaledSourcePivotY));
                int finalFrameX = (frameCount - 1) * outputFrameWidth;
                float finalScale = outputPpu / finalPpu;
                int scaledFinalWidth = (int)Math.Round(final.Width * finalScale);
                int scaledFinalHeight = (int)Math.Round(final.Height * finalScale);
                float scaledFinalPivotX = final.Width * finalPivotX * finalScale;
                float scaledFinalPivotY = final.Height * finalPivotY * finalScale;
                int finalPlacementX = (int)Math.Round(outputPivotX - scaledFinalPivotX);
                int finalPlacementY = (int)Math.Round(
                    outputFrameHeight - outputPivotY
                    - (scaledFinalHeight - scaledFinalPivotY));

                using (Graphics atlasGraphics = Graphics.FromImage(output))
                {
                    atlasGraphics.Clear(Color.Transparent);
                    for (int stage = 0; stage < frameCount - 1; stage++)
                    {
                        using (Bitmap frame = new Bitmap(
                            sourceFrameWidth,
                            sourceFrameHeight,
                            PixelFormat.Format32bppArgb))
                        using (Graphics frameGraphics = Graphics.FromImage(frame))
                        using (Bitmap scaled = new Bitmap(
                            scaledSourceWidth,
                            scaledSourceHeight,
                            PixelFormat.Format32bppArgb))
                        using (Graphics scaledGraphics = Graphics.FromImage(scaled))
                        using (ImageAttributes attributes = new ImageAttributes())
                        {
                            frameGraphics.CompositingMode = CompositingMode.SourceCopy;
                            frameGraphics.DrawImage(
                                source,
                                new Rectangle(0, 0, sourceFrameWidth, sourceFrameHeight),
                                new Rectangle(stage * sourceFrameWidth, 0, sourceFrameWidth, sourceFrameHeight),
                                GraphicsUnit.Pixel);

                            scaledGraphics.Clear(Color.Transparent);
                            scaledGraphics.CompositingMode = CompositingMode.SourceCopy;
                            scaledGraphics.CompositingQuality = CompositingQuality.HighQuality;
                            scaledGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            scaledGraphics.PixelOffsetMode = PixelOffsetMode.Half;
                            scaledGraphics.SmoothingMode = SmoothingMode.None;
                            attributes.SetWrapMode(WrapMode.TileFlipXY);
                            scaledGraphics.DrawImage(
                                frame,
                                new Rectangle(0, 0, scaledSourceWidth, scaledSourceHeight),
                                0,
                                0,
                                sourceFrameWidth,
                                sourceFrameHeight,
                                GraphicsUnit.Pixel,
                                attributes);

                            atlasGraphics.CompositingMode = CompositingMode.SourceCopy;
                            atlasGraphics.DrawImageUnscaled(
                                scaled,
                                stage * outputFrameWidth + sourcePlacementX,
                                sourcePlacementY);
                        }
                    }

                    DrawGenericScaffold(
                        atlasGraphics,
                        finalFrameX,
                        outputFrameWidth,
                        outputFrameHeight);

                    if (scaledFinalWidth != final.Width || scaledFinalHeight != final.Height)
                    {
                        atlasGraphics.CompositingMode = CompositingMode.SourceCopy;
                        atlasGraphics.CompositingQuality = CompositingQuality.HighQuality;
                        atlasGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        atlasGraphics.PixelOffsetMode = PixelOffsetMode.Half;
                        atlasGraphics.DrawImage(
                            final,
                            new Rectangle(
                                finalFrameX + finalPlacementX,
                                finalPlacementY,
                                scaledFinalWidth,
                                scaledFinalHeight),
                            0,
                            0,
                            final.Width,
                            final.Height,
                            GraphicsUnit.Pixel);
                    }
                }

                if (scaledFinalWidth == final.Width && scaledFinalHeight == final.Height)
                {
                    for (int y = 0; y < final.Height; y++)
                    {
                        for (int x = 0; x < final.Width; x++)
                        {
                            output.SetPixel(
                                finalFrameX + finalPlacementX + x,
                                finalPlacementY + y,
                                final.GetPixel(x, y));
                        }
                    }
                }

                output.Save(outputPath, ImageFormat.Png);
                return new Point(finalPlacementX, finalPlacementY);
            }
        }

        public static long[] Validate(
            string outputPath,
            string sourcePath,
            string finalPath,
            int sourceFrameWidth,
            int sourceFrameHeight,
            int outputFrameWidth,
            int outputFrameHeight,
            int frameCount,
            int finalPlacementX,
            int finalPlacementY,
            float outputPpu,
            float finalPpu,
            int alphaThreshold)
        {
            using (Bitmap output = OpenArgb(outputPath))
            using (Bitmap source = OpenArgb(sourcePath))
            using (Bitmap final = OpenArgb(finalPath))
            {
                if (output.Width != outputFrameWidth * frameCount
                    || output.Height != outputFrameHeight)
                {
                    throw new InvalidOperationException("Construction output dimensions changed.");
                }

                long visible = 0;
                long chroma = 0;
                long detailDifferences = 0;
                long previousDifference = 0;
                for (int stage = 0; stage < frameCount; stage++)
                {
                    long stageVisible = 0;
                    long stageDifference = 0;
                    for (int y = 0; y < outputFrameHeight; y++)
                    {
                        for (int x = 0; x < outputFrameWidth; x++)
                        {
                            Color pixel = output.GetPixel(stage * outputFrameWidth + x, y);
                            if (pixel.A >= alphaThreshold)
                            {
                                stageVisible++;
                                visible++;
                                if (pixel.R >= 238 && pixel.B >= 238 && pixel.G <= 48)
                                {
                                    chroma++;
                                }
                            }

                            if (stage > 0)
                            {
                                Color previous = output.GetPixel((stage - 1) * outputFrameWidth + x, y);
                                if (pixel.ToArgb() != previous.ToArgb())
                                {
                                    stageDifference++;
                                }
                            }
                        }
                    }

                    if (stageVisible == 0)
                    {
                        throw new InvalidOperationException("Construction stage is empty: " + stage);
                    }

                    if (stage > 0 && stageDifference == 0)
                    {
                        throw new InvalidOperationException("Adjacent construction stages are identical: " + stage);
                    }

                    previousDifference += stageDifference;
                }

                int sampledWidth = Math.Min(sourceFrameWidth * 2, outputFrameWidth);
                int sampledHeight = Math.Min(sourceFrameHeight * 2, outputFrameHeight);
                for (int stage = 0; stage < frameCount - 1; stage++)
                {
                    for (int y = 0; y < sampledHeight; y++)
                    {
                        for (int x = 0; x < sampledWidth; x++)
                        {
                            Color authored = output.GetPixel(stage * outputFrameWidth + x, y);
                            Color nearest = source.GetPixel(
                                stage * sourceFrameWidth + Math.Min(sourceFrameWidth - 1, x / 2),
                                Math.Min(sourceFrameHeight - 1, y / 2));
                            if (authored.ToArgb() != nearest.ToArgb())
                            {
                                detailDifferences++;
                            }
                        }
                    }
                }

                if (detailDifferences == 0)
                {
                    throw new InvalidOperationException(
                        "Construction stages are exact nearest-neighbor 2x copies.");
                }

                if (chroma > 0)
                {
                    throw new InvalidOperationException(
                        "Construction atlas contains visible magenta pixels: " + chroma);
                }

                if (Math.Abs(outputPpu - finalPpu) < 0.001f)
                {
                    int frameX = (frameCount - 1) * outputFrameWidth + finalPlacementX;
                    for (int y = 0; y < final.Height; y++)
                    {
                        for (int x = 0; x < final.Width; x++)
                        {
                            Color expected = final.GetPixel(x, y);
                            if (expected.A < alphaThreshold)
                            {
                                continue;
                            }

                            Color actual = output.GetPixel(frameX + x, finalPlacementY + y);
                            if (!EquivalentVisiblePixel(expected, actual))
                            {
                                throw new InvalidOperationException(
                                    String.Format(
                                        "Final authored sprite changed at ({0},{1}): expected {2:X8}, actual {3:X8}.",
                                        x,
                                        y,
                                        expected.ToArgb(),
                                        actual.ToArgb()));
                            }
                        }
                    }
                }

                return new[] { visible, detailDifferences, previousDifference };
            }
        }

        private static bool EquivalentVisiblePixel(Color expected, Color actual)
        {
            if (expected.A != actual.A)
            {
                return false;
            }

            if (expected.A == 255)
            {
                return expected.ToArgb() == actual.ToArgb();
            }

            return
                Math.Abs(expected.R * expected.A - actual.R * actual.A) <= 255
                && Math.Abs(expected.G * expected.A - actual.G * actual.A) <= 255
                && Math.Abs(expected.B * expected.A - actual.B * actual.A) <= 255;
        }

        private static void DrawGenericScaffold(
            Graphics graphics,
            int frameX,
            int frameWidth,
            int frameHeight)
        {
            int left = frameX + 7;
            int right = frameX + frameWidth - 8;
            int top = 14;
            int bottom = frameHeight - 12;
            using (Pen outline = new Pen(Color.FromArgb(255, 55, 35, 23), 6f))
            using (Pen timber = new Pen(Color.FromArgb(255, 151, 92, 45), 2f))
            using (Pen rope = new Pen(Color.FromArgb(255, 190, 151, 88), 2f))
            {
                Point[][] members =
                {
                    new[] { new Point(left, top + 22), new Point(left, bottom) },
                    new[] { new Point(left + 18, top + 14), new Point(left + 18, bottom - 4) },
                    new[] { new Point(right - 18, top + 16), new Point(right - 18, bottom - 3) },
                    new[] { new Point(right, top + 24), new Point(right, bottom) },
                    new[] { new Point(left, top + 36), new Point(left + 28, top + 36) },
                    new[] { new Point(right - 28, top + 38), new Point(right, top + 38) }
                };
                foreach (Point[] member in members)
                {
                    graphics.DrawLine(outline, member[0], member[1]);
                    graphics.DrawLine(timber, member[0], member[1]);
                }

                graphics.DrawLine(rope, left, bottom - 8, left + 28, bottom - 8);
                graphics.DrawLine(rope, right - 28, bottom - 8, right, bottom - 8);
            }
        }
    }
}
'@
}

function Resolve-ProjectPath {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Label)
    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $script:ProjectRoot $Path }
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
    param([Parameter(Mandatory)][string]$Path, [string]$Expected, [Parameter(Mandatory)][string]$Label)
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
        throw 'One or more requested construction families are missing from the manifest.'
    }
}

foreach ($entry in $selected) {
    $sourceFrameWidth = [int]$entry.sourceFrame.size[0]
    $sourceFrameHeight = [int]$entry.sourceFrame.size[1]
    $sourcePpu = [float]$entry.sourceFrame.ppu
    $sourcePivotX = [float]$entry.sourceFrame.pivotPixelsBottomLeft[0]
    $sourcePivotY = [float]$entry.sourceFrame.pivotPixelsBottomLeft[1]
    $outputFrameWidth = [int]$entry.outputFrame.size[0]
    $outputFrameHeight = [int]$entry.outputFrame.size[1]
    $outputPpu = [float]$entry.outputFrame.ppu
    $outputPivotX = [float]$entry.outputFrame.pivotPixelsBottomLeft[0]
    $outputPivotY = [float]$entry.outputFrame.pivotPixelsBottomLeft[1]
    $frameCount = [int]$entry.frameCount

    foreach ($variant in @($entry.variants)) {
        $sourcePath = Resolve-ProjectPath $variant.source "$($entry.tool) $($variant.id) source"
        $finalPath = Resolve-ProjectPath $variant.finalSprite "$($entry.tool) $($variant.id) final"
        $outputPath = Resolve-ProjectPath $variant.output "$($entry.tool) $($variant.id) output"
        foreach ($required in @($sourcePath, $finalPath)) {
            if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
                throw "Required construction input is missing: $required"
            }
        }

        Assert-Hash $sourcePath $variant.sourceSha256 "Construction source $($entry.tool) $($variant.id)" | Out-Null
        Assert-Hash $finalPath $variant.finalSha256 "Construction final $($entry.tool) $($variant.id)" | Out-Null
        $finalPpu = [float]$variant.finalPpu
        $finalPivotX = [float]$variant.finalPivotNormalized[0]
        $finalPivotY = [float]$variant.finalPivotNormalized[1]
        $finalImage = [ProjectUnknown.Tools.Art.AuthoredConstructionAtlasBuilder]::OpenArgb($finalPath)
        try {
            $finalScale = $outputPpu / $finalPpu
            $finalPlacementX = [int][Math]::Round(
                $outputPivotX - $finalImage.Width * $finalPivotX * $finalScale)
            $finalPlacementY = [int][Math]::Round(
                $outputFrameHeight - $outputPivotY -
                ($finalImage.Height * $finalScale - $finalImage.Height * $finalPivotY * $finalScale))
        }
        finally {
            $finalImage.Dispose()
        }

        if ($ValidateOnly) {
            if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
                throw "Construction output does not exist for validation: $outputPath"
            }

            $stats = [ProjectUnknown.Tools.Art.AuthoredConstructionAtlasBuilder]::Validate(
                $outputPath, $sourcePath, $finalPath,
                $sourceFrameWidth, $sourceFrameHeight,
                $outputFrameWidth, $outputFrameHeight, $frameCount,
                $finalPlacementX, $finalPlacementY,
                $outputPpu, $finalPpu, $AlphaThreshold)
            $hash = Assert-Hash $outputPath $variant.expectedSha256 "Construction output $($entry.tool) $($variant.id)"
            Write-Host "PASS $($entry.tool)/$($variant.id) hash=$hash visible=$($stats[0]) detailDelta=$($stats[1])"
            continue
        }

        if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
            throw "Construction output already exists: $outputPath. Pass -Force to replace it."
        }

        [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
        $temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
        try {
            $placement = [ProjectUnknown.Tools.Art.AuthoredConstructionAtlasBuilder]::Build(
                $sourcePath, $finalPath,
                $sourceFrameWidth, $sourceFrameHeight, $frameCount,
                $sourcePpu, $sourcePivotX, $sourcePivotY,
                $outputFrameWidth, $outputFrameHeight,
                $outputPpu, $outputPivotX, $outputPivotY,
                $finalPpu, $finalPivotX, $finalPivotY,
                $temporaryPath)
            $stats = [ProjectUnknown.Tools.Art.AuthoredConstructionAtlasBuilder]::Validate(
                $temporaryPath, $sourcePath, $finalPath,
                $sourceFrameWidth, $sourceFrameHeight,
                $outputFrameWidth, $outputFrameHeight, $frameCount,
                $placement.X, $placement.Y,
                $outputPpu, $finalPpu, $AlphaThreshold)
            $hash = Assert-Hash $temporaryPath $variant.expectedSha256 "Construction output $($entry.tool) $($variant.id)"
            Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
            Write-Host "Built $($entry.tool)/$($variant.id) -> $outputPath"
            Write-Host "  hash=$hash finalPlacement=$($placement.X),$($placement.Y) visible=$($stats[0]) detailDelta=$($stats[1])"
        }
        finally {
            if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
                Remove-Item -LiteralPath $temporaryPath -Force
            }
        }
    }
}
