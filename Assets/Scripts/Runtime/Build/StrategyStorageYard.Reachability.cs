using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private static bool CanOwnerReachReservationBuilding(object owner, Component source)
        {
            return owner is not StrategyResidentAgent resident
                || source != null && resident.CanReachBuildingForReservation(source);
        }

        private static bool CanOwnerReachReservationNode(
            object owner,
            IStrategyProductionLogisticsNode source)
        {
            return owner is not StrategyResidentAgent resident
                || source is Component component
                    && resident.CanReachBuildingForReservation(component);
        }

        public static bool TryFindNearestReachableStorageYard(
            Vector3 nearWorld,
            StrategyResidentAgent resident,
            out StrategyStorageYard yard)
        {
            yard = null;
            float bestDistance = float.MaxValue;
            List<StrategyStorageYard> yards = GetActiveYards();
            for (int i = 0; i < yards.Count; i++)
            {
                StrategyStorageYard candidate = yards[i];
                if (candidate == null
                    || resident != null && !resident.CanReachBuildingForReservation(candidate))
                {
                    continue;
                }

                float distance = (candidate.FootprintBounds.center - nearWorld).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    yard = candidate;
                }
            }

            return yard != null;
        }
    }
}
