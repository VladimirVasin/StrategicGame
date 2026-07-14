<#
.SYNOPSIS
Builds a seven-frame House construction atlas from a transparent 4x2 storyboard.

.DESCRIPTION
The storyboard must be an already-keyed 1536x1024 PNG with exact 384x512 cells.
Stages 0-3 occupy the top row and stages 4-6 the bottom row. Stages 0-5 use one
shared nearest-neighbor scale and are bottom-center aligned inside 92x82 cells.
Stage 6 is replaced by the exact final 80x80 House, bottom-aligned at local
x = 6, with optional deterministic scaffolding. The output is always a 644x82 PNG.

.EXAMPLE
./Tools/Art/Build-HouseConstructionAtlas.ps1 `
    -SourceStoryboard ./tmp/V01-transparent.png `
    -FinalHouse ./Assets/Resources/Visual/Authored/Buildings/House/V01.png `
    -Output ./Assets/Resources/Visual/Authored/Construction/House/V01.png `
    -Force
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [Alias('StoryboardPath')]
    [ValidateNotNullOrEmpty()]
    [string]$SourceStoryboard,

    [Parameter(Mandatory)]
    [Alias('OutputPath')]
    [ValidateNotNullOrEmpty()]
    [string]$Output,

    [Parameter(Mandatory)]
    [Alias('FinalHousePath')]
    [ValidateNotNullOrEmpty()]
    [string]$FinalHouse,

    [ValidateSet('FinalOnly', 'DeterministicScaffold')]
    [string]$Stage6Mode = 'DeterministicScaffold',

    [ValidateRange(0, 12)]
    [int]$TargetPadding = 5,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.HouseStoryboardAlpha' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ProjectUnknown.Tools.Art
{
    public static class HouseStoryboardAlpha
    {
        public static Rectangle FindBounds(Bitmap bitmap, Rectangle region)
        {
            BitmapData data = bitmap.LockBits(
                region,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = region.Width * 4;
                byte[] rowPixels = new byte[rowBytes];
                int minX = region.Width;
                int minY = region.Height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < region.Height; y++)
                {
                    IntPtr rowPointer = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(rowPointer, rowPixels, 0, rowBytes);
                    for (int x = 0; x < region.Width; x++)
                    {
                        if (rowPixels[x * 4 + 3] == 0)
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

$StoryboardWidth = 1536
$StoryboardHeight = 1024
$SourceCellWidth = 384
$SourceCellHeight = 512
$FrameWidth = 92
$FrameHeight = 82
$FrameCount = 7
$GeneratedStageCount = 6
$AtlasWidth = $FrameWidth * $FrameCount
$AtlasHeight = $FrameHeight
$FinalHouseWidth = 80
$FinalHouseHeight = 80

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
    param([Parameter(Mandatory)][int]$Stage)

    $column = $Stage % 4
    $row = [Math]::Floor($Stage / 4)
    return [System.Drawing.Rectangle]::new(
        $column * $SourceCellWidth,
        $row * $SourceCellHeight,
        $SourceCellWidth,
        $SourceCellHeight)
}

function Set-PixelGraphics {
    param([Parameter(Mandatory)][System.Drawing.Graphics]$Graphics)

    $Graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $Graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
}

function Draw-Stage6Scaffold {
    param([Parameter(Mandatory)][System.Drawing.Graphics]$Graphics)

    $originX = 6 * $FrameWidth
    $outlinePen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 55, 35, 23),
        3)
    $timberPen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 151, 92, 45),
        1)
    $highlightPen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 209, 145, 72),
        1)
    $ropePen = [System.Drawing.Pen]::new(
        [System.Drawing.Color]::FromArgb(255, 190, 151, 88),
        1)
    try {
        $members = @(
            @(4, 39, 4, 78), @(19, 34, 19, 75),
            @(3, 46, 27, 46), @(3, 58, 27, 58), @(3, 70, 25, 70),
            @(4, 76, 19, 42), @(4, 42, 19, 71),
            @(79, 41, 79, 77), @(87, 37, 87, 74)
        )
        foreach ($member in $members) {
            $Graphics.DrawLine(
                $outlinePen,
                $originX + $member[0],
                $member[1],
                $originX + $member[2],
                $member[3])
            $Graphics.DrawLine(
                $timberPen,
                $originX + $member[0],
                $member[1],
                $originX + $member[2],
                $member[3])
        }

        foreach ($ladderY in 46, 53, 60, 67) {
            $Graphics.DrawLine(
                $highlightPen,
                $originX + 79,
                $ladderY,
                $originX + 87,
                $ladderY)
        }

        $Graphics.DrawLine($ropePen, $originX + 3, 73, $originX + 26, 73)
    }
    finally {
        $outlinePen.Dispose()
        $timberPen.Dispose()
        $highlightPen.Dispose()
        $ropePen.Dispose()
    }
}

