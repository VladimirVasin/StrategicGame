<#
.SYNOPSIS
Builds the seven-frame authored Forager Camp construction atlas.

.DESCRIPTION
The transparent source is a regular 4x2 storyboard. Stages 0-3 occupy the top
row and stages 4-6 the bottom row; the eighth cell is ignored. Stages 0-5 share
one nearest-neighbor scale and a bottom-center anchor. Stage 6 is replaced by
the exact 88x58 final camp plus deterministic removable scaffolding. Output is
always a 644x82 PNG containing seven 92x82 frames.

.EXAMPLE
./Tools/Art/Build-ForagerCampConstructionAtlas.ps1 `
    -SourceStoryboard ./tmp/imagegen/forager-construction-alpha.png `
    -FinalCamp ./Assets/Resources/Visual/Authored/Buildings/ForagerCamp/V01.png `
    -Output ./Assets/Resources/Visual/Authored/Construction/ForagerCamp/V01.png `
    -Force
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$SourceStoryboard,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$FinalCamp,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Output,

    [ValidateRange(0, 12)]
    [int]$TargetPadding = 5,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.ForagerStoryboardAlpha' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ProjectUnknown.Tools.Art
{
    public static class ForagerStoryboardAlpha
    {
        public static Rectangle FindBounds(Bitmap bitmap, Rectangle region)
        {
            BitmapData data = bitmap.LockBits(region, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = region.Width * 4;
                byte[] row = new byte[rowBytes];
                int minX = region.Width;
                int minY = region.Height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < region.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, rowBytes);
                    for (int x = 0; x < region.Width; x++)
                    {
                        if (row[x * 4 + 3] == 0)
                        {
                            continue;
                        }

                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }

                return maxX < minX || maxY < minY
                    ? Rectangle.Empty
                    : Rectangle.FromLTRB(
                        region.Left + minX,
                        region.Top + minY,
                        region.Left + maxX + 1,
                        region.Top + maxY + 1);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
'@
}

$FrameWidth = 92
$FrameHeight = 82
$FrameCount = 7
$GeneratedStageCount = 6
$FinalCampWidth = 88
$FinalCampHeight = 58
$AtlasWidth = $FrameWidth * $FrameCount

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

function Open-ArgbBitmap {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    $source = $null
    $bitmap = $null
    try {
        $source = [System.Drawing.Image]::FromFile($Path)
        $bitmap = [System.Drawing.Bitmap]::new(
            $source.Width,
            $source.Height,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.DrawImageUnscaled($source, 0, 0)
        }
        finally {
            $graphics.Dispose()
        }

        return $bitmap
    }
    catch {
        if ($null -ne $bitmap) {
            $bitmap.Dispose()
        }

        throw "Failed to load $Label PNG '$Path': $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $source) {
            $source.Dispose()
        }
    }
}

function Get-StoryboardCellRectangle {
    param(
        [Parameter(Mandatory)][System.Drawing.Bitmap]$Storyboard,
        [Parameter(Mandatory)][int]$Stage
    )

    $column = $Stage % 4
    $row = [Math]::Floor($Stage / 4)
    $left = [int][Math]::Round($column * $Storyboard.Width / 4.0)
    $right = [int][Math]::Round(($column + 1) * $Storyboard.Width / 4.0)
    $top = [int][Math]::Round($row * $Storyboard.Height / 2.0)
    $bottom = [int][Math]::Round(($row + 1) * $Storyboard.Height / 2.0)
    return [System.Drawing.Rectangle]::FromLTRB($left, $top, $right, $bottom)
}

function Set-PixelGraphics {
    param([Parameter(Mandatory)][System.Drawing.Graphics]$Graphics)

    $Graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $Graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
}

function Draw-FinalScaffold {
    param([Parameter(Mandatory)][System.Drawing.Graphics]$Graphics)

    $originX = 6 * $FrameWidth
    $outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 55, 35, 23), 3)
    $timber = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 151, 92, 45), 1)
    $highlight = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 209, 145, 72), 1)
    $rope = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 190, 151, 88), 1)
    try {
        $members = @(
            @(3, 30, 3, 80), @(15, 27, 15, 76),
            @(2, 39, 24, 39), @(2, 53, 24, 53), @(3, 77, 15, 31),
            @(77, 30, 77, 79), @(89, 34, 89, 77),
            @(76, 43, 90, 43), @(76, 57, 90, 57)
        )
        foreach ($member in $members) {
            $Graphics.DrawLine(
                $outline,
                $originX + $member[0],
                $member[1],
                $originX + $member[2],
                $member[3])
            $Graphics.DrawLine(
                $timber,
                $originX + $member[0],
                $member[1],
                $originX + $member[2],
                $member[3])
        }

        foreach ($ladderY in 43, 50, 57, 64, 71) {
            $Graphics.DrawLine($highlight, $originX + 77, $ladderY, $originX + 89, $ladderY)
        }

        $Graphics.DrawLine($rope, $originX + 2, 74, $originX + 23, 74)
    }
    finally {
        $outline.Dispose()
        $timber.Dispose()
        $highlight.Dispose()
        $rope.Dispose()
    }
}

