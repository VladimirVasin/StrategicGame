using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyBridgeVisualProfile
    {
        public const int MinimumSpanCells = 3;
        public const int MaximumSpanCells = 12;
        public const int AuthoredPixelsPerUnit = 48;
        public const int CellPixels = 48;
        public const int CapOverhangPixels = 20;
        public const int HorizontalHeight = 112;
        public const int VerticalWidth = 124;

        // Start is the lower world coordinate (left/bottom); End is right/top.
        // This keeps composition independent from which bank the player clicked first.
        internal enum Module
        {
            Start = 0,
            Middle = 1,
            End = 2
        }

        internal delegate Sprite CatalogSpriteResolver(string sequenceId, int frame);
        internal delegate Sprite ResourceSpriteResolver(string resourcePath);

        private static bool resolverOverridesActive;
        private static CatalogSpriteResolver catalogResolverOverride;
        private static ResourceSpriteResolver resourceResolverOverride;
        private static readonly Dictionary<EntityId, ModulePixels> ModulePixelCache = new();

        public static bool TryCreateCompletedSprite(Vector2Int footprint, out Sprite sprite)
        {
            return TryCreateSprite(footprint, false, 0, true, out sprite);
        }

        public static bool TryCreateConstructionSprite(Vector2Int footprint, int stage, out Sprite sprite)
        {
            return TryCreateSprite(
                footprint,
                true,
                Mathf.Clamp(stage, 0, StrategyConstructionSpriteFactory.StageCount - 1),
                true,
                out sprite);
        }

        internal static bool TryCreateReadableCompletedSpriteForBake(
            Vector2Int footprint,
            out Sprite sprite)
        {
            return TryCreateSprite(footprint, false, 0, false, out sprite);
        }

        internal static bool TryCreateReadableConstructionSpriteForBake(
            Vector2Int footprint,
            int stage,
            out Sprite sprite)
        {
            return TryCreateSprite(
                footprint,
                true,
                Mathf.Clamp(stage, 0, StrategyConstructionSpriteFactory.StageCount - 1),
                false,
                out sprite);
        }

        internal static Vector2Int GetOutputPixelSize(Vector2Int footprint)
        {
            bool horizontal = footprint.x >= footprint.y;
            int span = Mathf.Max(1, horizontal ? footprint.x : footprint.y);
            int lengthPixels = span * CellPixels + CapOverhangPixels * 2;
            return horizontal
                ? new Vector2Int(Mathf.Max(144, lengthPixels), HorizontalHeight)
                : new Vector2Int(VerticalWidth, Mathf.Max(144, lengthPixels));
        }

        internal static Vector2Int GetModulePixelSize(bool horizontal, Module module)
        {
            int capPixels = CellPixels + CapOverhangPixels;
            if (horizontal)
            {
                return new Vector2Int(
                    module == Module.Middle ? CellPixels : capPixels,
                    HorizontalHeight);
            }

            return new Vector2Int(
                VerticalWidth,
                module == Module.Middle ? CellPixels : capPixels);
        }

        internal static string GetCatalogSequenceId(
            bool horizontal,
            Module module,
            bool construction)
        {
            string root = construction
                ? StrategyVisualSequenceIds.BridgeConstruction
                : StrategyVisualSequenceIds.BridgeFinal;
            return $"{root}/{GetOrientationName(horizontal)}/{module}";
        }

        internal static string GetResourcePath(
            bool horizontal,
            Module module,
            bool construction,
            int stage)
        {
            string orientation = GetOrientationName(horizontal);
            return construction
                ? $"Visual/Authored/Construction/Bridge/{orientation}/S{stage + 1:00}/{module}"
                : $"Visual/Authored/Buildings/Bridge/{orientation}/{module}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetRuntimeState()
        {
            ResetCache();
            resolverOverridesActive = false;
            catalogResolverOverride = null;
            resourceResolverOverride = null;
        }

        internal static void ResetCache()
        {
            ModulePixelCache.Clear();
        }

        internal static void SetResolversForTests(
            CatalogSpriteResolver catalogResolver,
            ResourceSpriteResolver resourceResolver)
        {
            resolverOverridesActive = true;
            catalogResolverOverride = catalogResolver;
            resourceResolverOverride = resourceResolver;
        }

        private static bool TryCreateSprite(
            Vector2Int footprint,
            bool construction,
            int stage,
            bool releaseCpuCopy,
            out Sprite sprite)
        {
            bool horizontal = footprint.x >= footprint.y;
            int span = Mathf.Max(1, horizontal ? footprint.x : footprint.y);
            if (span < MinimumSpanCells || span > MaximumSpanCells
                || !TryLoadModule(horizontal, Module.Start, construction, stage, out ModulePixels start)
                || !TryLoadModule(horizontal, Module.Middle, construction, stage, out ModulePixels middle)
                || !TryLoadModule(horizontal, Module.End, construction, stage, out ModulePixels end))
            {
                sprite = null;
                return false;
            }

            Vector2Int outputSize = GetOutputPixelSize(footprint);
            Color32[] output = new Color32[outputSize.x * outputSize.y];
            if (horizontal)
            {
                CopyModule(start, output, outputSize.x, 0, 0);
                for (int i = 0; i < span - 2; i++)
                {
                    CopyModule(middle, output, outputSize.x, start.Width + i * middle.Width, 0);
                }

                CopyModule(end, output, outputSize.x, outputSize.x - end.Width, 0);
            }
            else
            {
                CopyModule(start, output, outputSize.x, 0, 0);
                for (int i = 0; i < span - 2; i++)
                {
                    CopyModule(middle, output, outputSize.x, 0, start.Height + i * middle.Height);
                }

                CopyModule(end, output, outputSize.x, 0, outputSize.y - end.Height);
            }

            string kind = construction ? $"Construction Stage {stage + 1}" : "Completed";
            Texture2D texture = new(outputSize.x, outputSize.y, TextureFormat.RGBA32, false)
            {
                name = $"Authored Bridge {kind} {footprint.x}x{footprint.y}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(output);
            texture.Apply(false, releaseCpuCopy);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, outputSize.x, outputSize.y),
                new Vector2(0.5f, 0.5f),
                AuthoredPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + " Sprite";
            return true;
        }

        private static bool TryLoadModule(
            bool horizontal,
            Module module,
            bool construction,
            int stage,
            out ModulePixels pixels)
        {
            string sequenceId = GetCatalogSequenceId(horizontal, module, construction);
            int frame = construction ? stage : 0;
            Sprite candidate;
            if (resolverOverridesActive)
            {
                candidate = catalogResolverOverride?.Invoke(sequenceId, frame);
            }
            else
            {
                StrategyVisualCatalogProvider.TryGetSequenceSprite(sequenceId, frame, out candidate);
            }

            if (TryReadModule(candidate, horizontal, module, out pixels))
            {
                return true;
            }

            string resourcePath = GetResourcePath(horizontal, module, construction, stage);
            candidate = resolverOverridesActive
                ? resourceResolverOverride?.Invoke(resourcePath)
                : Resources.Load<Sprite>(resourcePath);
            return TryReadModule(candidate, horizontal, module, out pixels);
        }

        private static bool TryReadModule(
            Sprite sprite,
            bool horizontal,
            Module module,
            out ModulePixels modulePixels)
        {
            modulePixels = default;
            Vector2Int expected = GetModulePixelSize(horizontal, module);
            if (sprite == null
                || sprite.texture == null
                || Mathf.Abs(sprite.pixelsPerUnit - AuthoredPixelsPerUnit) > 0.01f
                || Mathf.RoundToInt(sprite.rect.width) != expected.x
                || Mathf.RoundToInt(sprite.rect.height) != expected.y)
            {
                return false;
            }

            EntityId cacheKey = sprite.GetEntityId();
            if (ModulePixelCache.TryGetValue(cacheKey, out modulePixels))
            {
                return true;
            }

            try
            {
                Rect rect = sprite.textureRect;
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);
                if (width != expected.x || height != expected.y)
                {
                    // Tight-mesh sprites expose trimmed textureRect bounds, while the
                    // authored module contract is stored in the full source rect.
                    rect = sprite.rect;
                    width = Mathf.RoundToInt(rect.width);
                    height = Mathf.RoundToInt(rect.height);
                }

                if (width != expected.x || height != expected.y
                    || !TryReadTexture(sprite.texture, out Color32[] texturePixels))
                {
                    return false;
                }

                int startX = Mathf.RoundToInt(rect.xMin);
                int startY = Mathf.RoundToInt(rect.yMin);
                if (startX < 0 || startY < 0
                    || startX + width > sprite.texture.width
                    || startY + height > sprite.texture.height)
                {
                    return false;
                }

                Color32[] cropped = new Color32[width * height];
                bool hasVisiblePixel = false;
                for (int y = 0; y < height; y++)
                {
                    int sourceIndex = (startY + y) * sprite.texture.width + startX;
                    int destinationIndex = y * width;
                    Array.Copy(texturePixels, sourceIndex, cropped, destinationIndex, width);
                    for (int x = 0; x < width; x++)
                    {
                        Color32 color = cropped[destinationIndex + x];
                        if (color.a > 16)
                        {
                            hasVisiblePixel = true;
                            if (color.r >= 240 && color.g <= 32 && color.b >= 240)
                            {
                                return false;
                            }
                        }
                    }
                }

                if (!hasVisiblePixel)
                {
                    return false;
                }

                modulePixels = new ModulePixels(width, height, cropped);
                ModulePixelCache[cacheKey] = modulePixels;
                return true;
            }
            catch (Exception exception) when (
                exception is UnityException
                || exception is ArgumentException
                || exception is InvalidOperationException
                || exception is NotSupportedException)
            {
                return false;
            }
        }

        private static bool TryReadTexture(Texture2D source, out Color32[] pixels)
        {
            if (source.isReadable)
            {
                try
                {
                    pixels = source.GetPixels32(0);
                    if (pixels.Length == source.width * source.height)
                    {
                        return true;
                    }
                }
                catch (UnityException)
                {
                    // A GPU readback below also supports normal non-readable imports.
                }
            }

            return TryReadTextureFromGpu(source, out pixels);
        }

        private static bool TryReadTextureFromGpu(Texture2D source, out Color32[] pixels)
        {
            pixels = null;
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D readable = null;
            try
            {
                temporary = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                temporary.filterMode = FilterMode.Point;
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                pixels = readable.GetPixels32(0);
                return pixels.Length == source.width * source.height;
            }
            catch (Exception exception) when (
                exception is UnityException
                || exception is ArgumentException
                || exception is InvalidOperationException
                || exception is NotSupportedException)
            {
                pixels = null;
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }

                if (readable != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(readable);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(readable);
                    }
                }
            }
        }

        private static void CopyModule(
            ModulePixels source,
            Color32[] destination,
            int destinationWidth,
            int destinationX,
            int destinationY)
        {
            for (int y = 0; y < source.Height; y++)
            {
                Array.Copy(
                    source.Pixels,
                    y * source.Width,
                    destination,
                    (destinationY + y) * destinationWidth + destinationX,
                    source.Width);
            }
        }

        private static string GetOrientationName(bool horizontal)
        {
            return horizontal ? "Horizontal" : "Vertical";
        }

        private readonly struct ModulePixels
        {
            public ModulePixels(int width, int height, Color32[] pixels)
            {
                Width = width;
                Height = height;
                Pixels = pixels;
            }

            public int Width { get; }
            public int Height { get; }
            public Color32[] Pixels { get; }
        }
    }
}
