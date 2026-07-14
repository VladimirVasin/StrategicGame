<#
.SYNOPSIS
Upgrades one authored seven-frame construction atlas to the 2x visual contract.

.DESCRIPTION
Stages 0-5 are enlarged independently with deterministic Scale2x edge rules, so
diagonals and corners gain real sub-pixel structure without blending or frame
bleed. Stage 6 is rebuilt from the canonical scaffold at exactly 2x scale and
the accepted high-resolution final sprite is copied over it without blending.
PNG round-trips are validated in premultiplied-alpha space so anti-aliased edge
pixels remain visually identical despite harmless codec rounding.

Input is always a 644x82 atlas with seven 92x82 frames. Output is always a
1288x164 atlas with seven 184x164 frames. House finals must be 160x160; Forager
Camp finals must be 176x116. The script writes atomically and never modifies an
input file.

.EXAMPLE
./Tools/Art/Upgrade-ConstructionAtlas2x.ps1 `
    -BuildingType House `
    -SourceAtlas ./Tools/Art/Source/HighResolution/Construction1x/House/V01.png `
    -FinalSprite ./tmp/high-resolution/House-V01.png `
    -Output ./tmp/high-resolution/Construction/House/V01.png `
    -Force

.EXAMPLE
./Tools/Art/Upgrade-ConstructionAtlas2x.ps1 `
    -BuildingType ForagerCamp `
    -SourceAtlas ./Tools/Art/Source/HighResolution/Construction1x/ForagerCamp/V01.png `
    -FinalSprite ./tmp/high-resolution/ForagerCamp-V01.png `
    -Output ./tmp/high-resolution/Construction/ForagerCamp/V01.png `
    -Force
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('House', 'ForagerCamp')]
    [string]$BuildingType,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$SourceAtlas,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$FinalSprite,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Output,

    [ValidateRange(0, 64)]
    [int]$SimilarityThreshold = 20,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.ConstructionAtlas2x' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProjectUnknown.Tools.Art
{
    public static class ConstructionAtlas2x
    {
        public const int SourceFrameWidth = 92;
        public const int SourceFrameHeight = 82;
        public const int FrameCount = 7;
        public const int OutputFrameWidth = 184;
        public const int OutputFrameHeight = 164;

        public static Bitmap OpenArgb(string path)
        {
            using (Image source = Image.FromFile(path))
            {
                Bitmap copy = new Bitmap(
                    source.Width,
                    source.Height,
                    PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(copy))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImageUnscaled(source, 0, 0);
                }

                return copy;
            }
        }

        public static long ScaleFrame(
            Bitmap source,
            int frameIndex,
            Bitmap output,
            int similarityThreshold)
        {
            int sourceX = frameIndex * SourceFrameWidth;
            int outputX = frameIndex * OutputFrameWidth;
            long changedFromNearest = 0;
            for (int y = 0; y < SourceFrameHeight; y++)
            {
                for (int x = 0; x < SourceFrameWidth; x++)
                {
                    Color center = Read(source, sourceX, x, y);
                    Color top = Read(source, sourceX, x, y - 1);
                    Color left = Read(source, sourceX, x - 1, y);
                    Color right = Read(source, sourceX, x + 1, y);
                    Color bottom = Read(source, sourceX, x, y + 1);

                    Color topLeft = center;
                    Color topRight = center;
                    Color bottomLeft = center;
                    Color bottomRight = center;
                    if (!Similar(top, bottom, similarityThreshold) &&
                        !Similar(left, right, similarityThreshold))
                    {
                        if (Similar(left, top, similarityThreshold))
                        {
                            topLeft = PickClosest(left, top, center);
                        }

                        if (Similar(top, right, similarityThreshold))
                        {
                            topRight = PickClosest(top, right, center);
                        }

                        if (Similar(left, bottom, similarityThreshold))
                        {
                            bottomLeft = PickClosest(left, bottom, center);
                        }

                        if (Similar(bottom, right, similarityThreshold))
                        {
                            bottomRight = PickClosest(bottom, right, center);
                        }
                    }

                    int targetX = outputX + x * 2;
                    int targetY = y * 2;
                    changedFromNearest += Write(output, targetX, targetY, topLeft, center);
                    changedFromNearest += Write(output, targetX + 1, targetY, topRight, center);
                    changedFromNearest += Write(output, targetX, targetY + 1, bottomLeft, center);
                    changedFromNearest += Write(output, targetX + 1, targetY + 1, bottomRight, center);
                }
            }

            return changedFromNearest;
        }

        public static void DrawDoubledScaffold(Bitmap output, string buildingType)
        {
            using (Bitmap scaffold = new Bitmap(
                SourceFrameWidth,
                SourceFrameHeight,
                PixelFormat.Format32bppArgb))
            {
                using (Graphics graphics = Graphics.FromImage(scaffold))
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half;
                    graphics.SmoothingMode = SmoothingMode.None;
                    if (String.Equals(buildingType, "House", StringComparison.Ordinal))
                    {
                        DrawHouseScaffold(graphics);
                    }
                    else
                    {
                        DrawForagerScaffold(graphics);
                    }
                }

                int outputX = 6 * OutputFrameWidth;
                for (int y = 0; y < SourceFrameHeight; y++)
                {
                    for (int x = 0; x < SourceFrameWidth; x++)
                    {
                        Color pixel = scaffold.GetPixel(x, y);
                        int targetX = outputX + x * 2;
                        int targetY = y * 2;
                        output.SetPixel(targetX, targetY, pixel);
                        output.SetPixel(targetX + 1, targetY, pixel);
                        output.SetPixel(targetX, targetY + 1, pixel);
                        output.SetPixel(targetX + 1, targetY + 1, pixel);
                    }
                }
            }
        }

        public static void CopyVisibleFinal(
            Bitmap finalSprite,
            Bitmap output,
            int localX,
            int localY)
        {
            int outputX = 6 * OutputFrameWidth + localX;
            for (int y = 0; y < finalSprite.Height; y++)
            {
                for (int x = 0; x < finalSprite.Width; x++)
                {
                    Color pixel = finalSprite.GetPixel(x, y);
                    if (pixel.A > 0)
                    {
                        output.SetPixel(outputX + x, localY + y, pixel);
                    }
                }
            }
        }

        public static void ValidateFinal(
            Bitmap finalSprite,
            Bitmap output,
            int localX,
            int localY)
        {
            int outputX = 6 * OutputFrameWidth + localX;
            for (int y = 0; y < finalSprite.Height; y++)
            {
                for (int x = 0; x < finalSprite.Width; x++)
                {
                    Color expected = finalSprite.GetPixel(x, y);
                    if (expected.A == 0)
                    {
                        continue;
                    }

                    Color actual = output.GetPixel(outputX + x, localY + y);
                    if (!EquivalentVisiblePixel(expected, actual))
                    {
                        throw new InvalidOperationException(String.Format(
                            "Final sprite pixel changed at ({0},{1}).",
                            x,
                            y));
                    }
                }
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
                Math.Abs(expected.R * expected.A - actual.R * actual.A) <= 255 &&
                Math.Abs(expected.G * expected.A - actual.G * actual.A) <= 255 &&
                Math.Abs(expected.B * expected.A - actual.B * actual.A) <= 255;
        }

        private static Color Read(Bitmap source, int frameX, int x, int y)
        {
            if (x < 0 || x >= SourceFrameWidth || y < 0 || y >= SourceFrameHeight)
            {
                return Color.Transparent;
            }

            return source.GetPixel(frameX + x, y);
        }

        private static bool Similar(Color first, Color second, int threshold)
        {
            if (first.A == 0 || second.A == 0)
            {
                return first.A == 0 && second.A == 0;
            }

            int alphaDifference = Math.Abs(first.A - second.A);
            if (alphaDifference > threshold)
            {
                return false;
            }

            int redDifference = first.R - second.R;
            int greenDifference = first.G - second.G;
            int blueDifference = first.B - second.B;
            int distanceSquared =
                redDifference * redDifference +
                greenDifference * greenDifference +
                blueDifference * blueDifference;
            return distanceSquared <= threshold * threshold * 3;
        }

        private static Color PickClosest(Color first, Color second, Color center)
        {
            return DistanceSquared(first, center) <= DistanceSquared(second, center)
                ? first
                : second;
        }

        private static int DistanceSquared(Color first, Color second)
        {
            int alphaDifference = first.A - second.A;
            int redDifference = first.R - second.R;
            int greenDifference = first.G - second.G;
            int blueDifference = first.B - second.B;
            return
                alphaDifference * alphaDifference +
                redDifference * redDifference +
                greenDifference * greenDifference +
                blueDifference * blueDifference;
        }

        private static int Write(
            Bitmap output,
            int x,
            int y,
            Color value,
            Color nearestValue)
        {
            output.SetPixel(x, y, value);
            return value.ToArgb() == nearestValue.ToArgb() ? 0 : 1;
        }

        private static void DrawHouseScaffold(Graphics graphics)
        {
            int[][] members =
            {
                new[] { 4, 39, 4, 78 }, new[] { 19, 34, 19, 75 },
                new[] { 3, 46, 27, 46 }, new[] { 3, 58, 27, 58 },
                new[] { 3, 70, 25, 70 }, new[] { 4, 76, 19, 42 },
                new[] { 4, 42, 19, 71 }, new[] { 79, 41, 79, 77 },
                new[] { 87, 37, 87, 74 }
            };
            DrawMembers(graphics, members);
            using (Pen highlight = new Pen(Color.FromArgb(255, 209, 145, 72), 1))
            using (Pen rope = new Pen(Color.FromArgb(255, 190, 151, 88), 1))
            {
                foreach (int y in new[] { 46, 53, 60, 67 })
                {
                    graphics.DrawLine(highlight, 79, y, 87, y);
                }

                graphics.DrawLine(rope, 3, 73, 26, 73);
            }
        }

        private static void DrawForagerScaffold(Graphics graphics)
        {
            int[][] members =
            {
                new[] { 3, 30, 3, 80 }, new[] { 15, 27, 15, 76 },
                new[] { 2, 39, 24, 39 }, new[] { 2, 53, 24, 53 },
                new[] { 3, 77, 15, 31 }, new[] { 77, 30, 77, 79 },
                new[] { 89, 34, 89, 77 }, new[] { 76, 43, 90, 43 },
                new[] { 76, 57, 90, 57 }
            };
            DrawMembers(graphics, members);
            using (Pen highlight = new Pen(Color.FromArgb(255, 209, 145, 72), 1))
            using (Pen rope = new Pen(Color.FromArgb(255, 190, 151, 88), 1))
            {
                foreach (int y in new[] { 43, 50, 57, 64, 71 })
                {
                    graphics.DrawLine(highlight, 77, y, 89, y);
                }

                graphics.DrawLine(rope, 2, 74, 23, 74);
            }
        }

        private static void DrawMembers(Graphics graphics, int[][] members)
        {
            using (Pen outline = new Pen(Color.FromArgb(255, 55, 35, 23), 3))
            using (Pen timber = new Pen(Color.FromArgb(255, 151, 92, 45), 1))
            {
                foreach (int[] member in members)
                {
                    graphics.DrawLine(
                        outline,
                        member[0],
                        member[1],
                        member[2],
                        member[3]);
                    graphics.DrawLine(
                        timber,
                        member[0],
                        member[1],
                        member[2],
                        member[3]);
                }
            }
        }
    }
}
'@
}

