using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class CityMapController
    {
        public bool IsCellWalkable(Vector2Int cell)
        {
            return IsCellWalkable(cell.x, cell.y);
        }

        public bool IsCellWalkable(int x, int y)
        {
            return TryGetCell(x, y, out CityMapCell cell)
                && (cell.IsBuildable || IsBridgeWalkableCell(x, y))
                && (blockedWalkCounts == null || blockedWalkCounts[x, y] <= 0);
        }

        public bool IsCellBuildable(Vector2Int cell)
        {
            return IsCellBuildable(cell.x, cell.y);
        }

        public bool IsCellBuildable(int x, int y)
        {
            return TryGetCell(x, y, out CityMapCell cell)
                && cell.IsBuildable
                && (blockedBuildCounts == null || blockedBuildCounts[x, y] <= 0);
        }

        public void SetCellsWalkable(Vector2Int origin, Vector2Int size, bool isWalkable)
        {
            EnsureWalkabilityLayer();
            ApplyCellCounter(origin, size, isWalkable, blockedWalkCounts);
            WalkabilityVersion++;
            StrategyTrailController.Active?.RefreshArea(origin, size);
        }

        public void SetCellsBuildable(Vector2Int origin, Vector2Int size, bool isBuildable)
        {
            EnsureBuildabilityLayer();
            ApplyCellCounter(origin, size, isBuildable, blockedBuildCounts);
            StrategyTrailController.Active?.RefreshArea(origin, size);
        }

        public void SetBridgeCellsWalkable(IReadOnlyList<Vector2Int> bridgeCells, bool isWalkable)
        {
            if (bridgeCells == null)
            {
                return;
            }

            EnsureBridgeWalkabilityLayer();
            bool changed = false;
            for (int i = 0; i < bridgeCells.Count; i++)
            {
                Vector2Int cell = bridgeCells[i];
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
                {
                    continue;
                }

                if (bridgeWalkableCells[cell.x, cell.y] == isWalkable)
                {
                    continue;
                }

                bridgeWalkableCells[cell.x, cell.y] = isWalkable;
                changed = true;
                StrategyTrailController.Active?.RefreshArea(cell, Vector2Int.one);
            }

            if (changed)
            {
                WalkabilityVersion++;
            }
        }

        public bool IsBridgeWalkableCell(Vector2Int cell)
        {
            return IsBridgeWalkableCell(cell.x, cell.y);
        }

        public bool IsBridgeWalkableCell(int x, int y)
        {
            return bridgeWalkableCells != null
                && x >= 0
                && x < width
                && y >= 0
                && y < height
                && bridgeWalkableCells[x, y];
        }

        private void ApplyCellCounter(Vector2Int origin, Vector2Int size, bool release, int[,] counts)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int cellX = origin.x + x;
                    int cellY = origin.y + y;
                    if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height)
                    {
                        continue;
                    }

                    counts[cellX, cellY] = release
                        ? Mathf.Max(0, counts[cellX, cellY] - 1)
                        : counts[cellX, cellY] + 1;
                }
            }
        }

        private void EnsureWalkabilityLayer()
        {
            if (blockedWalkCounts == null
                || blockedWalkCounts.GetLength(0) != width
                || blockedWalkCounts.GetLength(1) != height)
            {
                blockedWalkCounts = new int[width, height];
            }
        }

        private void EnsureBuildabilityLayer()
        {
            if (blockedBuildCounts == null
                || blockedBuildCounts.GetLength(0) != width
                || blockedBuildCounts.GetLength(1) != height)
            {
                blockedBuildCounts = new int[width, height];
            }
        }

        private void EnsureBridgeWalkabilityLayer()
        {
            if (bridgeWalkableCells == null
                || bridgeWalkableCells.GetLength(0) != width
                || bridgeWalkableCells.GetLength(1) != height)
            {
                bridgeWalkableCells = new bool[width, height];
            }
        }
    }
}
