using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        private bool TryFindWolfRoamCellAround(
            StrategyWolfAgent wolf,
            Vector2Int currentCell,
            Vector2Int center,
            int radiusLimit,
            bool preferSafety,
            int startRadius,
            out Vector2Int cell)
        {
            cell = default;
            float bestScore = float.MinValue;
            bool found = false;
            int maxVisited = Mathf.Max(64, radiusLimit * radiusLimit + 48);
            for (int radius = Mathf.Max(1, startRadius); radius <= radiusLimit + 6; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = center + new Vector2Int(x, y);
                        if (candidate == currentCell
                            || wolf.IsWolfRoamTargetBlocked(candidate)
                            || !IsWolfRoamCandidate(candidate)
                            || !HasWalkableMigrationConnection(currentCell, candidate, maxVisited))
                        {
                            continue;
                        }

                        float settlementPressure = GetSettlementPressure(candidate);
                        if (preferSafety && settlementPressure >= WolfSettlementPressureLimit * 0.55f)
                        {
                            continue;
                        }

                        float score = GetWolfTerrainScore(candidate)
                            + CountWalkableNeighbors(candidate, 2) * 0.12f
                            - settlementPressure * 3.5f
                            - Vector2Int.Distance(candidate, currentCell) * (preferSafety ? -0.08f : 0.04f)
                            + Hash01(map.ActiveSeed, candidate.x, candidate.y, wolf.PackId + 211) * 0.25f;
                        if (score <= bestScore)
                        {
                            continue;
                        }

                        bestScore = score;
                        cell = candidate;
                        found = true;
                    }
                }

                if (found && !preferSafety)
                {
                    break;
                }
            }

            return found;
        }
    }
}
