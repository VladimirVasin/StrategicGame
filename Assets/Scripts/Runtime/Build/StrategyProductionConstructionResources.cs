using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyProductionConstructionResources
    {
        public static StrategyConstructionResourceCost GetTotalConstructionResources()
        {
            return new StrategyConstructionResourceCost(
                StrategyLumberjackCamp.GetTotalAvailableConstructionLogs(),
                StrategyStonecutterCamp.GetTotalAvailableConstructionStone(),
                StrategySawmill.GetTotalAvailableConstructionPlanks());
        }

        public static int ReserveConstructionResources(
            object owner,
            StrategyConstructionResourceKind kind,
            int requested,
            Vector3 nearWorld)
        {
            if (owner == null || requested <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            return kind switch
            {
                StrategyConstructionResourceKind.Logs => StrategyLumberjackCamp.ReserveConstructionLogs(owner, requested, nearWorld),
                StrategyConstructionResourceKind.Stone => StrategyStonecutterCamp.ReserveConstructionStone(owner, requested, nearWorld),
                StrategyConstructionResourceKind.Planks => StrategySawmill.ReserveConstructionPlanks(owner, requested, nearWorld),
                _ => 0
            };
        }

        public static int SpendAvailableResources(
            StrategyConstructionResourceKind kind,
            int requested,
            Vector3 nearWorld)
        {
            if (requested <= 0 || kind == StrategyConstructionResourceKind.None)
            {
                return 0;
            }

            return kind switch
            {
                StrategyConstructionResourceKind.Logs => StrategyLumberjackCamp.SpendAvailableConstructionLogs(requested, nearWorld),
                StrategyConstructionResourceKind.Stone => StrategyStonecutterCamp.SpendAvailableConstructionStone(requested, nearWorld),
                StrategyConstructionResourceKind.Planks => StrategySawmill.SpendAvailableConstructionPlanks(requested, nearWorld),
                _ => 0
            };
        }

        public static void ReleaseConstructionReservations(object owner)
        {
            if (owner == null)
            {
                return;
            }

            StrategyLumberjackCamp.ReleaseConstructionReservations(owner);
            StrategyStonecutterCamp.ReleaseConstructionReservations(owner);
            StrategySawmill.ReleaseConstructionReservations(owner);
        }

        public static bool TryFindConstructionPickup(
            object owner,
            StrategyConstructionResourceKind kind,
            Vector3 nearWorld,
            int maxAmount,
            out IStrategyConstructionResourceSource source,
            out Vector2Int pickupCell,
            out int amount)
        {
            source = null;
            pickupCell = default;
            amount = 0;
            if (owner == null || kind == StrategyConstructionResourceKind.None || maxAmount <= 0)
            {
                return false;
            }

            if (kind == StrategyConstructionResourceKind.Logs
                && StrategyLumberjackCamp.TryFindConstructionPickup(
                    owner,
                    nearWorld,
                    maxAmount,
                    out StrategyLumberjackCamp logCamp,
                    out pickupCell,
                    out amount))
            {
                source = logCamp;
                return true;
            }

            if (kind == StrategyConstructionResourceKind.Stone
                && StrategyStonecutterCamp.TryFindConstructionPickup(
                    owner,
                    nearWorld,
                    maxAmount,
                    out StrategyStonecutterCamp stoneCamp,
                    out pickupCell,
                    out amount))
            {
                source = stoneCamp;
                return true;
            }

            if (kind == StrategyConstructionResourceKind.Planks
                && StrategySawmill.TryFindConstructionPickup(
                    owner,
                    nearWorld,
                    maxAmount,
                    out StrategySawmill sawmill,
                    out pickupCell,
                    out amount))
            {
                source = sawmill;
                return true;
            }

            return false;
        }
    }
}
