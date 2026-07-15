<#
.SYNOPSIS
Builds a deterministic geometry-plus-style reference sheet for image generation.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Tool,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$targetRoot = Join-Path $projectRoot "Assets/Resources/Visual/Baked/Buildings/$Tool"
if (-not (Test-Path -LiteralPath $targetRoot -PathType Container)) {
    throw "Baked building family does not exist: $targetRoot"
}

$targets = @(Get-ChildItem -LiteralPath $targetRoot -Filter '*.png' -File | Sort-Object Name)
if ($targets.Count -lt 1 -or $targets.Count -gt 6) {
    throw "Reference-sheet layout supports one to six target variants or animation frames; found $($targets.Count)."
}

$outputPath = if ([System.IO.Path]::IsPathRooted($Output)) {
    [System.IO.Path]::GetFullPath($Output)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $projectRoot $Output))
}
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null

$stylePaths = @(
    'Assets/Resources/Visual/Authored/Buildings/House/V01.png',
    'Assets/Resources/Visual/Authored/Buildings/House/V02.png',
    'Assets/Resources/Visual/Authored/Buildings/House/V04.png',
    'Assets/Resources/Visual/Authored/Buildings/ForagerCamp/V01.png')

$canvas = [System.Drawing.Bitmap]::new(
    2048,
    1024,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$font = [System.Drawing.Font]::new('Arial', 28, [System.Drawing.FontStyle]::Bold)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 55, 45, 37))
try {
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 224, 220, 207))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.DrawString("TARGET GEOMETRY - $($Tool.ToUpperInvariant())", $font, $brush, 44, 22)

    $targetCellWidth = [int][Math]::Floor(1980 / $targets.Count)
    for ($index = 0; $index -lt $targets.Count; $index++) {
        $image = [System.Drawing.Image]::FromFile($targets[$index].FullName)
        try {
            $tile = [System.Drawing.Rectangle]::new(
                48 + $index * $targetCellWidth,
                78,
                $targetCellWidth - 48,
                400)
            $graphics.FillRectangle([System.Drawing.Brushes]::WhiteSmoke, $tile)
            $scale = [Math]::Min(($tile.Width - 52.0) / $image.Width, 350.0 / $image.Height)
            $width = [int]($image.Width * $scale)
            $height = [int]($image.Height * $scale)
            $x = $tile.X + [int](($tile.Width - $width) / 2)
            $y = $tile.Y + [int](($tile.Height - $height) / 2)
            $graphics.DrawImage(
                $image,
                [System.Drawing.Rectangle]::new($x, $y, $width, $height),
                0,
                0,
                $image.Width,
                $image.Height,
                [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally {
            $image.Dispose()
        }
    }

    $graphics.DrawString('STYLE / MATERIAL DETAIL', $font, $brush, 44, 510)
    for ($index = 0; $index -lt $stylePaths.Count; $index++) {
        $stylePath = Join-Path $projectRoot $stylePaths[$index]
        $image = [System.Drawing.Image]::FromFile($stylePath)
        try {
            $tile = [System.Drawing.Rectangle]::new(48 + $index * 495, 568, 447, 396)
            $graphics.FillRectangle([System.Drawing.Brushes]::WhiteSmoke, $tile)
            $scale = [Math]::Min(410.0 / $image.Width, 350.0 / $image.Height)
            $width = [int]($image.Width * $scale)
            $height = [int]($image.Height * $scale)
            $x = $tile.X + [int](($tile.Width - $width) / 2)
            $y = $tile.Y + [int](($tile.Height - $height) / 2)
            $graphics.DrawImage(
                $image,
                [System.Drawing.Rectangle]::new($x, $y, $width, $height),
                0,
                0,
                $image.Width,
                $image.Height,
                [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally {
            $image.Dispose()
        }
    }

    $canvas.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Built reference sheet: $outputPath"
}
finally {
    $brush.Dispose()
    $font.Dispose()
    $graphics.Dispose()
    $canvas.Dispose()
}
