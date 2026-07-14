<#
.SYNOPSIS
Builds the 2x authored House and Forager Camp sprites from their detail masters.

.DESCRIPTION
The detail masters retain substantially more information than the 80x80 House
and 88x58 Forager Camp game sprites. This tool isolates the five House variants
and the single camp, resamples each master into a canvas that is exactly twice
the legacy size, and preserves the legacy visible bounds at exactly 2x.

Output is written below Buildings/House and Buildings/ForagerCamp. The tool
never overwrites its legacy alignment references and writes every PNG atomically.

.EXAMPLE
./Tools/Art/Build-HighResolutionAuthoredBuildings.ps1 `
    -HouseMaster ./Tools/Art/Source/HighResolution/House-Variants.png `
    -ForagerCampMaster ./Tools/Art/Source/HighResolution/ForagerCamp-V01.png `
    -OutputRoot ./tmp/high-resolution/Buildings `
    -Force
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$HouseMaster,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ForagerCampMaster,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputRoot,

    [ValidateRange(1, 254)]
    [int]$AlphaThreshold = 16,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ProjectUnknown.Tools.Art
{
    public static class HighResolutionBuildingBuilder
    {
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

        public static Rectangle[] FindComponents(
            Bitmap source,
            int alphaThreshold,
            int minimumPixels)
        {
            int width = source.Width;
            int height = source.Height;
            bool[] visited = new bool[width * height];
            Queue<int> queue = new Queue<int>();
            List<Rectangle> components = new List<Rectangle>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int start = y * width + x;
                    if (visited[start])
                    {
                        continue;
                    }

                    visited[start] = true;
                    if (source.GetPixel(x, y).A < alphaThreshold)
                    {
                        continue;
                    }

                    int left = x;
                    int top = y;
                    int right = x;
                    int bottom = y;
                    int pixelCount = 0;
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        int current = queue.Dequeue();
                        int currentX = current % width;
                        int currentY = current / width;
                        pixelCount++;
                        left = Math.Min(left, currentX);
                        top = Math.Min(top, currentY);
                        right = Math.Max(right, currentX);
                        bottom = Math.Max(bottom, currentY);

                        Visit(source, visited, queue, currentX - 1, currentY, alphaThreshold);
                        Visit(source, visited, queue, currentX + 1, currentY, alphaThreshold);
                        Visit(source, visited, queue, currentX, currentY - 1, alphaThreshold);
                        Visit(source, visited, queue, currentX, currentY + 1, alphaThreshold);
                    }

                    if (pixelCount >= minimumPixels)
                    {
                        components.Add(Rectangle.FromLTRB(left, top, right + 1, bottom + 1));
                    }
                }
            }

            return components.ToArray();
        }

        public static Rectangle Build(
            Bitmap master,
            Rectangle sourceBounds,
            int outputWidth,
            int outputHeight,
            Rectangle targetBounds,
            string outputPath)
        {
            if (targetBounds.Right > outputWidth || targetBounds.Bottom > outputHeight)
            {
                throw new InvalidOperationException("Target visible bounds exceed the output canvas.");
            }

            using (Bitmap output = new Bitmap(
                outputWidth,
                outputHeight,
                PixelFormat.Format32bppArgb))
            {
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
                        master,
                        targetBounds,
                        sourceBounds.X,
                        sourceBounds.Y,
                        sourceBounds.Width,
                        sourceBounds.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                }

                output.Save(outputPath, ImageFormat.Png);
            }

            return targetBounds;
        }

        private static void Visit(
            Bitmap source,
            bool[] visited,
            Queue<int> queue,
            int x,
            int y,
            int alphaThreshold)
        {
            if (x < 0 || y < 0 || x >= source.Width || y >= source.Height)
            {
                return;
            }

            int index = y * source.Width + x;
            if (visited[index])
            {
                return;
            }

            visited[index] = true;
            if (source.GetPixel(x, y).A >= alphaThreshold)
            {
                queue.Enqueue(index);
            }
        }
    }
}
'@
}

