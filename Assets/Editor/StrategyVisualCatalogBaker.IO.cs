using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private static void RecreateOutputRoot()
        {
            if (AssetDatabase.IsValidFolder(BakedRoot))
            {
                AssetDatabase.DeleteAsset(BakedRoot);
            }

            EnsureAssetDirectory(BakedRoot + "/placeholder.txt");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Sprite BakeSpriteAsset(Sprite source, string assetPath, bool readable = false)
        {
            if (source == null)
            {
                throw new InvalidOperationException("Cannot bake a null sprite: " + assetPath);
            }

            GetSpritePixels(source, out Color32[] pixels, out int width, out int height);
            WriteTexture(assetPath, pixels, width, height);
            ConfigureSpriteImporter(assetPath, source.pixelsPerUnit, NormalizePivot(source), readable);
            Sprite imported = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (imported == null)
            {
                throw new InvalidOperationException("Sprite import failed: " + assetPath);
            }

            return imported;
        }

        private static Sprite ResolveAuthoredSprite(string relativePath, Sprite fallback)
        {
            if (fallback == null)
            {
                throw new InvalidOperationException("Cannot resolve authored art without a fallback sprite: " + relativePath);
            }

            string assetPath = $"{AuthoredRoot}/{relativePath}";
            if (!File.Exists(ToAbsolutePath(assetPath)))
            {
                return fallback;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            Texture2D authoredTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (authoredTexture == null)
            {
                throw new InvalidOperationException("Authored texture import failed: " + assetPath);
            }

            int expectedWidth = Mathf.RoundToInt(fallback.rect.width);
            int expectedHeight = Mathf.RoundToInt(fallback.rect.height);
            float pixelsPerUnit = fallback.pixelsPerUnit;
            if (TryGetHighResolutionSpriteContract(
                    relativePath,
                    out int fallbackWidth,
                    out int fallbackHeight,
                    out int authoredWidth,
                    out int authoredHeight,
                    out float authoredPixelsPerUnit))
            {
                ValidateFallbackSpriteContract(
                    relativePath,
                    fallback,
                    fallbackWidth,
                    fallbackHeight);
                expectedWidth = authoredWidth;
                expectedHeight = authoredHeight;
                pixelsPerUnit = authoredPixelsPerUnit;
            }

            if (authoredTexture.width != expectedWidth || authoredTexture.height != expectedHeight)
            {
                throw new InvalidOperationException(
                    $"Authored sprite dimensions must remain {expectedWidth}x{expectedHeight}: "
                    + $"{assetPath} is {authoredTexture.width}x{authoredTexture.height}");
            }

            ConfigureSpriteImporter(
                assetPath,
                pixelsPerUnit,
                NormalizePivot(fallback),
                readable: false);
            Sprite authored = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (authored == null)
            {
                throw new InvalidOperationException("Authored sprite import failed: " + assetPath);
            }

            int actualWidth = Mathf.RoundToInt(authored.rect.width);
            int actualHeight = Mathf.RoundToInt(authored.rect.height);
            if (actualWidth != expectedWidth || actualHeight != expectedHeight)
            {
                throw new InvalidOperationException(
                    $"Authored sprite dimensions must remain {expectedWidth}x{expectedHeight}: "
                    + $"{assetPath} is {actualWidth}x{actualHeight}");
            }

            return authored;
        }

        private static StrategyVisualCatalog.VisualSequenceSet BakeSequenceAsset(
            string id,
            Sprite[] frames,
            string assetPath,
            string authoredRelativePath = null)
        {
            CalculateFrameLayout(frames, out int width, out int height, out Vector2 pivotPixels);
            Vector2 pivot = new(pivotPixels.x / width, pivotPixels.y / height);
            float pixelsPerUnit = frames[0].pixelsPerUnit;
            Color32[] atlasPixels = new Color32[width * frames.Length * height];
            for (int i = 0; i < frames.Length; i++)
            {
                BlitSprite(frames[i], atlasPixels, width * frames.Length, i * width, 0, pivotPixels);
            }

            WriteTexture(assetPath, atlasPixels, width * frames.Length, height);
            ConfigureAtlasImporter(assetPath);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (atlas == null)
            {
                throw new InvalidOperationException("Atlas import failed: " + assetPath);
            }

            if (!string.IsNullOrWhiteSpace(authoredRelativePath))
            {
                atlas = ResolveAuthoredSequenceAtlas(
                    authoredRelativePath,
                    atlas,
                    width,
                    height,
                    frames.Length,
                    frames[0].pixelsPerUnit,
                    out int resolvedFrameWidth,
                    out int resolvedFrameHeight,
                    out float resolvedPixelsPerUnit);
                width = resolvedFrameWidth;
                height = resolvedFrameHeight;
                pixelsPerUnit = resolvedPixelsPerUnit;
            }

            return new StrategyVisualCatalog.VisualSequenceSet(
                id,
                atlas,
                width,
                height,
                frames.Length,
                pixelsPerUnit,
                pivot);
        }

        private static Texture2D ResolveAuthoredSequenceAtlas(
            string relativePath,
            Texture2D fallback,
            int fallbackFrameWidth,
            int fallbackFrameHeight,
            int frameCount,
            float fallbackPixelsPerUnit,
            out int resolvedFrameWidth,
            out int resolvedFrameHeight,
            out float resolvedPixelsPerUnit)
        {
            if (fallback == null)
            {
                throw new InvalidOperationException(
                    "Cannot resolve an authored sequence atlas without a fallback texture: " + relativePath);
            }

            string assetPath = $"{AuthoredRoot}/{relativePath}";
            if (!File.Exists(ToAbsolutePath(assetPath)))
            {
                resolvedFrameWidth = fallbackFrameWidth;
                resolvedFrameHeight = fallbackFrameHeight;
                resolvedPixelsPerUnit = fallbackPixelsPerUnit;
                return fallback;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            Texture2D authored = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (authored == null)
            {
                throw new InvalidOperationException("Authored sequence atlas import failed: " + assetPath);
            }

            resolvedFrameWidth = fallbackFrameWidth;
            resolvedFrameHeight = fallbackFrameHeight;
            resolvedPixelsPerUnit = fallbackPixelsPerUnit;
            int expectedWidth = fallbackFrameWidth * frameCount;
            int expectedHeight = fallbackFrameHeight;
            float? importerPixelsPerUnit = null;
            if (TryGetHighResolutionSequenceContract(
                    relativePath,
                    out int expectedFallbackFrameWidth,
                    out int expectedFallbackFrameHeight,
                    out int authoredFrameWidth,
                    out int authoredFrameHeight,
                    out float authoredPixelsPerUnit))
            {
                if (frameCount != 7)
                {
                    throw new InvalidOperationException(
                        $"High-resolution authored sequence {relativePath} requires exactly 7 frames");
                }

                ValidateFallbackSequenceContract(
                    relativePath,
                    fallbackFrameWidth,
                    fallbackFrameHeight,
                    fallbackPixelsPerUnit,
                    expectedFallbackFrameWidth,
                    expectedFallbackFrameHeight);
                resolvedFrameWidth = authoredFrameWidth;
                resolvedFrameHeight = authoredFrameHeight;
                resolvedPixelsPerUnit = authoredPixelsPerUnit;
                expectedWidth = authoredFrameWidth * frameCount;
                expectedHeight = authoredFrameHeight;
                importerPixelsPerUnit = authoredPixelsPerUnit;
            }

            if (authored.width != expectedWidth || authored.height != expectedHeight)
            {
                throw new InvalidOperationException(
                    $"Authored sequence atlas dimensions must remain {expectedWidth}x{expectedHeight}: "
                    + $"{assetPath} is {authored.width}x{authored.height}");
            }

            ConfigureAtlasImporter(assetPath, importerPixelsPerUnit);
            authored = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (authored == null)
            {
                throw new InvalidOperationException("Authored sequence atlas reimport failed: " + assetPath);
            }

            return authored;
        }

        private static bool TryGetHighResolutionSpriteContract(
            string relativePath,
            out int fallbackWidth,
            out int fallbackHeight,
            out int authoredWidth,
            out int authoredHeight,
            out float pixelsPerUnit)
        {
            fallbackWidth = 0;
            fallbackHeight = 0;
            authoredWidth = 0;
            authoredHeight = 0;
            pixelsPerUnit = 0f;
            if (relativePath.StartsWith("Buildings/House/V", StringComparison.Ordinal)
                && relativePath.EndsWith(".png", StringComparison.Ordinal))
            {
                fallbackWidth = 80;
                fallbackHeight = 80;
                authoredWidth = 160;
                authoredHeight = 160;
                pixelsPerUnit = 48f;
                return true;
            }

            if (string.Equals(relativePath, "Buildings/ForagerCamp/V01.png", StringComparison.Ordinal))
            {
                fallbackWidth = 88;
                fallbackHeight = 58;
                authoredWidth = 176;
                authoredHeight = 116;
                pixelsPerUnit = 48f;
                return true;
            }

            return false;
        }

        private static bool TryGetHighResolutionSequenceContract(
            string relativePath,
            out int fallbackFrameWidth,
            out int fallbackFrameHeight,
            out int authoredFrameWidth,
            out int authoredFrameHeight,
            out float pixelsPerUnit)
        {
            bool isHouse = relativePath.StartsWith("Construction/House/V", StringComparison.Ordinal)
                && relativePath.EndsWith(".png", StringComparison.Ordinal);
            bool isForagerCamp = string.Equals(
                relativePath,
                "Construction/ForagerCamp/V01.png",
                StringComparison.Ordinal);
            fallbackFrameWidth = 92;
            fallbackFrameHeight = 82;
            authoredFrameWidth = 184;
            authoredFrameHeight = 164;
            pixelsPerUnit = 48f;
            return isHouse || isForagerCamp;
        }

        private static void ValidateFallbackSpriteContract(
            string relativePath,
            Sprite fallback,
            int expectedWidth,
            int expectedHeight)
        {
            int actualWidth = Mathf.RoundToInt(fallback.rect.width);
            int actualHeight = Mathf.RoundToInt(fallback.rect.height);
            if (actualWidth != expectedWidth
                || actualHeight != expectedHeight
                || !Mathf.Approximately(fallback.pixelsPerUnit, 24f))
            {
                throw new InvalidOperationException(
                    $"High-resolution authored sprite {relativePath} requires a "
                    + $"{expectedWidth}x{expectedHeight} @ 24 PPU fallback, but received "
                    + $"{actualWidth}x{actualHeight} @ {fallback.pixelsPerUnit} PPU");
            }
        }

        private static void ValidateFallbackSequenceContract(
            string relativePath,
            int fallbackFrameWidth,
            int fallbackFrameHeight,
            float fallbackPixelsPerUnit,
            int expectedFrameWidth,
            int expectedFrameHeight)
        {
            if (fallbackFrameWidth != expectedFrameWidth
                || fallbackFrameHeight != expectedFrameHeight
                || !Mathf.Approximately(fallbackPixelsPerUnit, 24f))
            {
                throw new InvalidOperationException(
                    $"High-resolution authored sequence {relativePath} requires "
                    + $"{expectedFrameWidth}x{expectedFrameHeight} @ 24 PPU fallback frames, but received "
                    + $"{fallbackFrameWidth}x{fallbackFrameHeight} @ {fallbackPixelsPerUnit} PPU");
            }
        }

        private static void CalculateFrameLayout(
            Sprite[] frames,
            out int width,
            out int height,
            out Vector2 pivotPixels)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                throw new InvalidOperationException("Cannot calculate an empty sprite sequence layout");
            }

            float minLeft = float.MaxValue;
            float minBottom = float.MaxValue;
            float maxRight = float.MinValue;
            float maxTop = float.MinValue;
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = frames[i] ?? throw new InvalidOperationException("Sprite sequence contains null");
                minLeft = Mathf.Min(minLeft, -sprite.pivot.x);
                minBottom = Mathf.Min(minBottom, -sprite.pivot.y);
                maxRight = Mathf.Max(maxRight, sprite.rect.width - sprite.pivot.x);
                maxTop = Mathf.Max(maxTop, sprite.rect.height - sprite.pivot.y);
            }

            width = Mathf.Max(1, Mathf.CeilToInt(maxRight - minLeft));
            height = Mathf.Max(1, Mathf.CeilToInt(maxTop - minBottom));
            pivotPixels = new Vector2(-minLeft, -minBottom);
        }

        private static void BlitSprite(
            Sprite source,
            Color32[] destination,
            int destinationWidth,
            int cellX,
            int cellY,
            Vector2 targetPivot)
        {
            GetSpritePixels(source, out Color32[] pixels, out int width, out int height);
            int offsetX = cellX + Mathf.RoundToInt(targetPivot.x - source.pivot.x);
            int offsetY = cellY + Mathf.RoundToInt(targetPivot.y - source.pivot.y);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    destination[(offsetY + y) * destinationWidth + offsetX + x] = pixels[y * width + x];
                }
            }
        }

        private static void GetSpritePixels(
            Sprite source,
            out Color32[] pixels,
            out int width,
            out int height)
        {
            Rect rect = source.rect;
            width = Mathf.RoundToInt(rect.width);
            height = Mathf.RoundToInt(rect.height);
            Color[] sourcePixels = source.texture.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                width,
                height);
            pixels = new Color32[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                pixels[i] = sourcePixels[i];
            }
        }

        private static Vector2 NormalizePivot(Sprite source)
        {
            return new Vector2(
                source.pivot.x / Mathf.Max(1f, source.rect.width),
                source.pivot.y / Mathf.Max(1f, source.rect.height));
        }

        private static void WriteTexture(
            string assetPath,
            Color32[] pixels,
            int width,
            int height)
        {
            EnsureAssetDirectory(assetPath);
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(ToAbsolutePath(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureSpriteImporter(string assetPath, float ppu, Vector2 pivot, bool readable)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
            importer.spritePivot = pivot;
            ConfigureCommonImporter(importer);
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static void ConfigureAtlasImporter(string assetPath, float? pixelsPerUnit = null)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            if (pixelsPerUnit.HasValue)
            {
                importer.spritePixelsPerUnit = pixelsPerUnit.Value;
            }

            ConfigureCommonImporter(importer);
            importer.SaveAndReimport();
        }

        private static void ConfigureCommonImporter(TextureImporter importer)
        {
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.maxTextureSize = 4096;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(ToAbsolutePath(assetPath));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
