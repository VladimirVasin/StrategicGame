using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyTerrainTexturePainter
    {
        private const int VariantCount = 6;

        public static void PaintTile(
            Color32[] pixels,
            int textureWidth,
            CityMapCell[,] cells,
            int cellX,
            int cellY,
            int tilePixels,
            int seed,
            bool drawGrid)
        {
            CityMapCell cell = cells[cellX, cellY];
            CityMapCellKind kind = cell.Kind;
            int startX = cellX * tilePixels;
            int startY = cellY * tilePixels;
            int variant = Hash(seed, cellX, cellY, (int)kind, 19) % VariantCount;
            float macroVariation = SampleMacroVariation(seed, cellX, cellY, kind);
            float macroShift = (macroVariation - 0.5f) * GetMacroStrength(kind);
            TilePaintContext context = CreateTilePaintContext(
                cells,
                cell,
                cellX,
                cellY,
                tilePixels,
                kind,
                variant);
            CatalogTileSample catalog = context.Catalog;
            bool hasCatalog = catalog.IsAvailable;
            ReliefPaintContext relief = context.Relief;

            for (int py = 0; py < tilePixels; py++)
            {
                int pixelIndex = (startY + py) * textureWidth + startX;
                int catalogRow = hasCatalog ? catalog.GetRowOffset(py, tilePixels) : 0;
                for (int px = 0; px < tilePixels; px++)
                {
                    Color pixel = PaintBasePixel(
                        kind,
                        in catalog,
                        catalogRow,
                        macroShift,
                        variant,
                        macroVariation,
                        seed,
                        cellX,
                        cellY,
                        px,
                        py,
                        tilePixels);
                    pixel = ApplyNeighborTransitions(pixel, in context, cellX, cellY, px, py, seed);
                    pixel = ApplyReliefShading(pixel, in relief, cellX, cellY, px, py, tilePixels, seed);

                    if (drawGrid && (px == 0 || py == 0))
                    {
                        pixel = Color.Lerp(pixel, Color.black, kind == CityMapCellKind.Water ? 0.10f : 0.15f);
                    }

                    pixel.a = 1f;
                    pixels[pixelIndex + px] = pixel;
                }
            }
        }

        private static Color PaintBasePixel(
            CityMapCellKind kind,
            in CatalogTileSample catalog,
            int catalogRow,
            float macroShift,
            int variant,
            float macroVariation,
            int seed,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels)
        {
            if (catalog.IsAvailable)
            {
                return Shift(catalog.Sample(catalogRow, px, tilePixels), macroShift);
            }

            return PaintProceduralBasePixel(
                kind,
                variant,
                macroVariation,
                seed,
                cellX,
                cellY,
                px,
                py,
                tilePixels);
        }

        private static Color PaintProceduralBasePixel(
            CityMapCellKind kind,
            int variant,
            float macroVariation,
            int seed,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels)
        {
            float noise = Hash01(seed, cellX, cellY, px, py, 1);
            float detail = Hash01(seed, cellX, cellY, px, py, 2);
            Color color = Color.Lerp(GetBaseColor(kind, variant), GetBaseColor(kind, 0), 0.74f);
            color = Shift(color, (macroVariation - 0.5f) * GetMacroStrength(kind));
            color = Shift(color, (noise - 0.5f) * GetNoiseStrength(kind));

            switch (kind)
            {
                case CityMapCellKind.Water:
                    return PaintWaterPixel(color, variant, noise, detail, px, py);
                case CityMapCellKind.Shore:
                    return PaintShorePixel(color, variant, noise, detail, px, py);
                case CityMapCellKind.Forest:
                    return PaintForestPixel(color, variant, noise, detail, px, py);
                case CityMapCellKind.Meadow:
                    return PaintMeadowPixel(color, variant, noise, detail, px, py, tilePixels);
                case CityMapCellKind.Dirt:
                    return PaintDirtPixel(color, variant, noise, detail, px, py);
                default:
                    return PaintGrassPixel(color, variant, noise, detail, px, py, tilePixels);
            }
        }

        private static Color PaintGrassPixel(Color color, int variant, float noise, float detail, int px, int py, int tilePixels)
        {
            if (noise < 0.08f)
            {
                color = Color.Lerp(color, Rgb(42, 88, 39), 0.42f);
            }

            if (noise > 0.92f)
            {
                color = Color.Lerp(color, Rgb(104, 146, 68), 0.36f);
            }

            bool stem = ((px + variant * 3) % 7 == 0 || (px + py + variant) % 13 == 0)
                && detail > 0.62f
                && py > 1
                && py < tilePixels - 1;
            if (stem)
            {
                color = Color.Lerp(color, Rgb(116, 158, 75), 0.52f);
            }

            return color;
        }

        private static Color PaintMeadowPixel(Color color, int variant, float noise, float detail, int px, int py, int tilePixels)
        {
            if (noise < 0.10f)
            {
                color = Color.Lerp(color, Rgb(72, 113, 45), 0.35f);
            }

            bool tallGrass = ((px + variant) % 5 == 0 || (px + py + variant * 2) % 11 == 0)
                && detail > 0.50f
                && py > 1
                && py < tilePixels - 1;
            if (tallGrass)
            {
                color = Color.Lerp(color, Rgb(136, 174, 77), 0.48f);
            }

            if (noise > 0.965f)
            {
                color = detail > 0.5f ? Rgb(218, 194, 82) : Rgb(172, 138, 190);
            }

            return color;
        }

        private static Color PaintForestPixel(Color color, int variant, float noise, float detail, int px, int py)
        {
            if (noise < 0.16f)
            {
                color = Color.Lerp(color, Rgb(31, 61, 34), 0.56f);
            }

            if (noise > 0.88f)
            {
                color = Color.Lerp(color, Rgb(72, 105, 46), 0.36f);
            }

            bool leaf = (px + py + variant * 3) % 8 == 0 && detail > 0.44f;
            if (leaf)
            {
                color = Color.Lerp(color, Rgb(88, 73, 43), 0.48f);
            }

            bool root = (px - py + variant * 2) % 13 == 0 && detail < 0.18f;
            if (root)
            {
                color = Color.Lerp(color, Rgb(63, 46, 31), 0.55f);
            }

            return color;
        }

        private static Color PaintDirtPixel(Color color, int variant, float noise, float detail, int px, int py)
        {
            if (noise < 0.13f)
            {
                color = Color.Lerp(color, Rgb(102, 72, 45), 0.42f);
            }

            if (noise > 0.90f)
            {
                color = Color.Lerp(color, Rgb(157, 116, 70), 0.35f);
            }

            bool crack = ((px + variant * 4) % 12 == 0 && (py + variant) % 5 < 2)
                || ((px - py + variant * 3) % 17 == 0 && detail < 0.30f);
            if (crack)
            {
                color = Color.Lerp(color, Rgb(73, 52, 35), 0.58f);
            }

            if (noise > 0.965f)
            {
                color = Rgb(128, 121, 96);
            }

            return color;
        }

        private static Color PaintShorePixel(Color color, int variant, float noise, float detail, int px, int py)
        {
            if (noise < 0.12f)
            {
                color = Color.Lerp(color, Rgb(121, 136, 74), 0.34f);
            }

            if (noise > 0.86f)
            {
                color = Color.Lerp(color, Rgb(202, 177, 103), 0.38f);
            }

            bool pebble = (px + py + variant) % 15 == 0 && detail > 0.70f;
            if (pebble)
            {
                color = Rgb(107, 107, 83);
            }

            return color;
        }

        private static Color PaintWaterPixel(Color color, int variant, float noise, float detail, int px, int py)
        {
            if ((py + variant * 2) % 5 == 0 && noise > 0.35f)
            {
                color = Color.Lerp(color, Rgb(69, 137, 177), 0.45f);
            }

            if ((px + variant) % 9 == 0 && (py + variant * 3) % 4 == 0 && detail > 0.52f)
            {
                color = Color.Lerp(color, Rgb(121, 181, 204), 0.55f);
            }

            if (noise < 0.08f)
            {
                color = Color.Lerp(color, Rgb(31, 87, 132), 0.42f);
            }

            return color;
        }

        private static Color ApplyNeighborTransitions(
            Color color,
            in TilePaintContext context,
            int cellX,
            int cellY,
            int px,
            int py,
            int seed)
        {
            if (!context.HasTransitions)
            {
                return color;
            }

            CityMapCellKind kind = context.Kind;
            int max = context.MaxPixel;
            if (context.North != kind)
            {
                color = BlendSide(color, kind, context.North, max - py, context.SideWidth, seed, cellX, cellY, px, py, 11);
            }

            if (context.South != kind)
            {
                color = BlendSide(color, kind, context.South, py, context.SideWidth, seed, cellX, cellY, px, py, 13);
            }

            if (context.West != kind)
            {
                color = BlendSide(color, kind, context.West, px, context.SideWidth, seed, cellX, cellY, px, py, 17);
            }

            if (context.East != kind)
            {
                color = BlendSide(color, kind, context.East, max - px, context.SideWidth, seed, cellX, cellY, px, py, 19);
            }

            if (context.NorthWest != kind)
            {
                color = BlendCorner(color, kind, context.NorthWest, px, max - py, context.CornerWidth, seed, cellX, cellY, px, py, 23);
            }

            if (context.NorthEast != kind)
            {
                color = BlendCorner(color, kind, context.NorthEast, max - px, max - py, context.CornerWidth, seed, cellX, cellY, px, py, 29);
            }

            if (context.SouthWest != kind)
            {
                color = BlendCorner(color, kind, context.SouthWest, px, py, context.CornerWidth, seed, cellX, cellY, px, py, 31);
            }

            if (context.SouthEast != kind)
            {
                color = BlendCorner(color, kind, context.SouthEast, max - px, py, context.CornerWidth, seed, cellX, cellY, px, py, 37);
            }

            return color;
        }

        private static Color BlendSide(
            Color color,
            CityMapCellKind kind,
            CityMapCellKind neighbor,
            int distanceToEdge,
            int width,
            int seed,
            int cellX,
            int cellY,
            int px,
            int py,
            int salt)
        {
            if (kind == neighbor)
            {
                return color;
            }

            if (distanceToEdge >= width)
            {
                return color;
            }

            float edge = (width - distanceToEdge) / (float)width;
            float ragged = Hash01(seed, cellX, cellY, px, py, salt);
            if (ragged + edge < 0.64f)
            {
                return color;
            }

            if (kind == CityMapCellKind.Water)
            {
                Color foam = neighbor == CityMapCellKind.Shore ? Rgb(185, 219, 215) : Rgb(91, 150, 183);
                return Color.Lerp(color, foam, Mathf.Clamp01(edge * 0.58f));
            }

            if (neighbor == CityMapCellKind.Water)
            {
                Color wet = kind == CityMapCellKind.Shore ? Rgb(204, 184, 116) : Rgb(103, 135, 104);
                Color foam = Rgb(218, 227, 204);
                color = Color.Lerp(color, wet, Mathf.Clamp01(edge * 0.48f));
                if (distanceToEdge <= 1 && ragged > 0.50f)
                {
                    color = Color.Lerp(color, foam, 0.36f);
                }

                return color;
            }

            Color transition = GetTransitionColor(kind, neighbor);
            float alpha = Mathf.Clamp01(edge * (0.24f + ragged * 0.22f));
            return Color.Lerp(color, transition, alpha);
        }

        private static Color BlendCorner(
            Color color,
            CityMapCellKind kind,
            CityMapCellKind neighbor,
            int distanceX,
            int distanceY,
            int width,
            int seed,
            int cellX,
            int cellY,
            int px,
            int py,
            int salt)
        {
            if (kind == neighbor)
            {
                return color;
            }

            if (distanceX >= width || distanceY >= width)
            {
                return color;
            }

            float corner = Mathf.Min(width - distanceX, width - distanceY) / (float)width;
            float ragged = Hash01(seed, cellX, cellY, px, py, salt);
            if (ragged + corner < 0.74f)
            {
                return color;
            }

            Color transition = neighbor == CityMapCellKind.Water
                ? Rgb(188, 210, 183)
                : GetTransitionColor(kind, neighbor);
            return Color.Lerp(color, transition, Mathf.Clamp01(corner * 0.24f));
        }

    }
}