function Resolve-InputFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label does not exist: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Resolve-DirectoryPath {
    param([Parameter(Mandatory)][string]$Path)

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path (Get-Location).Path $Path
    }

    return [System.IO.Path]::GetFullPath($candidate)
}

function Write-NormalizedSprite {
    param(
        [Parameter(Mandatory)][System.Drawing.Bitmap]$Master,
        [Parameter(Mandatory)][System.Drawing.Rectangle]$SourceBounds,
        [Parameter(Mandatory)][int]$OutputWidth,
        [Parameter(Mandatory)][int]$OutputHeight,
        [Parameter(Mandatory)][System.Drawing.Rectangle]$TargetBounds,
        [Parameter(Mandatory)][string]$OutputPath
    )

    if ((Test-Path -LiteralPath $OutputPath -PathType Leaf) -and -not $Force) {
        throw "Output already exists: $OutputPath. Pass -Force to replace it."
    }

    [System.IO.Directory]::CreateDirectory(
        [System.IO.Path]::GetDirectoryName($OutputPath)) | Out-Null
    $temporaryPath = $OutputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
    try {
        $writtenBounds = [ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder]::Build(
            $Master,
            $SourceBounds,
            $OutputWidth,
            $OutputHeight,
            $TargetBounds,
            $temporaryPath)
        Move-Item -LiteralPath $temporaryPath -Destination $OutputPath -Force
        Write-Host "Built $OutputPath"
        Write-Host "  source: $SourceBounds"
        Write-Host "  target: $writtenBounds"
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

$houseMasterPath = Resolve-InputFile $HouseMaster 'House detail master'
$foragerMasterPath = Resolve-InputFile $ForagerCampMaster 'Forager Camp detail master'
$outputRootPath = Resolve-DirectoryPath $OutputRoot
$houseTargetBounds = @(
    [System.Drawing.Rectangle]::new(12, 8, 140, 142),
    [System.Drawing.Rectangle]::new(18, 8, 134, 142),
    [System.Drawing.Rectangle]::new(12, 8, 134, 142),
    [System.Drawing.Rectangle]::new(14, 8, 134, 144),
    [System.Drawing.Rectangle]::new(18, 10, 134, 142))

$houseBitmap = $null
$foragerBitmap = $null
try {
    $houseBitmap = [ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder]::OpenArgb(
        $houseMasterPath)
    $houseComponents = [ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder]::FindComponents(
        $houseBitmap,
        $AlphaThreshold,
        20000)
    if ($houseComponents.Count -ne 5) {
        throw "House detail master must contain exactly five large connected sprites; found $($houseComponents.Count)."
    }

    $houseComponents = @($houseComponents | Sort-Object Y, X)
    for ($variant = 1; $variant -le 5; $variant++) {
        $fileName = 'V{0:D2}.png' -f $variant
        Write-NormalizedSprite `
            -Master $houseBitmap `
            -SourceBounds $houseComponents[$variant - 1] `
            -OutputWidth 160 `
            -OutputHeight 160 `
            -TargetBounds $houseTargetBounds[$variant - 1] `
            -OutputPath (Join-Path $outputRootPath "House/$fileName")
    }

    $foragerBitmap = [ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder]::OpenArgb(
        $foragerMasterPath)
    $foragerComponents = [ProjectUnknown.Tools.Art.HighResolutionBuildingBuilder]::FindComponents(
        $foragerBitmap,
        $AlphaThreshold,
        20000)
    if ($foragerComponents.Count -ne 1) {
        throw "Forager Camp detail master must contain exactly one large connected sprite; found $($foragerComponents.Count)."
    }

    Write-NormalizedSprite `
        -Master $foragerBitmap `
        -SourceBounds $foragerComponents[0] `
        -OutputWidth 176 `
        -OutputHeight 116 `
        -TargetBounds ([System.Drawing.Rectangle]::new(6, 2, 162, 112)) `
        -OutputPath (Join-Path $outputRootPath 'ForagerCamp/V01.png')
}
finally {
    if ($null -ne $houseBitmap) {
        $houseBitmap.Dispose()
    }

    if ($null -ne $foragerBitmap) {
        $foragerBitmap.Dispose()
    }
}
