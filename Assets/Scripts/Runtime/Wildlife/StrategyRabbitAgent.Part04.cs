using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {
        public bool ReactToHuntMiss(object owner, Vector3 threatWorld)
        {
            if (owner == null || huntReservationOwner != owner || !isAlive || isCarcass)
            {
                return false;
            }

            huntReservationOwner = null;
            hasTarget = false;
            hasThreat = true;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            StartFleeing(threatWorld, true);
            StrategyDebugLogger.Info(
                "Wildlife",
                "RabbitHuntMissFlee",
                StrategyDebugLogger.F("sex", sex),
                StrategyDebugLogger.F("group", groupId),
                StrategyDebugLogger.F("world", transform.position),
                StrategyDebugLogger.F("threatWorld", threatWorld));
            return true;
        }
    }
}
