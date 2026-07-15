<#
.SYNOPSIS
Builds the deterministic authored modular Bridge sprite kit.

.DESCRIPTION
The manifest owns the source crop, runtime canvas, target placement, PPU,
pivot, output paths, and accepted hashes for all six modules. The builder
uses high-quality resampling, emits six progressively stronger construction
previews, and copies the completed module byte-for-byte into stage seven.
Validation rejects empty or clipped art, surviving magenta chroma key pixels,
and nearest-neighbor resampling of the source master.
#>

[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'HighResolutionBridge.manifest.json'),
    [ValidateRange(1, 254)]
    [int]$AlphaThreshold = 16,
    [switch]$ValidateOnly,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

if (-not ('ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ProjectUnknown.Tools.Art
{
    public static class AuthoredBridgeKitBuilder
    {
        private static readonly double[] RevealProgress = { 0.18, 0.34, 0.50, 0.66, 0.82, 1.0 };
        private static readonly double[] RevealedAlpha = { 0.66, 0.72, 0.78, 0.84, 0.90, 0.95 };
        private static readonly double[] GhostAlpha = { 0.14, 0.16, 0.18, 0.20, 0.22, 0.25 };
        private static readonly double[] TintStrength = { 0.66, 0.55, 0.44, 0.32, 0.19, 0.08 };

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

        public static void BuildFinal(
            Bitmap source,
            Rectangle sourceBounds,
            int outputWidth,
            int outputHeight,
            Rectangle targetBounds,
            string outputPath)
        {
            AssertBounds(source, sourceBounds, outputWidth, outputHeight, targetBounds);
            using (Bitmap output = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb))
            {
                DrawResampled(source, sourceBounds, output, targetBounds, InterpolationMode.HighQualityBicubic);
                output.Save(outputPath, ImageFormat.Png);
            }
        }

        public static void BuildConstructionStage(
            string finalPath,
            int stageIndex,
            bool horizontal,
            Rectangle targetBounds,
            string outputPath)
        {
            if (stageIndex < 0 || stageIndex >= 6)
            {
                throw new ArgumentOutOfRangeException("stageIndex");
            }

            using (Bitmap final = OpenArgb(finalPath))
            using (Bitmap output = new Bitmap(final.Width, final.Height, PixelFormat.Format32bppArgb))
            {
                double progress = RevealProgress[stageIndex];
                double revealedAlpha = RevealedAlpha[stageIndex];
                double ghostAlpha = GhostAlpha[stageIndex];
                double tint = TintStrength[stageIndex];
                for (int y = 0; y < final.Height; y++)
                {
                    for (int x = 0; x < final.Width; x++)
                    {
                        Color source = final.GetPixel(x, y);
                        if (source.A == 0)
                        {
                            output.SetPixel(x, y, Color.Transparent);
                            continue;
                        }

                        double axisPosition = horizontal
                            ? (targetBounds.Bottom - 0.5 - y) / targetBounds.Height
                            : (x + 0.5 - targetBounds.X) / targetBounds.Width;
                        double reveal = stageIndex == 5
                            ? 1.0
                            : ClampUnit((progress - axisPosition) / 0.12 + 0.5);
                        double alphaScale = ghostAlpha + (revealedAlpha - ghostAlpha) * reveal;
                        int alpha = ClampByte((int)Math.Round(source.A * alphaScale));
                        int red = ClampByte((int)Math.Round(source.R * (1.0 - tint) + 126.0 * tint));
                        int green = ClampByte((int)Math.Round(source.G * (1.0 - tint) + 103.0 * tint));
                        int blue = ClampByte((int)Math.Round(source.B * (1.0 - tint) + 72.0 * tint));
                        output.SetPixel(x, y, Color.FromArgb(alpha, red, green, blue));
                    }
                }

                output.Save(outputPath, ImageFormat.Png);
            }
        }

        public static long[] ValidateFinal(
            string outputPath,
            Bitmap source,
            Rectangle sourceBounds,
            int expectedWidth,
            int expectedHeight,
            Rectangle targetBounds,
            string seamEdge,
            int alphaThreshold)
        {
            using (Bitmap output = OpenArgb(outputPath))
            using (Bitmap nearest = new Bitmap(expectedWidth, expectedHeight, PixelFormat.Format32bppArgb))
            {
                if (output.Width != expectedWidth || output.Height != expectedHeight)
                {
                    throw new InvalidOperationException(String.Format(
                        "Module must be {0}x{1}; got {2}x{3}.",
                        expectedWidth,
                        expectedHeight,
                        output.Width,
                        output.Height));
                }

                AssertBounds(source, sourceBounds, expectedWidth, expectedHeight, targetBounds);
                DrawResampled(source, sourceBounds, nearest, targetBounds, InterpolationMode.NearestNeighbor);

                long visible = 0;
                long chroma = 0;
                long nearestDifferences = 0;
                long outsideTarget = 0;
                for (int y = 0; y < output.Height; y++)
                {
                    for (int x = 0; x < output.Width; x++)
                    {
                        Color pixel = output.GetPixel(x, y);
                        if (pixel.A >= alphaThreshold)
                        {
                            visible++;
                            if (!targetBounds.Contains(x, y))
                            {
                                outsideTarget++;
                            }

                            if (IsMagenta(pixel))
                            {
                                chroma++;
                            }
                        }

                        if (pixel.ToArgb() != nearest.GetPixel(x, y).ToArgb())
                        {
                            nearestDifferences++;
                        }
                    }
                }

                if (visible == 0)
                {
                    throw new InvalidOperationException("Module has no visible pixels.");
                }

                if (outsideTarget != 0)
                {
                    throw new InvalidOperationException("Module has visible pixels outside its manifest target bounds.");
                }

                if (chroma != 0)
                {
                    throw new InvalidOperationException(
                        "Module contains visible magenta chroma-key pixels: " + chroma);
                }

                if (nearestDifferences == 0)
                {
                    throw new InvalidOperationException(
                        "Module is an exact nearest-neighbor resample of the source crop.");
                }

                long seamVisible = CountSeamPixels(output, seamEdge, alphaThreshold);
                if (seamVisible == 0)
                {
                    throw new InvalidOperationException(
                        "Module has no visible pixels on required composition edge " + seamEdge + ".");
                }

                return new[] { visible, nearestDifferences, seamVisible };
            }
        }

        public static long[] ValidateConstructionStage(
            string stagePath,
            string finalPath,
            int expectedWidth,
            int expectedHeight,
            int stageIndex,
            int alphaThreshold)
        {
            using (Bitmap stage = OpenArgb(stagePath))
            using (Bitmap final = OpenArgb(finalPath))
            {
                if (stage.Width != expectedWidth || stage.Height != expectedHeight
                    || final.Width != expectedWidth || final.Height != expectedHeight)
                {
                    throw new InvalidOperationException("Construction module dimensions changed.");
                }

                long visible = 0;
                long chroma = 0;
                long finalDifferences = 0;
                for (int y = 0; y < stage.Height; y++)
                {
                    for (int x = 0; x < stage.Width; x++)
                    {
                        Color pixel = stage.GetPixel(x, y);
                        if (pixel.A >= alphaThreshold)
                        {
                            visible++;
                            if (IsMagenta(pixel))
                            {
                                chroma++;
                            }
                        }

                        if (pixel.ToArgb() != final.GetPixel(x, y).ToArgb())
                        {
                            finalDifferences++;
                        }
                    }
                }

                if (visible == 0)
                {
                    throw new InvalidOperationException("Construction module has no visible pixels.");
                }

                if (chroma != 0)
                {
                    throw new InvalidOperationException(
                        "Construction module contains visible magenta pixels: " + chroma);
                }

                if (stageIndex < 6 && finalDifferences == 0)
                {
                    throw new InvalidOperationException(
                        "A pre-final construction module is byte-identical in pixels to the final module.");
                }

                if (stageIndex == 6 && finalDifferences != 0)
                {
                    throw new InvalidOperationException(
                        "Construction stage seven must be pixel-identical to the final module.");
                }

                return new[] { visible, finalDifferences };
            }
        }

        public static bool FilesEqual(string leftPath, string rightPath)
        {
            FileInfo left = new FileInfo(leftPath);
            FileInfo right = new FileInfo(rightPath);
            if (left.Length != right.Length)
            {
                return false;
            }

            const int bufferSize = 81920;
            byte[] leftBuffer = new byte[bufferSize];
            byte[] rightBuffer = new byte[bufferSize];
            using (FileStream leftStream = File.OpenRead(leftPath))
            using (FileStream rightStream = File.OpenRead(rightPath))
            {
                while (true)
                {
                    int leftRead = leftStream.Read(leftBuffer, 0, bufferSize);
                    int rightRead = rightStream.Read(rightBuffer, 0, bufferSize);
                    if (leftRead != rightRead)
                    {
                        return false;
                    }

                    if (leftRead == 0)
                    {
                        return true;
                    }

                    for (int index = 0; index < leftRead; index++)
                    {
                        if (leftBuffer[index] != rightBuffer[index])
                        {
                            return false;
                        }
                    }
                }
            }
        }

        private static void DrawResampled(
            Bitmap source,
            Rectangle sourceBounds,
            Bitmap output,
            Rectangle targetBounds,
            InterpolationMode interpolation)
        {
            using (Graphics graphics = Graphics.FromImage(output))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = interpolation;
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
            }
        }

        private static void AssertBounds(
            Bitmap source,
            Rectangle sourceBounds,
            int outputWidth,
            int outputHeight,
            Rectangle targetBounds)
        {
            if (sourceBounds.X < 0 || sourceBounds.Y < 0
                || sourceBounds.Width <= 0 || sourceBounds.Height <= 0
                || sourceBounds.Right > source.Width || sourceBounds.Bottom > source.Height)
            {
                throw new InvalidOperationException("Source bounds exceed the Bridge master image.");
            }

            if (targetBounds.X < 0 || targetBounds.Y < 0
                || targetBounds.Width <= 0 || targetBounds.Height <= 0
                || targetBounds.Right > outputWidth || targetBounds.Bottom > outputHeight)
            {
                throw new InvalidOperationException("Target bounds exceed the Bridge runtime canvas.");
            }
        }

        private static bool IsMagenta(Color pixel)
        {
            return pixel.R >= 238 && pixel.G <= 48 && pixel.B >= 238;
        }

        private static long CountSeamPixels(Bitmap bitmap, string edge, int alphaThreshold)
        {
            if (String.Equals(edge, "Left", StringComparison.Ordinal))
            {
                return CountVerticalEdge(bitmap, 0, alphaThreshold);
            }

            if (String.Equals(edge, "Right", StringComparison.Ordinal))
            {
                return CountVerticalEdge(bitmap, bitmap.Width - 1, alphaThreshold);
            }

            if (String.Equals(edge, "Top", StringComparison.Ordinal))
            {
                return CountHorizontalEdge(bitmap, 0, alphaThreshold);
            }

            if (String.Equals(edge, "Bottom", StringComparison.Ordinal))
            {
                return CountHorizontalEdge(bitmap, bitmap.Height - 1, alphaThreshold);
            }

            if (String.Equals(edge, "BothHorizontal", StringComparison.Ordinal))
            {
                return Math.Min(
                    CountVerticalEdge(bitmap, 0, alphaThreshold),
                    CountVerticalEdge(bitmap, bitmap.Width - 1, alphaThreshold));
            }

            if (String.Equals(edge, "BothVertical", StringComparison.Ordinal))
            {
                return Math.Min(
                    CountHorizontalEdge(bitmap, 0, alphaThreshold),
                    CountHorizontalEdge(bitmap, bitmap.Height - 1, alphaThreshold));
            }

            throw new InvalidOperationException("Unsupported seam edge: " + edge);
        }

        private static long CountVerticalEdge(Bitmap bitmap, int x, int alphaThreshold)
        {
            long count = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).A >= alphaThreshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static long CountHorizontalEdge(Bitmap bitmap, int y, int alphaThreshold)
        {
            long count = 0;
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A >= alphaThreshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ClampByte(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private static double ClampUnit(double value)
        {
            return Math.Max(0.0, Math.Min(1.0, value));
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

function Assert-BridgeContract {
    param([Parameter(Mandatory)]$Manifest)

    if ([int]$Manifest.pixelsPerUnit -ne 48) {
        throw 'Bridge authored modules must use 48 PPU.'
    }

    if ($Manifest.pivotNormalized.Count -ne 2 -or
        [double]$Manifest.pivotNormalized[0] -ne 0.5 -or
        [double]$Manifest.pivotNormalized[1] -ne 0.5) {
        throw 'Bridge authored modules must use the centered 0.5,0.5 pivot.'
    }

    if ([int]$Manifest.constructionStageCount -ne 7) {
        throw 'Bridge authored modules require exactly seven construction stages.'
    }

    $expectedSizes = @{
        'Horizontal/Start' = '68x112'
        'Horizontal/Middle' = '48x112'
        'Horizontal/End' = '68x112'
        'Vertical/Start' = '124x68'
        'Vertical/Middle' = '124x48'
        'Vertical/End' = '124x68'
    }
    $seen = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($Manifest.modules)) {
        $key = "$($entry.orientation)/$($entry.module)"
        if (-not $expectedSizes.ContainsKey($key)) {
            throw "Unsupported Bridge module contract entry: $key"
        }

        if (-not $seen.Add($key)) {
            throw "Duplicate Bridge module contract entry: $key"
        }

        if ($entry.outputCanvas.Count -ne 2) {
            throw "$key outputCanvas must contain width and height."
        }

        $actualSize = "$([int]$entry.outputCanvas[0])x$([int]$entry.outputCanvas[1])"
        if ($actualSize -ne $expectedSizes[$key]) {
            throw "$key must use runtime module size $($expectedSizes[$key]); got $actualSize."
        }

        $expectedOutput = "Assets/Resources/Visual/Authored/Buildings/Bridge/$key.png"
        if (([string]$entry.output).Replace('\', '/') -ne $expectedOutput) {
            throw "$key final output path must be $expectedOutput"
        }

        $construction = @($entry.construction)
        if ($construction.Count -ne 7) {
            throw "$key must declare exactly seven construction outputs."
        }

        for ($stage = 1; $stage -le 7; $stage++) {
            $stageEntry = $construction[$stage - 1]
            if ([int]$stageEntry.stage -ne $stage) {
                throw "$key construction entries must be ordered S01 through S07."
            }

            $expectedStagePath = "Assets/Resources/Visual/Authored/Construction/Bridge/$($entry.orientation)/S$($stage.ToString('00'))/$($entry.module).png"
            if (([string]$stageEntry.output).Replace('\', '/') -ne $expectedStagePath) {
                throw "$key stage $stage output path must be $expectedStagePath"
            }
        }
    }

    if ($seen.Count -ne $expectedSizes.Count) {
        throw 'Bridge manifest must declare Horizontal and Vertical Start/Middle/End exactly once.'
    }
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

Assert-BridgeContract $manifest

$sourcePath = Resolve-ProjectPath $manifest.source 'Bridge source master'
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Bridge source master does not exist: $sourcePath"
}

$sourceHash = Assert-Hash $sourcePath $manifest.sourceSha256 'Bridge source master'
$source = [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::OpenArgb($sourcePath)
try {
    if ($manifest.sourceSize.Count -ne 2 -or
        $source.Width -ne [int]$manifest.sourceSize[0] -or
        $source.Height -ne [int]$manifest.sourceSize[1]) {
        throw "Bridge source dimensions changed: $($source.Width)x$($source.Height)"
    }

    if (-not $ValidateOnly -and -not $Force) {
        foreach ($entry in @($manifest.modules)) {
            $candidatePaths = @([string]$entry.output) + @($entry.construction | ForEach-Object { [string]$_.output })
            foreach ($candidate in $candidatePaths) {
                $candidatePath = Resolve-ProjectPath $candidate 'Bridge output'
                if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
                    throw "Output already exists: $candidatePath. Pass -Force to replace it."
                }
            }
        }
    }

    foreach ($entry in @($manifest.modules)) {
        $key = "$($entry.orientation)/$($entry.module)"
        $sourceBounds = ConvertTo-Rectangle $entry.sourceBounds "$key sourceBounds"
        $targetBounds = ConvertTo-Rectangle $entry.targetBoundsTopLeft "$key targetBoundsTopLeft"
        $outputWidth = [int]$entry.outputCanvas[0]
        $outputHeight = [int]$entry.outputCanvas[1]
        $outputPath = Resolve-ProjectPath $entry.output "$key final output"

        if ($ValidateOnly) {
            if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
                throw "Bridge final output does not exist: $outputPath"
            }

            $finalStats = [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::ValidateFinal(
                $outputPath,
                $source,
                $sourceBounds,
                $outputWidth,
                $outputHeight,
                $targetBounds,
                [string]$entry.seamEdge,
                $AlphaThreshold)
            $finalHash = Assert-Hash $outputPath $entry.expectedSha256 "$key final output"
            Write-Host "PASS Bridge/$key final hash=$finalHash visible=$($finalStats[0]) detailDelta=$($finalStats[1]) seamPixels=$($finalStats[2])"
        }
        else {
            [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
            $temporaryPath = $outputPath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
            try {
                [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::BuildFinal(
                    $source,
                    $sourceBounds,
                    $outputWidth,
                    $outputHeight,
                    $targetBounds,
                    $temporaryPath)
                $finalStats = [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::ValidateFinal(
                    $temporaryPath,
                    $source,
                    $sourceBounds,
                    $outputWidth,
                    $outputHeight,
                    $targetBounds,
                    [string]$entry.seamEdge,
                    $AlphaThreshold)
                $finalHash = Assert-Hash $temporaryPath $entry.expectedSha256 "$key final output"
                Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
                Write-Host "Built Bridge/$key final -> $outputPath"
                Write-Host "  hash=$finalHash sourceHash=$sourceHash visible=$($finalStats[0]) detailDelta=$($finalStats[1]) seamPixels=$($finalStats[2])"
            }
            finally {
                if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
                    Remove-Item -LiteralPath $temporaryPath -Force
                }
            }
        }

        $previousVisible = -1L
        foreach ($stageEntry in @($entry.construction)) {
            $stage = [int]$stageEntry.stage
            $stageIndex = $stage - 1
            $stagePath = Resolve-ProjectPath $stageEntry.output "$key S$($stage.ToString('00')) output"
            if ($ValidateOnly) {
                if (-not (Test-Path -LiteralPath $stagePath -PathType Leaf)) {
                    throw "Bridge construction output does not exist: $stagePath"
                }

                $stageStats = [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::ValidateConstructionStage(
                    $stagePath,
                    $outputPath,
                    $outputWidth,
                    $outputHeight,
                    $stageIndex,
                    $AlphaThreshold)
                $stageHash = Assert-Hash $stagePath $stageEntry.expectedSha256 "$key S$($stage.ToString('00')) output"
            }
            else {
                [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($stagePath)) | Out-Null
                $temporaryStagePath = $stagePath + '.tmp.' + [Guid]::NewGuid().ToString('N') + '.png'
                try {
                    if ($stage -eq 7) {
                        Copy-Item -LiteralPath $outputPath -Destination $temporaryStagePath
                    }
                    else {
                        [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::BuildConstructionStage(
                            $outputPath,
                            $stageIndex,
                            ([string]$entry.orientation -eq 'Horizontal'),
                            $targetBounds,
                            $temporaryStagePath)
                    }

                    $stageStats = [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::ValidateConstructionStage(
                        $temporaryStagePath,
                        $outputPath,
                        $outputWidth,
                        $outputHeight,
                        $stageIndex,
                        $AlphaThreshold)
                    if ($stage -eq 7 -and
                        -not [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::FilesEqual(
                            $temporaryStagePath,
                            $outputPath)) {
                        throw "$key S07 must be byte-identical to its final module."
                    }

                    $stageHash = Assert-Hash $temporaryStagePath $stageEntry.expectedSha256 "$key S$($stage.ToString('00')) output"
                    Move-Item -LiteralPath $temporaryStagePath -Destination $stagePath -Force
                }
                finally {
                    if (Test-Path -LiteralPath $temporaryStagePath -PathType Leaf) {
                        Remove-Item -LiteralPath $temporaryStagePath -Force
                    }
                }
            }

            if ($stageStats[0] -lt $previousVisible) {
                throw "$key construction visible coverage decreased at S$($stage.ToString('00'))."
            }

            $previousVisible = $stageStats[0]
            if ($stage -eq 7 -and
                -not [ProjectUnknown.Tools.Art.AuthoredBridgeKitBuilder]::FilesEqual($stagePath, $outputPath)) {
                throw "$key S07 is not byte-identical to its final module."
            }

            $verb = if ($ValidateOnly) { 'PASS' } else { 'Built' }
            Write-Host "$verb Bridge/$key S$($stage.ToString('00')) hash=$stageHash visible=$($stageStats[0]) finalDelta=$($stageStats[1])"
        }
    }
}
finally {
    $source.Dispose()
}
