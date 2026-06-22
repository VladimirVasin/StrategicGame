using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        public bool TryReserveHuntTarget(
            Vector2Int center,
            int radius,
            bool allowDeer,
            object owner,
            out IStrategyHuntTarget target,
            System.Predicate<IStrategyHuntTarget> candidateFilter = null)
        {
            target = null;
            if (owner == null || map == null)
            {
                return false;
            }

            RemoveMissingRabbits();
            RemoveMissingDeer();
            float bestSqr = float.MaxValue;
            StrategyRabbitAgent bestRabbit = null;
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent candidate = rabbits[i];
                if (candidate == null
                    || !candidate.CanBeHunted
                    || !candidate.TryGetCurrentCell(out Vector2Int cell)
                    || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell)
                    || (candidateFilter != null && !candidateFilter(candidate)))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    bestRabbit = candidate;
                }
            }

            StrategyDeerAgent bestDeer = null;
            if (allowDeer)
            {
                for (int i = 0; i < deer.Count; i++)
                {
                    StrategyDeerAgent candidate = deer[i];
                    if (candidate == null
                        || !candidate.CanBeHunted
                        || !candidate.TryGetCurrentCell(out Vector2Int cell)
                        || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell)
                        || (candidateFilter != null && !candidateFilter(candidate)))
                    {
                        continue;
                    }

                    float sqr = (cell - center).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        bestDeer = candidate;
                        bestRabbit = null;
                    }
                }
            }

            if (bestRabbit != null && bestRabbit.TryReserveForHunt(owner))
            {
                target = bestRabbit;
            }
            else if (bestDeer != null && bestDeer.TryReserveForHunt(owner))
            {
                target = bestDeer;
            }

            if (target == null)
            {
                return false;
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "TargetReservedForHunt",
                StrategyDebugLogger.F("kind", target.HuntTargetKind),
                StrategyDebugLogger.F("campCenter", center),
                StrategyDebugLogger.F("radius", radius),
                StrategyDebugLogger.F("targetWorld", target.HuntWorldPosition));
            return true;
        }

        public int CountHuntableDeer(Vector2Int center, int radius)
        {
            if (map == null)
            {
                return 0;
            }

            RemoveMissingDeer();
            int count = 0;
            float radiusSqr = radius * radius;
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent agent = deer[i];
                if (agent == null
                    || !agent.CanBeHunted
                    || !agent.TryGetCurrentCell(out Vector2Int cell)
                    || !StrategyWildlifeRiverCrossing.IsLandCell(map, cell))
                {
                    continue;
                }

                if ((cell - center).sqrMagnitude <= radiusSqr)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
