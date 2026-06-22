using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private void EnsureStarterMineralDeposits()
        {
            if (!hasExclusion)
            {
                return;
            }

            EnsureStarterCoalDeposits();
            EnsureStarterIronDeposits();
        }

        private void EnsureStarterCoalDeposits()
        {
            if (coal == null)
            {
                return;
            }

            int created = 0;
            for (int i = CountCoalDepositsNear(excludedCenter, StarterMineralMaxDistance);
                i < StarterMineralMinimumDeposits;
                i++)
            {
                if (!TryFindStarterMineralCell(i, true, out CityMapCell cell)
                    || !TryCreateStarterCoalDeposit(cell, i))
                {
                    break;
                }

                created++;
            }

            LogStarterMineralReady("Coal", "StarterCoal", created, CountCoalDepositsNear(excludedCenter, StarterMineralMaxDistance));
        }

        private void EnsureStarterIronDeposits()
        {
            if (iron == null)
            {
                return;
            }

            int created = 0;
            for (int i = CountIronDepositsNear(excludedCenter, StarterMineralMaxDistance);
                i < StarterMineralMinimumDeposits;
                i++)
            {
                if (!TryFindStarterMineralCell(i, false, out CityMapCell cell)
                    || !TryCreateStarterIronDeposit(cell, i))
                {
                    break;
                }

                created++;
            }

            LogStarterMineralReady("Iron", "StarterIron", created, CountIronDepositsNear(excludedCenter, StarterMineralMaxDistance));
        }

        private bool TryFindStarterMineralCell(int placementIndex, bool coalDeposit, out CityMapCell cell)
        {
            cell = default;
            List<CityMapCell> candidates = new();
            int minDistance = Mathf.Max(StarterMineralMinDistance, excludedRadius + 1);
            int minDistanceSqr = minDistance * minDistance;
            int maxDistanceSqr = StarterMineralMaxDistance * StarterMineralMaxDistance;
            Vector2Int footprint = new(2, 2);

            for (int radius = minDistance; radius <= StarterMineralMaxDistance; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        int distanceSqr = x * x + y * y;
                        if (distanceSqr < minDistanceSqr || distanceSqr > maxDistanceSqr)
                        {
                            continue;
                        }

                        Vector2Int candidateCell = excludedCenter + new Vector2Int(x, y);
                        if (!map.TryGetCell(candidateCell.x, candidateCell.y, out CityMapCell candidate)
                            || IsInsideExclusion(candidateCell.x, candidateCell.y)
                            || !map.IsCellWalkable(candidateCell)
                            || !map.IsCellBuildable(candidateCell))
                        {
                            continue;
                        }

                        bool allowed = coalDeposit
                            ? IsCoalAllowedKind(candidate.Kind) && CanPlaceCoalFootprint(candidateCell, footprint)
                            : IsIronAllowedKind(candidate.Kind) && CanPlaceIronFootprint(candidateCell, footprint);
                        if (allowed)
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    int salt = coalDeposit ? 5201 : 5301;
                    int index = Hash(map.ActiveSeed, placementIndex, radius, salt, candidates.Count) % candidates.Count;
                    cell = candidates[index];
                    return true;
                }
            }

            return false;
        }

        private bool TryCreateStarterCoalDeposit(CityMapCell cell, int placementIndex)
        {
            return TryCreateCoalDeposit(
                cell,
                new Vector2Int(2, 2),
                StrategyNaturePropKind.CoalSeam,
                StrategyCoalDepositKind.CoalSeam,
                5211 + placementIndex * 7,
                0.92f,
                1.10f,
                42,
                68);
        }

        private bool TryCreateStarterIronDeposit(CityMapCell cell, int placementIndex)
        {
            return TryCreateIronDeposit(
                cell,
                new Vector2Int(2, 2),
                StrategyNaturePropKind.IronVein,
                StrategyIronDepositKind.IronVein,
                5311 + placementIndex * 7,
                0.92f,
                1.10f,
                46,
                76);
        }

        private void LogStarterMineralReady(string system, string eventPrefix, int created, int nearby)
        {
            if (nearby >= StarterMineralMinimumDeposits)
            {
                StrategyDebugLogger.Info(
                    system,
                    eventPrefix + "Ready",
                    StrategyDebugLogger.F("campCell", excludedCenter),
                    StrategyDebugLogger.F("created", created),
                    StrategyDebugLogger.F("nearby", nearby),
                    StrategyDebugLogger.F("minimum", StarterMineralMinimumDeposits),
                    StrategyDebugLogger.F("radius", StarterMineralMaxDistance));
                return;
            }

            StrategyDebugLogger.Warn(
                system,
                eventPrefix + "FallbackShort",
                StrategyDebugLogger.F("campCell", excludedCenter),
                StrategyDebugLogger.F("created", created),
                StrategyDebugLogger.F("nearby", nearby),
                StrategyDebugLogger.F("minimum", StarterMineralMinimumDeposits),
                StrategyDebugLogger.F("radius", StarterMineralMaxDistance));
        }

        private int CountIronDepositsNear(Vector2Int center, int radius)
        {
            return CountMineralDepositsNear(iron?.Deposits, center, radius);
        }

        private int CountCoalDepositsNear(Vector2Int center, int radius)
        {
            return CountMineralDepositsNear(coal?.Deposits, center, radius);
        }

        private int GetNearestIronDepositDistance(Vector2Int center)
        {
            return GetNearestMineralDepositDistance(iron?.Deposits, center);
        }

        private int GetNearestCoalDepositDistance(Vector2Int center)
        {
            return GetNearestMineralDepositDistance(coal?.Deposits, center);
        }

        private static int CountMineralDepositsNear<T>(IReadOnlyList<T> deposits, Vector2Int center, int radius)
            where T : Component
        {
            if (deposits == null)
            {
                return 0;
            }

            int count = 0;
            int radiusSqr = radius * radius;
            for (int i = 0; i < deposits.Count; i++)
            {
                if (TryGetMineralCell(deposits[i], out Vector2Int cell)
                    && (cell - center).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetNearestMineralDepositDistance<T>(IReadOnlyList<T> deposits, Vector2Int center)
            where T : Component
        {
            if (deposits == null || deposits.Count == 0)
            {
                return -1;
            }

            int bestSqr = int.MaxValue;
            for (int i = 0; i < deposits.Count; i++)
            {
                if (TryGetMineralCell(deposits[i], out Vector2Int cell))
                {
                    bestSqr = Mathf.Min(bestSqr, (cell - center).sqrMagnitude);
                }
            }

            return bestSqr == int.MaxValue ? -1 : Mathf.CeilToInt(Mathf.Sqrt(bestSqr));
        }

        private static bool TryGetMineralCell(Component deposit, out Vector2Int cell)
        {
            switch (deposit)
            {
                case StrategyIronDeposit ironDeposit when !ironDeposit.IsDepleted:
                    cell = ironDeposit.Cell;
                    return true;
                case StrategyCoalDeposit coalDeposit when !coalDeposit.IsDepleted:
                    cell = coalDeposit.Cell;
                    return true;
                default:
                    cell = default;
                    return false;
            }
        }
    }
}