$SourceWidth = 644
$SourceHeight = 82
$OutputWidth = 1288
$OutputHeight = 164
$GeneratedStageCount = 6

function Resolve-InputPngPath {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label PNG does not exist: $Path"
    }

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if (-not [System.IO.Path]::GetExtension($resolved).Equals(
            '.png',
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must be a PNG file: $resolved"
    }

    return $resolved
}

function Resolve-OutputPngPath {
    param([Parameter(Mandatory)][string]$Path)

    if (-not [System.IO.Path]::GetExtension($Path).Equals(
            '.png',
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Output must use a .png extension: $Path"
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path (Get-Location).Path $Path
    }

    return [System.IO.Path]::GetFullPath($candidate)
}

$sourceAtlasPath = Resolve-InputPngPath $SourceAtlas 'Source atlas'
$finalSpritePath = Resolve-InputPngPath $FinalSprite 'Final sprite'
$outputPath = Resolve-OutputPngPath $Output
if ($outputPath.Equals($sourceAtlasPath, [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputPath.Equals($finalSpritePath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output must not overwrite an input PNG.'
}

if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
    throw "Output already exists: $outputPath. Pass -Force to replace it."
}

$expectedFinalWidth = if ($BuildingType -eq 'House') { 160 } else { 176 }
$expectedFinalHeight = if ($BuildingType -eq 'House') { 160 } else { 116 }
$finalLocalX = if ($BuildingType -eq 'House') { 12 } else { 4 }
$finalLocalY = if ($BuildingType -eq 'House') { 4 } else { 48 }

$outputDirectory = [System.IO.Path]::GetDirectoryName($outputPath)
if (-not [string]::IsNullOrEmpty($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
$sourceBitmap = $null
$finalBitmap = $null
$outputBitmap = $null
try {
    $sourceBitmap = [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::OpenArgb($sourceAtlasPath)
    $finalBitmap = [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::OpenArgb($finalSpritePath)
    if ($sourceBitmap.Width -ne $SourceWidth -or $sourceBitmap.Height -ne $SourceHeight) {
        throw "Source atlas must be exactly ${SourceWidth}x${SourceHeight}; got $($sourceBitmap.Width)x$($sourceBitmap.Height)."
    }

    if ($finalBitmap.Width -ne $expectedFinalWidth -or
        $finalBitmap.Height -ne $expectedFinalHeight) {
        throw "Final $BuildingType must be exactly ${expectedFinalWidth}x${expectedFinalHeight}; got $($finalBitmap.Width)x$($finalBitmap.Height)."
    }

    $outputBitmap = [System.Drawing.Bitmap]::new(
        $OutputWidth,
        $OutputHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $changedByStage = [System.Collections.Generic.List[long]]::new()
    for ($stage = 0; $stage -lt $GeneratedStageCount; $stage++) {
        $changed = [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::ScaleFrame(
            $sourceBitmap,
            $stage,
            $outputBitmap,
            $SimilarityThreshold)
        $changedByStage.Add($changed)
    }

    [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::DrawDoubledScaffold(
        $outputBitmap,
        $BuildingType)
    [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::CopyVisibleFinal(
        $finalBitmap,
        $outputBitmap,
        $finalLocalX,
        $finalLocalY)
    [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::ValidateFinal(
        $finalBitmap,
        $outputBitmap,
        $finalLocalX,
        $finalLocalY)

    $outputBitmap.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $validation = [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::OpenArgb($temporaryPath)
    try {
        if ($validation.Width -ne $OutputWidth -or $validation.Height -ne $OutputHeight) {
            throw "Output dimensions changed unexpectedly: $($validation.Width)x$($validation.Height)."
        }

        [ProjectUnknown.Tools.Art.ConstructionAtlas2x]::ValidateFinal(
            $finalBitmap,
            $validation,
            $finalLocalX,
            $finalLocalY)
    }
    finally {
        $validation.Dispose()
    }

    Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
    $changedTotal = ($changedByStage | Measure-Object -Sum).Sum
    Write-Host "Built 2x $BuildingType construction atlas: $outputPath"
    Write-Host "Layout: ${OutputWidth}x${OutputHeight}, 7 frames of 184x164"
    Write-Host "Stage 0-5 Scale2x changes versus nearest: $($changedByStage -join ', ') (total $changedTotal)"
    Write-Host "Stage 6 final: unblended visible pixels at local ($finalLocalX,$finalLocalY); canonical scaffold doubled exactly"
    if ($changedTotal -eq 0) {
        Write-Warning 'Scale2x found no eligible corners; stages 0-5 are equivalent to nearest-neighbor output.'
    }
}
finally {
    if ($null -ne $sourceBitmap) {
        $sourceBitmap.Dispose()
    }

    if ($null -ne $finalBitmap) {
        $finalBitmap.Dispose()
    }

    if ($null -ne $outputBitmap) {
        $outputBitmap.Dispose()
    }

    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}
