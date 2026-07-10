using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryFindReachableRingWorkCell(Vector2Int origin, out Vector2Int cell, int maxPathChecks = int.MaxValue)
        {
            cell = default;
            if (map == null)
            {
                return false;
            }

            List<Vector2Int> candidates = new();
            int pathChecks = 0;
            for (int radius = 1; radius <= 2; radius++)
            {
                GatherRingWorkCells(origin, radius, candidates);
                while (candidates.Count > 0 && pathChecks < maxPathChecks)
                {
                    int index = GetNearestWorkCellIndex(candidates);
                    Vector2Int candidate = candidates[index];
                    candidates.RemoveAt(index);
                    pathChecks++;
                    if (TryBuildPathTo(candidate))
                    {
                        cell = candidate;
                        return true;
                    }
                }

                if (pathChecks >= maxPathChecks)
                {
                    break;
                }
            }

            path.Clear();
            pathIndex = 0;
            return false;
        }

        private void GatherRingWorkCells(Vector2Int origin, int radius, List<Vector2Int> candidates)
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

                    Vector2Int candidate = origin + new Vector2Int(x, y);
                    if (map.IsCellWalkable(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
            }
        }

        private int GetNearestWorkCellIndex(List<Vector2Int> candidates)
        {
            int bestIndex = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 world = map.GetCellCenterWorld(candidates[i].x, candidates[i].y);
                float score = (world - transform.position).sqrMagnitude + Random.Range(0f, 0.05f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private int GetConstructionWorkCellIndex(
            StrategyConstructionSite site,
            List<Vector2Int> candidates)
        {
            int bestIndex = 0;
            float bestScore = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector2Int candidate = candidates[i];
                Vector3 world = map.GetCellCenterWorld(candidate.x, candidate.y);
                int visualPriority = site != null ? site.GetWorkerVisualCellPriority(candidate) : 4;
                float score = visualPriority * 10000f
                    + (world - transform.position).sqrMagnitude
                    + Random.Range(0f, 0.05f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