$storyboardPath = Resolve-InputPngPath $SourceStoryboard 'Source storyboard'
$finalHousePath = Resolve-InputPngPath $FinalHouse 'Final House'
$outputPath = Resolve-OutputPngPath $Output
if ($outputPath.Equals($storyboardPath, [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputPath.Equals($finalHousePath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output must not overwrite the source storyboard or final House PNG.'
}

if ((Test-Path -LiteralPath $outputPath -PathType Leaf) -and -not $Force) {
    throw "Output already exists: $outputPath. Pass -Force to replace it."
}

$outputDirectory = [System.IO.Path]::GetDirectoryName($outputPath)
if (-not [string]::IsNullOrEmpty($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$storyboardBitmap = $null
$finalHouseBitmap = $null
$atlasBitmap = $null
$temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
try {
    $storyboardBitmap = Open-ArgbBitmap $storyboardPath 'source storyboard'
    $finalHouseBitmap = Open-ArgbBitmap $finalHousePath 'final House'
    if ($storyboardBitmap.Width -ne $StoryboardWidth -or
        $storyboardBitmap.Height -ne $StoryboardHeight) {
        throw "Storyboard must be exactly ${StoryboardWidth}x${StoryboardHeight}; got $($storyboardBitmap.Width)x$($storyboardBitmap.Height)."
    }

    if ($finalHouseBitmap.Width -ne $FinalHouseWidth -or
        $finalHouseBitmap.Height -ne $FinalHouseHeight) {
        throw "Final House must be exactly ${FinalHouseWidth}x${FinalHouseHeight}; got $($finalHouseBitmap.Width)x$($finalHouseBitmap.Height)."
    }

    $stageBounds = [System.Collections.Generic.List[System.Drawing.Rectangle]]::new()
    $maxWidth = 0
    $maxHeight = 0
    for ($stage = 0; $stage -lt $GeneratedStageCount; $stage++) {
        $cell = Get-StoryboardCellRectangle $stage
        $bounds = [ProjectUnknown.Tools.Art.HouseStoryboardAlpha]::FindBounds(
            $storyboardBitmap,
            $cell)
        if ($bounds.IsEmpty) {
            throw "Storyboard stage $stage has no visible pixels."
        }

        if ($bounds.Equals($cell)) {
            throw "Storyboard stage $stage fills its complete 384x512 cell. The chroma background probably has not been removed."
        }

        $stageBounds.Add($bounds)
        $maxWidth = [Math]::Max($maxWidth, $bounds.Width)
        $maxHeight = [Math]::Max($maxHeight, $bounds.Height)
    }

    $availableWidth = $FrameWidth - 2 * $TargetPadding
    $availableHeight = $FrameHeight - 2 * $TargetPadding
    $sharedScale = [Math]::Min(
        $availableWidth / [double]$maxWidth,
        $availableHeight / [double]$maxHeight)
    if ($sharedScale -le 0) {
        throw 'Could not calculate a positive shared scale from the alpha bounds.'
    }

    $atlasBitmap = [System.Drawing.Bitmap]::new(
        $AtlasWidth,
        $AtlasHeight,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($atlasBitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        Set-PixelGraphics $graphics
        for ($stage = 0; $stage -lt $GeneratedStageCount; $stage++) {
            $source = $stageBounds[$stage]
            $targetWidth = [Math]::Max(1, [int][Math]::Round($source.Width * $sharedScale))
            $targetHeight = [Math]::Max(1, [int][Math]::Round($source.Height * $sharedScale))
            $targetX = $stage * $FrameWidth + [Math]::Floor(($FrameWidth - $targetWidth) / 2)
            $targetY = $FrameHeight - $TargetPadding - $targetHeight
            $target = [System.Drawing.Rectangle]::new(
                $targetX,
                $targetY,
                $targetWidth,
                $targetHeight)
            $graphics.DrawImage(
                $storyboardBitmap,
                $target,
                $source.X,
                $source.Y,
                $source.Width,
                $source.Height,
                [System.Drawing.GraphicsUnit]::Pixel)
        }

        if ($Stage6Mode -eq 'DeterministicScaffold') {
            Draw-Stage6Scaffold $graphics
        }

        # SourceOver preserves the scaffold in transparent House pixels while every
        # visible accepted House pixel remains unchanged.
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
        $finalHouseX = 6 * $FrameWidth + 6
        $finalHouseY = $FrameHeight - $finalHouseBitmap.Height
        $graphics.DrawImageUnscaled($finalHouseBitmap, $finalHouseX, $finalHouseY)
    }
    finally {
        $graphics.Dispose()
    }

    $atlasBitmap.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $validation = Open-ArgbBitmap $temporaryPath 'assembled atlas'
    try {
        if ($validation.Width -ne $AtlasWidth -or $validation.Height -ne $AtlasHeight) {
            throw "Assembled atlas dimensions changed unexpectedly: $($validation.Width)x$($validation.Height)."
        }
    }
    finally {
        $validation.Dispose()
    }

    Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
    Write-Host "Built House construction atlas: $outputPath"
    Write-Host "Layout: ${AtlasWidth}x${AtlasHeight}, 7 frames of ${FrameWidth}x${FrameHeight}"
    Write-Host ('Shared nearest-neighbor scale: {0:F4}; stage 6: exact House at (6,{1}), {2}' -f `
        $sharedScale,
        ($FrameHeight - $finalHouseBitmap.Height),
        $Stage6Mode)
}
finally {
    if ($null -ne $storyboardBitmap) {
        $storyboardBitmap.Dispose()
    }

    if ($null -ne $finalHouseBitmap) {
        $finalHouseBitmap.Dispose()
    }

    if ($null -ne $atlasBitmap) {
        $atlasBitmap.Dispose()
    }

    if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}
