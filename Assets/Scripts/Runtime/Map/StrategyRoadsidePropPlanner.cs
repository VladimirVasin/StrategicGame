using System;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal readonly struct StrategyRoadsidePropPlacement
    {
        public StrategyRoadsidePropPlacement(Vector2Int roadCell, Vector2Int sideOffset, Vector3 worldPosition, int variant)
        {
            RoadCell = roadCell;
            SideOffset = sideOffset;
            WorldPosition = worldPosition;
            Variant = variant;
        }

        public Vector2Int RoadCell { get; }
        public Vector2Int SideOffset { get; }
        public Vector3 WorldPosition { get; }
        public int Variant { get; }
    }

    internal static class StrategyRoadsidePropPlanner
    {
        private const int TorchCadence = 6;

        public static bool TryGetTorchPlacement(
            Vector2Int cell,
            int seed,
            Func<Vector2Int, bool> isRoadCell,
            Func<Vector2Int, bool> canPlaceSideCell,
            Func<Vector2Int, Vector3> getCellCenter,
            float sideDistance,
            out StrategyRoadsidePropPlacement placement)
        {
            placement = default;
            if (isRoadCell == null || canPlaceSideCell == null || getCellCenter == null || !isRoadCell(cell))
            {
                return false;
            }

            bool west = isRoadCell(cell + Vector2Int.left);
            bool east = isRoadCell(cell + Vector2Int.right);
            bool south = isRoadCell(cell + Vector2Int.down);
            bool north = isRoadCell(cell + Vector2Int.up);
            bool horizontal = west && east && !south && !north;
            bool vertical = north && south && !west && !east;
            if (!horizontal && !vertical)
            {
                return false;
            }

            int axis = horizontal ? cell.x : cell.y;
            int seedOffset = Mathf.Abs(seed % TorchCadence);
            if (Mathf.Abs(axis + seedOffset) % TorchCadence != 0)
            {
                return false;
            }

            Vector2Int sideOffset = GetPreferredSideOffset(cell, seed, horizontal, axis);
            if (!canPlaceSideCell(cell + sideOffset))
            {
                sideOffset = -sideOffset;
                if (!canPlaceSideCell(cell + sideOffset))
                {
                    return false;
                }
            }

            Vector3 center = getCellCenter(cell);
            Vector3 side = new Vector3(sideOffset.x * sideDistance, sideOffset.y * sideDistance, -0.18f);
            int variant = (Hash(seed, cell.x, cell.y, 1181) & int.MaxValue) % StrategyBuildingLightSpriteFactory.FrameCount;
            placement = new StrategyRoadsidePropPlacement(cell, sideOffset, center + side, variant);
            return true;
        }

        private static Vector2Int GetPreferredSideOffset(Vector2Int cell, int seed, bool horizontal, int axis)
        {
            int lane = Mathf.Abs((axis / TorchCadence) + seed + cell.x * 3 + cell.y * 5) & 1;
            if (horizontal)
            {
                return lane == 0 ? Vector2Int.up : Vector2Int.down;
            }

            return lane == 0 ? Vector2Int.right : Vector2Int.left;
        }

        private static int Hash(int seed, int x, int y, int salt)
        {
            unchecked
            {
                int h = seed;
                h = h * 374761393 + x * 668265263;
                h = h * 1274126177 + y * 461845907;
                h ^= salt * 83492791;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
