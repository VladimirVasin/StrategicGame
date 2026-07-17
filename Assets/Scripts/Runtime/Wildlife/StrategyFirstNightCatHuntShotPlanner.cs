using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyFirstNightCatHuntPhase
    {
        Unprepared,
        Prepared,
        Staged,
        Stalking,
        Pouncing,
        Celebrating,
        Completed,
        Cleaned
    }

    public readonly struct StrategyFirstNightCatHuntShotPlan
    {
        public StrategyFirstNightCatHuntShotPlan(
            Vector2Int startCell,
            Vector2Int endCell,
            Vector2Int direction,
            Vector3 catStartWorld,
            Vector3 catPounceWorld,
            Vector3 mouseStartWorld,
            Vector3 mousePounceWorld,
            Vector3 catchWorld,
            StrategyCatCoat coat,
            int mouseVariant)
        {
            StartCell = startCell;
            EndCell = endCell;
            Direction = direction;
            CatStartWorld = catStartWorld;
            CatPounceWorld = catPounceWorld;
            MouseStartWorld = mouseStartWorld;
            MousePounceWorld = mousePounceWorld;
            CatchWorld = catchWorld;
            Coat = coat;
            MouseVariant = mouseVariant;
        }

        public Vector2Int StartCell { get; }
        public Vector2Int EndCell { get; }
        public Vector2Int Direction { get; }
        public Vector3 CatStartWorld { get; }
        public Vector3 CatPounceWorld { get; }
        public Vector3 MouseStartWorld { get; }
        public Vector3 MousePounceWorld { get; }
        public Vector3 CatchWorld { get; }
        public StrategyCatCoat Coat { get; }
        public int MouseVariant { get; }
    }

    public static class StrategyFirstNightCatHuntShotPlanner
    {
        private const int MaximumSearchRadius = 8;
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down
        };

        public static bool TryCreate(
            StrategyPopulationController population,
            CityMapController map,
            out StrategyFirstNightCatHuntShotPlan plan)
        {
            plan = default;
            if (map == null)
            {
                return false;
            }

            Vector2Int anchor = population != null
                && population.TryGetCampCell(out Vector2Int campCell)
                    ? campCell
                    : new Vector2Int(map.Width / 2, map.Height / 2);
            return TryCreate(
                anchor,
                map.ActiveSeed,
                map.IsCellWalkable,
                cell => map.GetCellCenterWorld(cell.x, cell.y),
                out plan);
        }

        internal static bool TryCreate(
            Vector2Int anchor,
            int seed,
            Func<Vector2Int, bool> isWalkable,
            Func<Vector2Int, Vector3> getWorld,
            out StrategyFirstNightCatHuntShotPlan plan)
        {
            plan = default;
            if (isWalkable == null || getWorld == null)
            {
                return false;
            }

            int directionStart = PositiveModulo(seed, Directions.Length);
            int lengthStart = PositiveModulo(seed / Directions.Length, 3);
            for (int radius = 0; radius <= MaximumSearchRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int center = anchor + new Vector2Int(x, y);
                        for (int directionOffset = 0; directionOffset < Directions.Length; directionOffset++)
                        {
                            Vector2Int direction = Directions[(directionStart + directionOffset) % Directions.Length];
                            for (int lengthOffset = 0; lengthOffset < 3; lengthOffset++)
                            {
                                int length = 4 + (lengthStart + lengthOffset) % 3;
                                int startDistance = length / 2;
                                Vector2Int start = center - direction * startDistance;
                                if (!IsWalkableLine(start, direction, length, isWalkable))
                                {
                                    continue;
                                }

                                Vector2Int end = start + direction * length;
                                plan = BuildPlan(start, end, direction, seed, getWorld);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static StrategyFirstNightCatHuntShotPlan BuildPlan(
            Vector2Int start,
            Vector2Int end,
            Vector2Int direction,
            int seed,
            Func<Vector2Int, Vector3> getWorld)
        {
            Vector3 startWorld = getWorld(start);
            Vector3 endWorld = getWorld(end);
            Vector3 catStart = WithZ(Vector3.Lerp(startWorld, endWorld, 0.08f), -0.073f);
            Vector3 catPounce = WithZ(Vector3.Lerp(startWorld, endWorld, 0.58f), -0.073f);
            Vector3 mouseStart = WithZ(Vector3.Lerp(startWorld, endWorld, 0.55f), -0.071f);
            Vector3 mousePounce = WithZ(Vector3.Lerp(startWorld, endWorld, 0.72f), -0.071f);
            Vector3 catchWorld = WithZ(Vector3.Lerp(startWorld, endWorld, 0.86f), -0.072f);
            int coat = PositiveModulo(StableHash(seed, start.x, start.y, end.x), 7);
            int mouseVariant = PositiveModulo(
                StableHash(seed, end.x, end.y, start.y),
                StrategyCinematicRatSpriteFactory.VariantCount);
            return new StrategyFirstNightCatHuntShotPlan(
                start,
                end,
                direction,
                catStart,
                catPounce,
                mouseStart,
                mousePounce,
                catchWorld,
                (StrategyCatCoat)coat,
                mouseVariant);
        }

        private static bool IsWalkableLine(
            Vector2Int start,
            Vector2Int direction,
            int length,
            Func<Vector2Int, bool> isWalkable)
        {
            for (int step = 0; step <= length; step++)
            {
                if (!isWalkable(start + direction * step))
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 WithZ(Vector3 value, float z)
        {
            value.z = z;
            return value;
        }

        private static int StableHash(int a, int b, int c, int d)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + c;
                return hash * 31 + d;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
