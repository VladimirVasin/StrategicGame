using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class CityMapController
    {
        private const float HillReliefThreshold = 0.48f;
        private const float MountainReliefThreshold = 0.72f;

        private float PickReliefHeight(
            int x,
            int y,
            MapGenerationProfile profile,
            CityMapCellKind kind,
            CityMapWaterKind waterKind)
        {
            if (kind == CityMapCellKind.Water)
            {
                return waterKind == CityMapWaterKind.River ? 0.025f : 0.04f;
            }

            if (kind == CityMapCellKind.Shore)
            {
                float shoreNoise = FractalNoise(x, y, profile.ReliefOffset, profile.ReliefScale * 1.7f, 2, 0.45f);
                return 0.14f + shoreNoise * 0.10f;
            }

            float broad = FractalNoise(x, y, profile.ReliefOffset, profile.ReliefScale, 4, 0.58f);
            float ridgeNoise = FractalNoise(x, y, profile.ReliefRidgeOffset, profile.ReliefRidgeScale, 3, 0.52f);
            float ridges = 1f - Mathf.Abs(ridgeNoise * 2f - 1f);
            float edgeDistance = Mathf.Min(
                Mathf.Min(x, width - 1 - x),
                Mathf.Min(y, height - 1 - y));
            float edgeLift = Mathf.Clamp01(1f - edgeDistance / Mathf.Max(1f, Mathf.Min(width, height) * 0.30f));
            float waterLowland = CalculateWaterLowland(x, y, profile);
            float relief = 0.16f
                + broad * 0.55f
                + ridges * 0.28f
                + edgeLift * profile.ReliefMountainBias
                - waterLowland * 0.18f;

            if (kind == CityMapCellKind.Dirt)
            {
                relief += 0.10f;
            }
            else if (kind == CityMapCellKind.Forest)
            {
                relief += 0.06f;
            }
            else if (kind == CityMapCellKind.Meadow)
            {
                relief -= 0.02f;
            }

            return Mathf.Clamp01(relief);
        }

        private float CalculateWaterLowland(int x, int y, MapGenerationProfile profile)
        {
            const int radius = 6;
            float nearest = radius + 1f;
            for (int oy = -radius; oy <= radius; oy++)
            {
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int nx = x + ox;
                    int ny = y + oy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    if (!TryPickWaterKind(nx, ny, profile, out CityMapCellKind waterKind, out _)
                        || waterKind != CityMapCellKind.Water)
                    {
                        continue;
                    }

                    float distance = Mathf.Sqrt(ox * ox + oy * oy);
                    if (distance < nearest)
                    {
                        nearest = distance;
                    }
                }
            }

            return nearest <= radius ? 1f - nearest / radius : 0f;
        }

        private void CountRelief(out int hillCells, out int mountainCells)
        {
            hillCells = 0;
            mountainCells = 0;
            if (cells == null)
            {
                return;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CityMapCell cell = cells[x, y];
                    if (cell.IsWater || cell.IsShore)
                    {
                        continue;
                    }

                    if (cell.ReliefHeight >= MountainReliefThreshold)
                    {
                        mountainCells++;
                    }
                    else if (cell.ReliefHeight >= HillReliefThreshold)
                    {
                        hillCells++;
                    }
                }
            }
        }
    }
}
