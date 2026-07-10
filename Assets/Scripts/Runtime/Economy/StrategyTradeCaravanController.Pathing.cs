using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyTradeCaravanController
    {
        private readonly List<Vector2Int> navigationRawCells = new();
        private readonly List<Vector2Int> navigationSmoothedCells = new();
        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private List<Vector2Int> CollectEdgeCandidates(Vector2Int target)
        {
            List<Vector2Int> candidates = new();
            if (map == null)
            {
                return candidates;
            }

            for (int x = 0; x < map.Width; x++)
            {
                AddEdgeCandidate(candidates, new Vector2Int(x, 0));
                AddEdgeCandidate(candidates, new Vector2Int(x, map.Height - 1));
            }

            for (int y = 1; y < map.Height - 1; y++)
            {
                AddEdgeCandidate(candidates, new Vector2Int(0, y));
                AddEdgeCandidate(candidates, new Vector2Int(map.Width - 1, y));
            }

            candidates.Sort((left, right) =>
            {
                int leftDistance = Mathf.Abs(left.x - target.x) + Mathf.Abs(left.y - target.y);
                int rightDistance = Mathf.Abs(right.x - target.x) + Mathf.Abs(right.y - target.y);
                return leftDistance.CompareTo(rightDistance);
            });
            return candidates;
        }

        private void AddEdgeCandidate(List<Vector2Int> candidates, Vector2Int cell)
        {
            if (map != null && map.IsCellWalkable(cell))
            {
                candidates.Add(cell);
            }
        }

        private bool TryBuildCellPath(
            Vector2Int start,
            Vector2Int target,
            out List<Vector2Int> cellPath)
        {
            cellPath = null;
            if (map == null || !map.IsCellWalkable(start) || !map.IsCellWalkable(target))
            {
                return false;
            }

            StrategyNavigationService navigation = StrategyNavigationService.Active;
            if (navigation == null)
            {
                return false;
            }

            StrategyNavigationStatus status = navigation.TryBuildPath(
                new StrategyNavigationQuery(
                    start,
                    target,
                    StrategyNavigationMode.GroundCardinal,
                    Mathf.Max(256, map.Width * map.Height)),
                navigationRawCells,
                navigationSmoothedCells,
                false);
            if (status != StrategyNavigationStatus.Success)
            {
                return false;
            }

            cellPath = new List<Vector2Int>(navigationSmoothedCells.Count + 1) { start };
            cellPath.AddRange(navigationSmoothedCells);
            return cellPath.Count > 1;
        }

        private List<Vector3> ToWorldPath(List<Vector2Int> cellPath)
        {
            List<Vector3> worldPath = new();
            if (map == null || cellPath == null)
            {
                return worldPath;
            }

            for (int i = 0; i < cellPath.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cellPath[i].x, cellPath[i].y);
                center.z = -0.10f;
                worldPath.Add(center);
            }

            return worldPath;
        }
    }
}
