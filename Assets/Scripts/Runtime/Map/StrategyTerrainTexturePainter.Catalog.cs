using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyTerrainTexturePainter
    {
        private const int CatalogTilePixels = 16;

        private static Color32[][] catalogPixels;
        private static int[] catalogWidths;
        private static int[] catalogHeights;
        private static bool catalogPrepared;

        public static void PrewarmCatalog(StrategyVisualCatalog catalog)
        {
            if (catalogPrepared || catalog == null)
            {
                return;
            }

            int kindCount = System.Enum.GetValues(typeof(CityMapCellKind)).Length;
            Color32[][] preparedPixels = new Color32[kindCount * VariantCount][];
            int[] preparedWidths = new int[preparedPixels.Length];
            int[] preparedHeights = new int[preparedPixels.Length];
            for (int kindIndex = 0; kindIndex < kindCount; kindIndex++)
            {
                CityMapCellKind kind = (CityMapCellKind)kindIndex;
                for (int variant = 0; variant < VariantCount; variant++)
                {
                    if (!catalog.TryGetTerrainSprite(kind, variant, out Sprite sprite)
                        || sprite == null
                        || sprite.texture == null
                        || !sprite.texture.isReadable)
                    {
                        continue;
                    }

                    int index = GetCatalogIndex(kind, variant);
                    Rect rect = sprite.rect;
                    int width = Mathf.RoundToInt(rect.width);
                    int height = Mathf.RoundToInt(rect.height);
                    preparedPixels[index] = sprite.texture.GetPixels32();
                    preparedWidths[index] = width;
                    preparedHeights[index] = height;
                }
            }

            catalogPixels = preparedPixels;
            catalogWidths = preparedWidths;
            catalogHeights = preparedHeights;
            catalogPrepared = true;
        }

        public static Sprite CreateCatalogSwatchSprite(CityMapCellKind kind, int variant)
        {
            int normalizedVariant = Mathf.Abs(variant) % VariantCount;
            Texture2D texture = new(CatalogTilePixels, CatalogTilePixels, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Terrain {kind} V{normalizedVariant + 1}"
            };
            int seed = 8171 + (int)kind * 131 + normalizedVariant * 977;
            for (int y = 0; y < CatalogTilePixels; y++)
            {
                for (int x = 0; x < CatalogTilePixels; x++)
                {
                    Color color = PaintProceduralBasePixel(
                        kind,
                        normalizedVariant,
                        0.5f,
                        seed,
                        normalizedVariant * 3,
                        (int)kind * 5,
                        x,
                        y,
                        CatalogTilePixels);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, CatalogTilePixels, CatalogTilePixels),
                new Vector2(0.5f, 0.5f),
                CatalogTilePixels);
            sprite.name = texture.name + " Sprite";
            return sprite;
        }

        private static bool TrySampleCatalog(
            CityMapCellKind kind,
            int variant,
            int px,
            int py,
            int tilePixels,
            out Color color)
        {
            Color32[][] pixelsByTile = catalogPixels;
            int[] widths = catalogWidths;
            int[] heights = catalogHeights;
            int index = GetCatalogIndex(kind, variant);
            if (!catalogPrepared
                || pixelsByTile == null
                || index < 0
                || index >= pixelsByTile.Length
                || pixelsByTile[index] == null
                || widths[index] <= 0
                || heights[index] <= 0)
            {
                color = default;
                return false;
            }

            int sampleX = Mathf.Clamp(px * widths[index] / Mathf.Max(1, tilePixels), 0, widths[index] - 1);
            int sampleY = Mathf.Clamp(py * heights[index] / Mathf.Max(1, tilePixels), 0, heights[index] - 1);
            color = pixelsByTile[index][sampleY * widths[index] + sampleX];
            return true;
        }

        private static int GetCatalogIndex(CityMapCellKind kind, int variant)
        {
            int normalizedVariant = Mathf.Abs(variant) % VariantCount;
            return (int)kind * VariantCount + normalizedVariant;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCatalogCache()
        {
            catalogPixels = null;
            catalogWidths = null;
            catalogHeights = null;
            catalogPrepared = false;
        }
    }
}
