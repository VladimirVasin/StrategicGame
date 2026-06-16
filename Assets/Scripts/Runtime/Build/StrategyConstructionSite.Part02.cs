using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyConstructionSite
    {
        public bool TryCollectBuildWorkCells(List<Vector2Int> candidates)
        {
            if (candidates == null)
            {
                return false;
            }

            candidates.Clear();
            AddBridgeBuildWorkCandidates(candidates);
            if (candidates.Count > 0)
            {
                return true;
            }

            AddCloseBuildWorkCandidates(candidates);
            AddBuildWorkRingCandidates(blockOrigin, blockFootprint, 2, candidates);
            return candidates.Count > 0;
        }

        public bool TryCollectDropoffCells(List<Vector2Int> candidates)
        {
            if (candidates == null)
            {
                return false;
            }

            candidates.Clear();
            AddBridgeBuildWorkCandidates(candidates);
            if (candidates.Count > 0)
            {
                return true;
            }

            AddBuildWorkRingCandidates(blockOrigin, blockFootprint, 4, candidates);
            return candidates.Count > 0;
        }

        private void CompleteConstruction()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            StrategyStorageYard.ReleaseConstructionReservations(this);
            StrategyDebugLogger.Info(
                "Construction",
                "Completed",
                StrategyDebugLogger.F("tool", tool),
                StrategyDebugLogger.F("origin", origin),
                StrategyDebugLogger.F("builders", builders.Count));

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.NotifyConstructionCompleted(this);
                }
            }

            placement?.CompleteConstructionSite(this);
        }

        private void AddBridgeBuildWorkCandidates(List<Vector2Int> candidates)
        {
            for (int i = bridgeWorkCells.Count - 1; i >= 0; i--)
            {
                Vector2Int candidate = bridgeWorkCells[i];
                if (map != null && map.IsCellWalkable(candidate))
                {
                    AddWalkableCandidate(candidate, candidates);
                    continue;
                }

                bridgeWorkCells.RemoveAt(i);
            }
        }

        private void AddCloseBuildWorkCandidates(List<Vector2Int> candidates)
        {
            for (int x = -1; x <= footprint.x; x++)
            {
                AddWalkableCandidate(origin + new Vector2Int(x, -1), candidates);
            }

            for (int y = 0; y < footprint.y; y++)
            {
                AddWalkableCandidate(origin + new Vector2Int(-1, y), candidates);
                AddWalkableCandidate(origin + new Vector2Int(footprint.x, y), candidates);
            }

            for (int x = 0; x < footprint.x; x++)
            {
                AddWalkableCandidate(origin + new Vector2Int(x, footprint.y), candidates);
            }
        }

        private void AddBuildWorkRingCandidates(
            Vector2Int targetOrigin,
            Vector2Int targetFootprint,
            int maxRadius,
            List<Vector2Int> candidates)
        {
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int y = -radius; y < targetFootprint.y + radius; y++)
                {
                    for (int x = -radius; x < targetFootprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == targetFootprint.x + radius - 1
                            || y == targetFootprint.y + radius - 1;
                        if (isEdge)
                        {
                            AddWalkableCandidate(targetOrigin + new Vector2Int(x, y), candidates);
                        }
                    }
                }
            }
        }
    }
}
