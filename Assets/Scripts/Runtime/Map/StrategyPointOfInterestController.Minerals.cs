using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPointOfInterestController
    {
        private bool CanUsePlannedCell(Vector2Int cell, HashSet<Vector2Int> forageCells)
        {
            return map != null
                && map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell)
                && mapCell.IsBuildable
                && map.IsCellWalkable(cell)
                && map.IsCellBuildable(cell)
                && (forageCells == null || !forageCells.Contains(cell))
                && StrategyTrailController.Active?.HasRouteRoadAt(cell) != true;
        }

        private bool CanRestoreMineralSite(
            Vector2Int pointCell,
            StrategyPointOfInterestResourceKind kind,
            Vector2Int mineralOrigin,
            HashSet<Vector2Int> forageCells)
        {
            if (kind == StrategyPointOfInterestResourceKind.None
                || nature == null
                || StrategyPointOfInterestPlacement.DistanceToFootprint(
                    pointCell,
                    mineralOrigin,
                    StrategyPointOfInterestPlacement.MineralFootprint)
                    < StrategyPointOfInterestPlacement.MineralPointMinDistance
                || StrategyPointOfInterestPlacement.DistanceToFootprint(
                    pointCell,
                    mineralOrigin,
                    StrategyPointOfInterestPlacement.MineralFootprint)
                    > StrategyPointOfInterestPlacement.MineralPointMaxDistance
                || StrategyPointOfInterestPlacement.DistanceToFootprint(
                    campCell,
                    mineralOrigin,
                    StrategyPointOfInterestPlacement.MineralFootprint)
                    <= StrategyPointOfInterestPlacement.CampMineralExclusionRadius)
            {
                return false;
            }

            for (int y = 0; y < StrategyPointOfInterestPlacement.MineralFootprint.y; y++)
            {
                for (int x = 0; x < StrategyPointOfInterestPlacement.MineralFootprint.x; x++)
                {
                    Vector2Int cell = mineralOrigin + new Vector2Int(x, y);
                    if (!CanUsePlannedCell(cell, forageCells))
                    {
                        return false;
                    }
                }
            }

            return nature.CanPlacePointOfInterestMineral(kind, mineralOrigin);
        }

        private bool TryCreateMineralSite(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin,
            int remainingAmount,
            int salt)
        {
            return remainingAmount <= 0
                || nature != null
                && nature.TryCreatePointOfInterestMineral(kind, origin, remainingAmount, salt);
        }

        private static int GetInitialMineralAmount(StrategyPointOfInterestPlan plan, int index)
        {
            int minimum = plan.ResourceKind == StrategyPointOfInterestResourceKind.Coal ? 52 : 56;
            int maximum = plan.ResourceKind == StrategyPointOfInterestResourceKind.Coal ? 72 : 80;
            int range = maximum - minimum + 1;
            return minimum + StableMineralHash(
                plan.MineralOrigin.x,
                plan.MineralOrigin.y,
                index,
                (int)plan.ResourceKind) % range;
        }

        private static int GetRemainingMineralAmount(StrategyPointOfInterest point)
        {
            if (point == null || !point.HasMineralSite)
            {
                return 0;
            }

            if (point.ResourceKind == StrategyPointOfInterestResourceKind.Coal)
            {
                IReadOnlyList<StrategyCoalDeposit> deposits = StrategyCoalResourceController.Active?.Deposits;
                if (deposits != null)
                {
                    for (int i = 0; i < deposits.Count; i++)
                    {
                        StrategyCoalDeposit deposit = deposits[i];
                        if (deposit != null && deposit.Cell == point.MineralOrigin)
                        {
                            return Mathf.Max(0, deposit.CoalAmount);
                        }
                    }
                }
            }
            else if (point.ResourceKind == StrategyPointOfInterestResourceKind.Iron)
            {
                IReadOnlyList<StrategyIronDeposit> deposits = StrategyIronResourceController.Active?.Deposits;
                if (deposits != null)
                {
                    for (int i = 0; i < deposits.Count; i++)
                    {
                        StrategyIronDeposit deposit = deposits[i];
                        if (deposit != null && deposit.Cell == point.MineralOrigin)
                        {
                            return Mathf.Max(0, deposit.IronAmount);
                        }
                    }
                }
            }

            return 0;
        }

        private static int StableMineralHash(int x, int y, int index, int kind)
        {
            unchecked
            {
                int hash = 146959810;
                hash = hash * 16777619 ^ x;
                hash = hash * 16777619 ^ y;
                hash = hash * 16777619 ^ index;
                hash = hash * 16777619 ^ kind;
                return hash & int.MaxValue;
            }
        }

        internal static string GetInvestigationTitle(StrategyPointOfInterestResourceKind kind)
        {
            return kind switch
            {
                StrategyPointOfInterestResourceKind.Coal => "Coal Deposits Found",
                StrategyPointOfInterestResourceKind.Iron => "Iron Deposits Found",
                _ => "Point Investigated"
            };
        }

        internal static string GetInvestigationResult(StrategyPointOfInterestResourceKind kind)
        {
            return kind switch
            {
                StrategyPointOfInterestResourceKind.Coal =>
                    "Coal deposits were found near this point.\n\nA Coal Pit can be built over the deposit.",
                StrategyPointOfInterestResourceKind.Iron =>
                    "Iron deposits were found near this point.\n\nA Mine can be built over the deposit.",
                _ => "No useful mineral deposits were found near this point."
            };
        }

        private static string GetInvestigationBody(
            StrategyPointOfInterest point,
            StrategyResidentAgent resident)
        {
            string report = resident.FullName
                + " investigated a landmark at "
                + FormatCell(point.Cell)
                + ".\n\n";
            return report + GetInvestigationResult(point.ResourceKind);
        }

        private int CountResourceKind(StrategyPointOfInterestResourceKind kind)
        {
            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].ResourceKind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasExpectedMineralDistribution(
            IReadOnlyList<StrategyPointOfInterestPlan> plans)
        {
            int targetCount = StrategyPointOfInterestPlacement.DefaultResourcePointCount;
            if (plans == null
                || plans.Count != targetCount)
            {
                return false;
            }

            for (int i = 0; i < plans.Count; i++)
            {
                if (!plans[i].HasMineralSite
                    || i > 0 && plans[i].ResourceKind == plans[i - 1].ResourceKind)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CountPlannedKind(
            IReadOnlyList<StrategyPointOfInterestPlan> plans,
            StrategyPointOfInterestResourceKind kind)
        {
            int count = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].ResourceKind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