$storyboardPath = Resolve-InputPngPath $SourceStoryboard 'Source storyboard'
$finalCampPath = Resolve-InputPngPath $FinalCamp 'Final Forager Camp'
$outputPath = Resolve-OutputPngPath $Output
if ($outputPath.Equals($storyboardPath, [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputPath.Equals($finalCampPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output must not overwrite an input PNG.'
}

if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
    throw "Output already exists: $outputPath. Pass -Force to replace it."
}

$outputDirectory = [System.IO.Path]::GetDirectoryName($outputPath)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
$storyboard = $null
$finalCampBitmap = $null
$atlas = $null
try {
    $storyboard = Open-ArgbBitmap $storyboardPath 'source storyboard'
    $finalCampBitmap = Open-ArgbBitmap $finalCampPath 'final Forager Camp'
    if ($storyboard.Width -lt 8 -or $storyboard.Height -lt 4) {
        throw "Storyboard is too small: $($storyboard.Width)x$($storyboard.Height)."
    }

    if ($finalCampBitmap.Width -ne $FinalCampWidth -or $finalCampBitmap.Height -ne $FinalCampHeight) {
        throw "Final Forager Camp must be ${FinalCampWidth}x${FinalCampHeight}; got $($finalCampBitmap.Width)x$($finalCampBitmap.Height)."
    }

    $stageBounds = [System.Collections.Generic.List[System.Drawing.Rectangle]]::new()
    $maxWidth = 0
    $maxHeight = 0
    for ($stage = 0; $stage -lt $GeneratedStageCount; $stage++) {
        $cell = Get-StoryboardCellRectangle $storyboard $stage
        $bounds = [ProjectUnknown.Tools.Art.ForagerStoryboardAlpha]::FindBounds($storyboard, $cell)
        if ($bounds.IsEmpty) {
            throw "Storyboard stage $stage has no visible pixels."
        }

        if ($bounds.Equals($cell)) {
            throw "Storyboard stage $stage fills its cell; remove the chroma background first."
        }

        $stageBounds.Add($bounds)
        $maxWidth = [Math]::Max($maxWidth, $bounds.Width)
        $maxHeight = [Math]::Max($maxHeight, $bounds.Height)
    }

    $availableWidth = $FrameWidth - 2 * $TargetPadding
    $availableHeight = $FinalCampHeight
    $sharedScale = [Math]::Min(
        $availableWidth / [double]$maxWidth,
        $availableHeight / [double]$maxHeight)
    if ($sharedScale -le 0) {
        throw 'Could not calculate a positive storyboard scale.'
    }

    $atlas = [System.Drawing.Bitmap]::new(
        $AtlasWidth,
        $FrameHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($atlas)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        Set-PixelGraphics $graphics
        for ($stage = 0; $stage -lt $GeneratedStageCount; $stage++) {
            $source = $stageBounds[$stage]
            $targetWidth = [Math]::Max(1, [int][Math]::Round($source.Width * $sharedScale))
            $targetHeight = [Math]::Max(1, [int][Math]::Round($source.Height * $sharedScale))
            $targetX = $stage * $FrameWidth + [Math]::Floor(($FrameWidth - $targetWidth) / 2)
            $targetY = $FrameHeight - $targetHeight
            $target = [System.Drawing.Rectangle]::new($targetX, $targetY, $targetWidth, $targetHeight)
            $graphics.DrawImage(
                $storyboard,
                $target,
                $source.X,
                $source.Y,
                $source.Width,
                $source.Height,
                [System.Drawing.GraphicsUnit]::Pixel)
        }

        Draw-FinalScaffold $graphics
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
        $finalX = 6 * $FrameWidth + [Math]::Floor(($FrameWidth - $FinalCampWidth) / 2)
        $finalY = $FrameHeight - $FinalCampHeight
        $graphics.DrawImageUnscaled($finalCampBitmap, $finalX, $finalY)
    }
    finally {
        $graphics.Dispose()
    }

    $atlas.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $validation = Open-ArgbBitmap $temporaryPath 'assembled atlas'
    try {
        if ($validation.Width -ne $AtlasWidth -or $validation.Height -ne $FrameHeight) {
            throw "Assembled atlas dimensions changed: $($validation.Width)x$($validation.Height)."
        }

        $finalX = 6 * $FrameWidth + 2
        $finalY = $FrameHeight - $FinalCampHeight
        for ($y = 0; $y -lt $FinalCampHeight; $y++) {
            for ($x = 0; $x -lt $FinalCampWidth; $x++) {
                $expected = $finalCampBitmap.GetPixel($x, $y)
                if ($expected.A -eq 0) {
                    continue
                }

                $actual = $validation.GetPixel($finalX + $x, $finalY + $y)
                if ($actual.ToArgb() -ne $expected.ToArgb()) {
                    throw "Final Forager Camp pixel changed at ($x,$y)."
                }
            }
        }
    }
    finally {
        $validation.Dispose()
    }

    Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
    Write-Host "Built Forager Camp construction atlas: $outputPath"
    Write-Host "Layout: ${AtlasWidth}x${FrameHeight}, 7 frames of ${FrameWidth}x${FrameHeight}"
    Write-Host ('Shared nearest-neighbor scale: {0:F4}; final camp embedded at (2,24)' -f $sharedScale)
}
finally {
    if ($null -ne $storyboard) {
        $storyboard.Dispose()
    }

    if ($null -ne $finalCampBitmap) {
        $finalCampBitmap.Dispose()
    }

    if ($null -ne $atlas) {
        $atlas.Dispose()
    }

    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}
