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
                    resourceKind = (int)point.ResourceKind,
                    hasMineralSite = point.HasMineralSite,
                    mineralOriginX = point.MineralOrigin.x,
                    mineralOriginY = point.MineralOrigin.y,
                    remainingMineralAmount = GetRemainingMineralAmount(point),
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
            HashSet<Vector2Int> forageCells = CaptureForageCells();
            bool restoreFailed = false;
            for (int i = 0; i < savedPoints.Count; i++)
            {
                StrategyPointOfInterestSaveData saved = savedPoints[i];
                if (saved == null)
                {
                    restoreFailed = true;
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
                    restoreFailed = true;
                    StrategyDebugLogger.Warn(
                        "PointOfInterest",
                        "RestoreEntrySkipped",
                        StrategyDebugLogger.F("index", i),
                        StrategyDebugLogger.F("id", stableId),
                        StrategyDebugLogger.F("cell", cell));
                    continue;
                }

                if (saved.resourceKind < (int)StrategyPointOfInterestResourceKind.None
                    || saved.resourceKind > (int)StrategyPointOfInterestResourceKind.Iron)
                {
                    restoreFailed = true;
                    continue;
                }

                StrategyPointOfInterestResourceKind resourceKind =
                    (StrategyPointOfInterestResourceKind)saved.resourceKind;
                bool hasMineralSite = resourceKind != StrategyPointOfInterestResourceKind.None
                    && saved.hasMineralSite;
                Vector2Int mineralOrigin = new(saved.mineralOriginX, saved.mineralOriginY);
                if (resourceKind != StrategyPointOfInterestResourceKind.None
                    && (!hasMineralSite
                        || saved.remainingMineralAmount > 0
                        && (!CanRestoreMineralSite(
                                cell,
                                resourceKind,
                                mineralOrigin,
                                forageCells)
                            || !TryCreateMineralSite(
                                resourceKind,
                                mineralOrigin,
                                saved.remainingMineralAmount,
                                i))))
                {
                    restoreFailed = true;
                    StrategyDebugLogger.Warn(
                        "PointOfInterest",
                        "RestoreMineralSiteSkipped",
                        StrategyDebugLogger.F("index", i),
                        StrategyDebugLogger.F("id", stableId),
                        StrategyDebugLogger.F("resourceKind", resourceKind),
                        StrategyDebugLogger.F("origin", mineralOrigin),
                        StrategyDebugLogger.F("amount", saved.remainingMineralAmount));
                    continue;
                }

                if (!TryCreatePoint(
                        stableId,
                        cell,
                        resourceKind,
                        hasMineralSite,
                        mineralOrigin,
                        saved.investigated))
                {
                    restoreFailed = true;
                    if (hasMineralSite && saved.remainingMineralAmount > 0)
                    {
                        nature?.TryRemovePointOfInterestMineral(resourceKind, mineralOrigin);
                    }
                }
            }

            if (restoreFailed || points.Count != savedPoints.Count)
            {
                StrategyDebugLogger.Warn(
                    "PointOfInterest",
                    "RestoreRolledBack",
                    StrategyDebugLogger.F("saved", savedPoints.Count),
                    StrategyDebugLogger.F("restored", points.Count));
                GenerateDefaultPoints();
                return;
            }

            StrategyDebugLogger.Info(
                "PointOfInterest",
                "Restored",
                StrategyDebugLogger.F("saved", savedPoints.Count),
                StrategyDebugLogger.F("restored", points.Count),
                StrategyDebugLogger.F("coal", CountResourceKind(StrategyPointOfInterestResourceKind.Coal)),
                StrategyDebugLogger.F("iron", CountResourceKind(StrategyPointOfInterestResourceKind.Iron)),
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
