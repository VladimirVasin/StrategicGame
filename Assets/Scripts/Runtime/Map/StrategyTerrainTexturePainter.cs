using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static class StrategyTerrainTexturePainter
    {
        private const int VariantCount = 6;

        public static void PaintTile(
            Texture2D texture,
            CityMapCell[,] cells,
            int cellX,
            int cellY,
            int tilePixels,
            int seed,
            bool drawGrid)
        {
            CityMapCellKind kind = cells[cellX, cellY].Kind;
            int startX = cellX * tilePixels;
            int startY = cellY * tilePixels;
            int variant = Hash(seed, cellX, cellY, (int)kind, 19) % VariantCount;

            for (int py = 0; py < tilePixels; py++)
            {
                for (int px = 0; px < tilePixels; px++)
                {
                    Color pixel = PaintBasePixel(kind, variant, seed, cellX, cellY, px, py, tilePixels);
                    pixel = ApplyNeighborTransitions(pixel, cells, cellX, cellY, px, py, tilePixels, seed);

                    if (drawGrid && (px == 0 || py == 0))
                    {
                        pixel = Color.Lerp(pixel, Color.black, kind == CityMapCellKind.Water ? 0.10f : 0.15f);
                    }

                    pixel.a = 1f;
                    texture.SetPixel(startX + px, startY + py, pixel);
                }
            }
        }

        private static Color PaintBasePixel(
            CityMapCellKind kind,
            int variant,
            int seed,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels)
        {
            float noise = Hash01(seed, cellX, cellY, px, py, 1);
            float detail = Hash01(seed, cellX, cellY, px, py, 2);
            Color color = GetBaseColor(kind, variant);
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
            CityMapCell[,] cells,
            int cellX,
            int cellY,
            int px,
            int py,
            int tilePixels,
            int seed)
        {
            CityMapCellKind kind = cells[cellX, cellY].Kind;
            int max = tilePixels - 1;

            color = BlendSide(color, kind, GetKind(cells, cellX, cellY + 1, kind), max - py, tilePixels, seed, cellX, cellY, px, py, 11);
            color = BlendSide(color, kind, GetKind(cells, cellX, cellY - 1, kind), py, tilePixels, seed, cellX, cellY, px, py, 13);
            color = BlendSide(color, kind, GetKind(cells, cellX - 1, cellY, kind), px, tilePixels, seed, cellX, cellY, px, py, 17);
            color = BlendSide(color, kind, GetKind(cells, cellX + 1, cellY, kind), max - px, tilePixels, seed, cellX, cellY, px, py, 19);

            color = BlendCorner(color, kind, GetKind(cells, cellX - 1, cellY + 1, kind), px, max - py, tilePixels, seed, cellX, cellY, px, py, 23);
            color = BlendCorner(color, kind, GetKind(cells, cellX + 1, cellY + 1, kind), max - px, max - py, tilePixels, seed, cellX, cellY, px, py, 29);
            color = BlendCorner(color, kind, GetKind(cells, cellX - 1, cellY - 1, kind), px, py, tilePixels, seed, cellX, cellY, px, py, 31);
            color = BlendCorner(color, kind, GetKind(cells, cellX + 1, cellY - 1, kind), max - px, py, tilePixels, seed, cellX, cellY, px, py, 37);
            return color;
        }

        private static Color BlendSide(
            Color color,
            CityMapCellKind kind,
            CityMapCellKind neighbor,
            int distanceToEdge,
            int tilePixels,
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

            int width = Mathf.Max(3, tilePixels / 4);
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
            int tilePixels,
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

            int width = Mathf.Max(3, tilePixels / 5);
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

        private static CityMapCellKind GetKind(CityMapCell[,] cells, int x, int y, CityMapCellKind fallback)
        {
            return x >= 0 && y >= 0 && x < cells.GetLength(0) && y < cells.GetLength(1)
                ? cells[x, y].Kind
                : fallback;
        }

        private static Color GetBaseColor(CityMapCellKind kind, int variant)
        {
            int v = Mathf.Abs(variant) % VariantCount;
            return kind switch
            {
                CityMapCellKind.Water => v switch
                {
                    0 => Rgb(38, 105, 157),
                    1 => Rgb(43, 116, 169),
                    2 => Rgb(32, 96, 148),
                    3 => Rgb(50, 124, 171),
                    4 => Rgb(35, 89, 136),
                    _ => Rgb(45, 109, 161)
                },
                CityMapCellKind.Shore => v switch
                {
                    0 => Rgb(155, 158, 83),
                    1 => Rgb(173, 155, 90),
                    2 => Rgb(144, 151, 78),
                    3 => Rgb(186, 170, 104),
                    4 => Rgb(136, 148, 86),
                    _ => Rgb(164, 159, 94)
                },
                CityMapCellKind.Forest => v switch
                {
                    0 => Rgb(40, 83, 43),
                    1 => Rgb(35, 77, 39),
                    2 => Rgb(46, 90, 48),
                    3 => Rgb(33, 68, 38),
                    4 => Rgb(50, 87, 42),
                    _ => Rgb(39, 80, 45)
                },
                CityMapCellKind.Meadow => v switch
                {
                    0 => Rgb(103, 151, 70),
                    1 => Rgb(112, 164, 75),
                    2 => Rgb(96, 145, 67),
                    3 => Rgb(123, 169, 80),
                    4 => Rgb(91, 138, 65),
                    _ => Rgb(109, 157, 73)
                },
                CityMapCellKind.Dirt => v switch
                {
                    0 => Rgb(128, 90, 55),
                    1 => Rgb(141, 101, 61),
                    2 => Rgb(112, 80, 50),
                    3 => Rgb(153, 111, 67),
                    4 => Rgb(120, 86, 58),
                    _ => Rgb(136, 96, 58)
                },
                _ => v switch
                {
                    0 => Rgb(72, 125, 58),
                    1 => Rgb(81, 133, 62),
                    2 => Rgb(65, 116, 54),
                    3 => Rgb(88, 140, 66),
                    4 => Rgb(69, 120, 57),
                    _ => Rgb(78, 130, 61)
                }
            };
        }

        private static Color GetTransitionColor(CityMapCellKind kind, CityMapCellKind neighbor)
        {
            if (kind == CityMapCellKind.Shore || neighbor == CityMapCellKind.Shore)
            {
                return Rgb(174, 160, 96);
            }

            if (kind == CityMapCellKind.Dirt || neighbor == CityMapCellKind.Dirt)
            {
                return Rgb(119, 99, 58);
            }

            if (kind == CityMapCellKind.Forest || neighbor == CityMapCellKind.Forest)
            {
                return Rgb(54, 93, 47);
            }

            if (kind == CityMapCellKind.Meadow || neighbor == CityMapCellKind.Meadow)
            {
                return Rgb(111, 154, 72);
            }

            return Rgb(75, 128, 61);
        }

        private static float GetNoiseStrength(CityMapCellKind kind)
        {
            return kind switch
            {
                CityMapCellKind.Water => 0.08f,
                CityMapCellKind.Shore => 0.12f,
                CityMapCellKind.Forest => 0.14f,
                CityMapCellKind.Dirt => 0.16f,
                CityMapCellKind.Meadow => 0.13f,
                _ => 0.11f
            };
        }

        private static Color Shift(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                1f);
        }

        private static float Hash01(int seed, int cellX, int cellY, int px, int py, int salt)
        {
            return Hash(seed, cellX * 31 + px, cellY * 31 + py, salt, px * 7 + py * 13) / (float)int.MaxValue;
        }

        private static int Hash(int seed, int a, int b, int c, int d)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + a * 668265263;
                h = h * 1274126177 + b * 461845907;
                h = h * 1103515245 + c * 12345;
                h = h * 1597334677 + d * 381201580;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & int.MaxValue;
            }
        }

        private static Color Rgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 255);
        }
    }
}
