using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private bool TryPickMigrationTarget(
            Vector2Int currentCenter,
            int step,
            int minDistance,
            System.Func<Vector2Int, bool> isCandidate,
            System.Func<Vector2Int, float> scoreCandidate,
            bool requireWalkableConnection,
            out Vector2Int target)
        {
            target = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 72; attempt++)
            {
                Vector2Int candidate = new(Random.Range(0, map.Width), Random.Range(0, map.Height));
                float distance = Vector2Int.Distance(candidate, currentCenter);
                if (distance < minDistance
                    || IsMigrationTargetCoolingDown(candidate)
                    || !isCandidate(candidate)
                    || !IsViableMigrationTarget(
                        currentCenter,
                        candidate,
                        step,
                        requireWalkableConnection,
                        isCandidate,
                        scoreCandidate))
                {
                    continue;
                }

                float score = scoreCandidate(candidate)
                    + Mathf.Clamp(distance, 0f, 64f) * 0.05f
                    + Random.value * 0.35f;
                if (score > bestScore)
                {
                    bestScore = score;
                    target = candidate;
                    found = true;
                }
            }

            return found;
        }
    }
}
