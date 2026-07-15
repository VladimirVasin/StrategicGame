using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPointOfInterestController
    {
        public void CapturePersistentState(List<StrategyPointOfInterestSaveData> target)
        {
            if (target == null)
            {
                return;
            }

            PruneMissingPoints();
            for (int i = 0; i < points.Count; i++)
            {
                StrategyPointOfInterest point = points[i];
                target.Add(new StrategyPointOfInterestSaveData
                {
                    stableId = point.StableId,
                    cellX = point.Cell.x,
                    cellY = point.Cell.y,
                    investigated = point.IsInvestigated
                });
            }
        }

        public void ClearForLoad()
        {
            CancelPendingNotices();
            ClearPointObjects();
        }

        public void RestorePersistentState(
            IReadOnlyList<StrategyPointOfInterestSaveData> savedPoints)
        {
            ClearForLoad();
            if (!configured || map == null)
            {
                return;
            }

            if (savedPoints == null || savedPoints.Count <= 0)
            {
                GenerateDefaultPoints();
                return;
            }

            HashSet<Vector2Int> usedCells = new();
            HashSet<string> usedIds = new(StringComparer.Ordinal);
            for (int i = 0; i < savedPoints.Count; i++)
            {
                StrategyPointOfInterestSaveData saved = savedPoints[i];
                if (saved == null)
                {
                    continue;
                }

                Vector2Int cell = new(saved.cellX, saved.cellY);
                string stableId = string.IsNullOrWhiteSpace(saved.stableId)
                    ? StrategyPointOfInterest.BuildStableId(cell)
                    : saved.stableId;
                if (!IsBaseLandCell(cell)
                    || !usedCells.Add(cell)
                    || !usedIds.Add(stableId))
                {
                    StrategyDebugLogger.Warn(
                        "PointOfInterest",
                        "RestoreEntrySkipped",
                        StrategyDebugLogger.F("index", i),
                        StrategyDebugLogger.F("id", stableId),
                        StrategyDebugLogger.F("cell", cell));
                    continue;
                }

                CreatePoint(stableId, cell, saved.investigated);
            }

            if (points.Count <= 0)
            {
                GenerateDefaultPoints();
            }

            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Restored",
                StrategyDebugLogger.F("saved", savedPoints.Count),
                StrategyDebugLogger.F("restored", points.Count),
                StrategyDebugLogger.F("investigated", CountInvestigated()));
        }

        public bool HasPointAt(Vector2Int cell)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsBaseLandCell(Vector2Int cell)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.IsBuildable
                && map.IsCellBuildable(cell)
                && map.IsCellWalkable(cell)
                && !HasForageAt(cell);
        }

        private static bool HasForageAt(Vector2Int cell)
        {
            StrategyForageResourceController forage = StrategyForageResourceController.Active;
            if (forage == null)
            {
                return false;
            }

            IReadOnlyList<StrategyForageNode> nodes = forage.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountInvestigated()
        {
            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].IsInvestigated)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
