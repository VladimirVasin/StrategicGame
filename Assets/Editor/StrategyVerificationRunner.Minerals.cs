using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const int DecorativeNaturePropLimit = 3600;

        private static void VerifyGeneratedWorldResources(
            CityMapController map,
            StrategyPopulationController population)
        {
            Require(map != null && map.IsGenerated, "Generated map is missing during resource verification");
            Require(StrategyStoneResourceController.Active != null
                && StrategyStoneResourceController.Active.Deposits.Count >= 112,
                "Generated Stone minimum was not met");
            Require(StrategyClayResourceController.Active != null
                && StrategyClayResourceController.Active.Deposits.Count >= 28,
                "Generated Clay minimum was not met");

            StrategyIronResourceController iron = StrategyIronResourceController.Active;
            StrategyCoalResourceController coal = StrategyCoalResourceController.Active;
            StrategyPointOfInterestController points = StrategyPointOfInterestController.Active;
            Require(iron != null, "Iron resource controller is missing");
            Require(coal != null, "Coal resource controller is missing");
            Require(points != null, "Point-of-interest controller is missing");
            Require(population != null, "Population is missing for mineral verification");
            Require(population.TryGetCampCell(out Vector2Int campCell),
                "Camp cell is missing for mineral verification");

            int liveMineralCount = VerifyPointOfInterestMinerals(
                points.Points,
                iron.Deposits,
                coal.Deposits,
                campCell);
            GameObject natureRoot = GameObject.Find("Nature Props");
            Require(natureRoot != null
                && natureRoot.transform.childCount <= DecorativeNaturePropLimit + liveMineralCount,
                "Decorative nature prop limit was exceeded after allowing POI-owned minerals");
        }

        private static int VerifyPointOfInterestMinerals(
            IReadOnlyList<StrategyPointOfInterest> points,
            IReadOnlyList<StrategyIronDeposit> ironDeposits,
            IReadOnlyList<StrategyCoalDeposit> coalDeposits,
            Vector2Int campCell)
        {
            Require(points != null
                && points.Count == StrategyPointOfInterestPlacement.DefaultPointCount,
                "New world did not create the complete point-of-interest layout");
            Require(points[0] != null
                && points[0].ResourceKind == StrategyPointOfInterestResourceKind.None
                && !points[0].HasMineralSite,
                "The first point of interest must be mineral-free");

            int neutralCount = 0;
            int typedCount = 0;
            int coalCount = 0;
            int ironCount = 0;
            for (int i = 0; i < points.Count; i++)
            {
                StrategyPointOfInterest point = points[i];
                Require(point != null, "Point-of-interest registry contains a missing entry");
                if (point.ResourceKind == StrategyPointOfInterestResourceKind.None)
                {
                    neutralCount++;
                    Require(!point.HasMineralSite, "Neutral point owns a mineral site");
                    VerifyNoMineralsNearNeutral(point.Cell, ironDeposits, coalDeposits);
                    continue;
                }

                typedCount++;
                if (point.ResourceKind == StrategyPointOfInterestResourceKind.Coal)
                {
                    coalCount++;
                }
                else if (point.ResourceKind == StrategyPointOfInterestResourceKind.Iron)
                {
                    ironCount++;
                }

                Require(i <= 1 || point.ResourceKind != points[i - 1].ResourceKind,
                    "Resource points of interest do not alternate Coal and Iron");
                Require(point.HasMineralSite, "Typed point has no owned mineral site");
                int distance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                    point.Cell,
                    point.MineralOrigin,
                    StrategyPointOfInterestPlacement.MineralFootprint);
                Require(distance >= StrategyPointOfInterestPlacement.MineralPointMinDistance
                    && distance <= StrategyPointOfInterestPlacement.MineralPointMaxDistance,
                    "Typed point owns an out-of-zone mineral site");
                Require(StrategyPointOfInterestPlacement.DistanceToFootprint(
                        campCell,
                        point.MineralOrigin,
                        StrategyPointOfInterestPlacement.MineralFootprint)
                    > StrategyPointOfInterestPlacement.CampMineralExclusionRadius,
                    "POI-owned mineral site entered the camp exclusion radius");
                Require(CountLiveDepositsAt(
                        point.ResourceKind,
                        point.MineralOrigin,
                        ironDeposits,
                        coalDeposits) == 1,
                    "Typed point does not own exactly one matching live deposit");
            }

            Require(neutralCount == 1, "New world must contain exactly one neutral point of interest");
            Require(typedCount == points.Count - 1, "New world contains an untyped resource point");
            Require(coalCount + ironCount == typedCount && Mathf.Abs(coalCount - ironCount) == 1,
                "New world mineral points do not have the expected 5/4 Coal-Iron split");

            int liveCount = 0;
            for (int i = 0; i < ironDeposits.Count; i++)
            {
                StrategyIronDeposit deposit = ironDeposits[i];
                if (deposit == null || deposit.IsDepleted)
                {
                    continue;
                }

                liveCount++;
                VerifyLiveDepositOwnership(
                    StrategyPointOfInterestResourceKind.Iron,
                    deposit.Cell,
                    deposit.Footprint,
                    points,
                    campCell);
            }

            for (int i = 0; i < coalDeposits.Count; i++)
            {
                StrategyCoalDeposit deposit = coalDeposits[i];
                if (deposit == null || deposit.IsDepleted)
                {
                    continue;
                }

                liveCount++;
                VerifyLiveDepositOwnership(
                    StrategyPointOfInterestResourceKind.Coal,
                    deposit.Cell,
                    deposit.Footprint,
                    points,
                    campCell);
            }

            Require(liveCount == typedCount,
                "New world live mineral count does not match its typed points of interest");
            return liveCount;
        }

        private static void VerifyLiveDepositOwnership(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin,
            Vector2Int footprint,
            IReadOnlyList<StrategyPointOfInterest> points,
            Vector2Int campCell)
        {
            Require(footprint == StrategyPointOfInterestPlacement.MineralFootprint,
                "POI-owned mineral deposit has an unexpected footprint");
            Require(StrategyPointOfInterestPlacement.DistanceToFootprint(campCell, origin, footprint)
                > StrategyPointOfInterestPlacement.CampMineralExclusionRadius,
                "Live mineral deposit entered the camp exclusion radius");

            int exactOwners = 0;
            int zoneOwners = 0;
            for (int i = 0; i < points.Count; i++)
            {
                StrategyPointOfInterest point = points[i];
                if (point.HasMineralSite && point.MineralOrigin == origin)
                {
                    exactOwners++;
                    Require(point.ResourceKind == kind,
                        "Live mineral deposit is owned by the opposite POI type");
                }

                int distance = StrategyPointOfInterestPlacement.DistanceToFootprint(
                    point.Cell,
                    origin,
                    footprint);
                if (distance >= StrategyPointOfInterestPlacement.MineralPointMinDistance
                    && distance <= StrategyPointOfInterestPlacement.MineralPointMaxDistance)
                {
                    zoneOwners++;
                    Require(point.HasMineralSite
                        && point.ResourceKind == kind
                        && point.MineralOrigin == origin,
                        "Live mineral deposit appears in an unrelated POI zone");
                }
            }

            Require(exactOwners == 1, "Live mineral deposit does not have exactly one owner");
            Require(zoneOwners == 1, "Live mineral deposit does not belong to exactly one typed POI zone");
        }

        private static void VerifyNoMineralsNearNeutral(
            Vector2Int neutralCell,
            IReadOnlyList<StrategyIronDeposit> ironDeposits,
            IReadOnlyList<StrategyCoalDeposit> coalDeposits)
        {
            for (int i = 0; i < ironDeposits.Count; i++)
            {
                StrategyIronDeposit deposit = ironDeposits[i];
                if (deposit != null && !deposit.IsDepleted)
                {
                    Require(StrategyPointOfInterestPlacement.DistanceToFootprint(
                            neutralCell,
                            deposit.Cell,
                            deposit.Footprint)
                        > StrategyPointOfInterestPlacement.MineralFreeRadius,
                        "Iron generated near the neutral point of interest");
                }
            }

            for (int i = 0; i < coalDeposits.Count; i++)
            {
                StrategyCoalDeposit deposit = coalDeposits[i];
                if (deposit != null && !deposit.IsDepleted)
                {
                    Require(StrategyPointOfInterestPlacement.DistanceToFootprint(
                            neutralCell,
                            deposit.Cell,
                            deposit.Footprint)
                        > StrategyPointOfInterestPlacement.MineralFreeRadius,
                        "Coal generated near the neutral point of interest");
                }
            }
        }

        private static int CountLiveDepositsAt(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin,
            IReadOnlyList<StrategyIronDeposit> ironDeposits,
            IReadOnlyList<StrategyCoalDeposit> coalDeposits)
        {
            int count = 0;
            if (kind == StrategyPointOfInterestResourceKind.Iron)
            {
                for (int i = 0; i < ironDeposits.Count; i++)
                {
                    StrategyIronDeposit deposit = ironDeposits[i];
                    if (deposit != null && !deposit.IsDepleted && deposit.Cell == origin)
                    {
                        count++;
                    }
                }
            }
            else if (kind == StrategyPointOfInterestResourceKind.Coal)
            {
                for (int i = 0; i < coalDeposits.Count; i++)
                {
                    StrategyCoalDeposit deposit = coalDeposits[i];
                    if (deposit != null && !deposit.IsDepleted && deposit.Cell == origin)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
