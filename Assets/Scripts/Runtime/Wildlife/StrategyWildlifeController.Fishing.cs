using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        public bool TryReserveFishForFishing(Vector2Int center, int radius, object owner, out StrategyFishAgent reservedFish)
        {
            return TryReserveFishForFishing(center, radius, owner, null, out reservedFish);
        }

        public bool TryReserveFishForFishing(
            Vector2Int center,
            int radius,
            object owner,
            System.Func<StrategyFishAgent, bool> isCandidateReachable,
            out StrategyFishAgent reservedFish)
        {
            reservedFish = null;
            if (owner == null || map == null)
            {
                return false;
            }

            RemoveMissingFish();
            float bestSqr = float.MaxValue;
            float radiusSqr = radius * radius;
            StrategyFishAgent best = null;
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent candidate = fish[i];
                if (candidate == null || !candidate.CanBeFished || !candidate.TryGetCurrentCell(out Vector2Int cell))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr > radiusSqr
                    || sqr >= bestSqr
                    || isCandidateReachable != null && !isCandidateReachable(candidate))
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null || !best.TryReserveForFishing(owner))
            {
                return false;
            }

            reservedFish = best;
            Vector2Int fishCell = reservedFish.TryGetCurrentCell(out Vector2Int reservedCell)
                ? reservedCell
                : Vector2Int.zero;
            StrategyDebugLogger.Info(
                "Wildlife",
                "FishReservedForFishing",
                StrategyDebugLogger.F("hutCenter", center),
                StrategyDebugLogger.F("radius", radius),
                StrategyDebugLogger.F("fishCell", fishCell),
                StrategyDebugLogger.F("fishWorld", reservedFish.transform.position));
            return true;
        }
    }
}
