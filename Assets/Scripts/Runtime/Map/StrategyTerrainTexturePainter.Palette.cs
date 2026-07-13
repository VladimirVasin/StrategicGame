using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyTerrainTexturePainter
    {
        private static Color GetBaseColor(CityMapCellKind kind, int variant)
        {
            int v = Mathf.Abs(variant) % VariantCount;
            return kind switch
            {
                CityMapCellKind.Water => v switch
                {
                    0 => Rgb(38, 105, 157), 1 => Rgb(43, 116, 169),
                    2 => Rgb(32, 96, 148), 3 => Rgb(50, 124, 171),
                    4 => Rgb(35, 89, 136), _ => Rgb(45, 109, 161)
                },
                CityMapCellKind.Shore => v switch
                {
                    0 => Rgb(155, 158, 83), 1 => Rgb(173, 155, 90),
                    2 => Rgb(144, 151, 78), 3 => Rgb(186, 170, 104),
                    4 => Rgb(136, 148, 86), _ => Rgb(164, 159, 94)
                },
                CityMapCellKind.Forest => v switch
                {
                    0 => Rgb(40, 83, 43), 1 => Rgb(35, 77, 39),
                    2 => Rgb(46, 90, 48), 3 => Rgb(33, 68, 38),
                    4 => Rgb(50, 87, 42), _ => Rgb(39, 80, 45)
                },
                CityMapCellKind.Meadow => v switch
                {
                    0 => Rgb(103, 151, 70), 1 => Rgb(112, 164, 75),
                    2 => Rgb(96, 145, 67), 3 => Rgb(123, 169, 80),
                    4 => Rgb(91, 138, 65), _ => Rgb(109, 157, 73)
                },
                CityMapCellKind.Dirt => v switch
                {
                    0 => Rgb(128, 90, 55), 1 => Rgb(141, 101, 61),
                    2 => Rgb(112, 80, 50), 3 => Rgb(153, 111, 67),
                    4 => Rgb(120, 86, 58), _ => Rgb(136, 96, 58)
                },
                _ => v switch
                {
                    0 => Rgb(72, 125, 58), 1 => Rgb(81, 133, 62),
                    2 => Rgb(65, 116, 54), 3 => Rgb(88, 140, 66),
                    4 => Rgb(69, 120, 57), _ => Rgb(78, 130, 61)
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

            return kind == CityMapCellKind.Meadow || neighbor == CityMapCellKind.Meadow
                ? Rgb(111, 154, 72)
                : Rgb(75, 128, 61);
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
            return Hash(seed, cellX * 31 + px, cellY * 31 + py, salt, px * 7 + py * 13)
                / (float)int.MaxValue;
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
