using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private static readonly Vector2Int PointOfInterestMineralFootprint = new(2, 2);

        internal bool CanPlacePointOfInterestMineral(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin)
        {
            if (map == null)
            {
                return false;
            }

            return kind switch
            {
                StrategyPointOfInterestResourceKind.Coal =>
                    coal != null && CanPlaceCoalFootprint(origin, PointOfInterestMineralFootprint),
                StrategyPointOfInterestResourceKind.Iron =>
                    iron != null && CanPlaceIronFootprint(origin, PointOfInterestMineralFootprint),
                _ => false
            };
        }

        internal bool TryCreatePointOfInterestMineral(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin,
            int remainingAmount,
            int salt)
        {
            if (remainingAmount <= 0
                || !CanPlacePointOfInterestMineral(kind, origin)
                || !map.TryGetCell(origin.x, origin.y, out CityMapCell cell))
            {
                return false;
            }

            return kind switch
            {
                StrategyPointOfInterestResourceKind.Coal => TryCreateCoalDeposit(
                    cell,
                    PointOfInterestMineralFootprint,
                    StrategyNaturePropKind.CoalSeam,
                    StrategyCoalDepositKind.CoalSeam,
                    salt,
                    0.92f,
                    1.10f,
                    remainingAmount,
                    remainingAmount,
                    false),
                StrategyPointOfInterestResourceKind.Iron => TryCreateIronDeposit(
                    cell,
                    PointOfInterestMineralFootprint,
                    StrategyNaturePropKind.IronVein,
                    StrategyIronDepositKind.IronVein,
                    salt,
                    0.92f,
                    1.10f,
                    remainingAmount,
                    remainingAmount,
                    false),
                _ => false
            };
        }

        internal bool TryRemovePointOfInterestMineral(
            StrategyPointOfInterestResourceKind kind,
            Vector2Int origin)
        {
            if (kind == StrategyPointOfInterestResourceKind.Coal && coal != null)
            {
                IReadOnlyList<StrategyCoalDeposit> deposits = coal.Deposits;
                for (int i = deposits.Count - 1; i >= 0; i--)
                {
                    StrategyCoalDeposit deposit = deposits[i];
                    if (deposit == null
                        || deposit.Cell != origin
                        || deposit.Footprint != PointOfInterestMineralFootprint
                        || deposit.Kind != StrategyCoalDepositKind.CoalSeam)
                    {
                        continue;
                    }

                    deposit.RemoveFromWorld();
                    spawnedCoalDeposits = Mathf.Max(0, spawnedCoalDeposits - 1);
                    spawnedCoalSeams = Mathf.Max(0, spawnedCoalSeams - 1);
                    return true;
                }
            }
            else if (kind == StrategyPointOfInterestResourceKind.Iron && iron != null)
            {
                IReadOnlyList<StrategyIronDeposit> deposits = iron.Deposits;
                for (int i = deposits.Count - 1; i >= 0; i--)
                {
                    StrategyIronDeposit deposit = deposits[i];
                    if (deposit == null
                        || deposit.Cell != origin
                        || deposit.Footprint != PointOfInterestMineralFootprint
                        || deposit.Kind != StrategyIronDepositKind.IronVein)
                    {
                        continue;
                    }

                    deposit.RemoveFromWorld();
                    spawnedIronDeposits = Mathf.Max(0, spawnedIronDeposits - 1);
                    spawnedIronVeins = Mathf.Max(0, spawnedIronVeins - 1);
                    return true;
                }
            }

            return false;
        }
    }
}
