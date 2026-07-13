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

        private static CatalogTileSample ResolveCatalogTile(CityMapCellKind kind, int variant)
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
                return default;
            }

            return new CatalogTileSample(pixelsByTile[index], widths[index], heights[index]);
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

        private readonly struct CatalogTileSample
        {
            private readonly Color32[] pixels;

            public CatalogTileSample(Color32[] pixels, int width, int height)
            {
                this.pixels = pixels;
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }
            public bool IsAvailable => pixels != null;

            public int GetRowOffset(int py, int tilePixels)
            {
                int sampleY = Height == tilePixels
                    ? py
                    : Mathf.Clamp(py * Height / Mathf.Max(1, tilePixels), 0, Height - 1);
                return sampleY * Width;
            }

            public Color Sample(int rowOffset, int px, int tilePixels)
            {
                int sampleX = Width == tilePixels
                    ? px
                    : Mathf.Clamp(px * Width / Mathf.Max(1, tilePixels), 0, Width - 1);
                return pixels[rowOffset + sampleX];
            }
        }

        private static TilePaintContext CreateTilePaintContext(
            CityMapCell[,] cells,
            CityMapCell cell,
            int cellX,
            int cellY,
            int tilePixels,
            CityMapCellKind kind,
            int variant)
        {
            int mapWidth = cells.GetLength(0);
            int mapHeight = cells.GetLength(1);
            CityMapCellKind north = GetKind(cells, cellX, cellY + 1, mapWidth, mapHeight, kind);
            CityMapCellKind south = GetKind(cells, cellX, cellY - 1, mapWidth, mapHeight, kind);
            CityMapCellKind west = GetKind(cells, cellX - 1, cellY, mapWidth, mapHeight, kind);
            CityMapCellKind east = GetKind(cells, cellX + 1, cellY, mapWidth, mapHeight, kind);
            CityMapCellKind northWest = GetKind(cells, cellX - 1, cellY + 1, mapWidth, mapHeight, kind);
            CityMapCellKind northEast = GetKind(cells, cellX + 1, cellY + 1, mapWidth, mapHeight, kind);
            CityMapCellKind southWest = GetKind(cells, cellX - 1, cellY - 1, mapWidth, mapHeight, kind);
            CityMapCellKind southEast = GetKind(cells, cellX + 1, cellY - 1, mapWidth, mapHeight, kind);
            return new TilePaintContext(
                kind,
                north,
                south,
                west,
                east,
                northWest,
                northEast,
                southWest,
                southEast,
                tilePixels - 1,
                Mathf.Max(3, tilePixels / 4),
                Mathf.Max(3, tilePixels / 5),
                ResolveCatalogTile(kind, variant),
                CreateReliefContext(cells, cell, cellX, cellY, mapWidth, mapHeight));
        }

        private static CityMapCellKind GetKind(
            CityMapCell[,] cells,
            int x,
            int y,
            int mapWidth,
            int mapHeight,
            CityMapCellKind fallback)
        {
            return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight
                ? cells[x, y].Kind
                : fallback;
        }

        private readonly struct TilePaintContext
        {
            public TilePaintContext(
                CityMapCellKind kind,
                CityMapCellKind north,
                CityMapCellKind south,
                CityMapCellKind west,
                CityMapCellKind east,
                CityMapCellKind northWest,
                CityMapCellKind northEast,
                CityMapCellKind southWest,
                CityMapCellKind southEast,
                int maxPixel,
                int sideWidth,
                int cornerWidth,
                CatalogTileSample catalog,
                ReliefPaintContext relief)
            {
                Kind = kind;
                North = north;
                South = south;
                West = west;
                East = east;
                NorthWest = northWest;
                NorthEast = northEast;
                SouthWest = southWest;
                SouthEast = southEast;
                MaxPixel = maxPixel;
                SideWidth = sideWidth;
                CornerWidth = cornerWidth;
                Catalog = catalog;
                Relief = relief;
                HasTransitions = north != kind
                    || south != kind
                    || west != kind
                    || east != kind
                    || northWest != kind
                    || northEast != kind
                    || southWest != kind
                    || southEast != kind;
            }

            public CityMapCellKind Kind { get; }
            public CityMapCellKind North { get; }
            public CityMapCellKind South { get; }
            public CityMapCellKind West { get; }
            public CityMapCellKind East { get; }
            public CityMapCellKind NorthWest { get; }
            public CityMapCellKind NorthEast { get; }
            public CityMapCellKind SouthWest { get; }
            public CityMapCellKind SouthEast { get; }
            public int MaxPixel { get; }
            public int SideWidth { get; }
            public int CornerWidth { get; }
            public CatalogTileSample Catalog { get; }
            public ReliefPaintContext Relief { get; }
            public bool HasTransitions { get; }
        }
    }
}
