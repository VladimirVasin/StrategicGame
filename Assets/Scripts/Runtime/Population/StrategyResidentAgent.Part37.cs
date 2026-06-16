using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private bool TryMoveToRangedHuntingTarget(StrategyRabbitAgent rabbit)
        {
            if (rabbit == null || !rabbit.IsAlive || rabbit.IsCarcass || map == null)
            {
                return false;
            }

            activeHuntTarget = rabbit;
            if (CanShootCurrentHuntTarget("current_position"))
            {
                StartAimingBow();
                return true;
            }

            if (!rabbit.TryGetCurrentCell(out Vector2Int targetCell))
            {
                RejectHuntMove(Vector2Int.zero, "target_cell_missing", 0);
                return false;
            }

            if (!TryFindHuntingStandCell(rabbit, targetCell, out HuntingStandCandidate stand, out int checkedCandidates))
            {
                hunterWorkplace?.RegisterRejectedRabbitTarget(rabbit, targetCell, "no_valid_ranged_stand");
                RejectHuntMove(targetCell, "no_valid_ranged_stand", checkedCandidates);
                return false;
            }

            activity = ResidentActivity.MovingToHuntingRange;
            hasTarget = true;
            waitTimer = Random.Range(0.04f, 0.18f);
            StrategyDebugLogger.Info(
                "Hunting",
                "HuntMoveStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitCell", targetCell),
                StrategyDebugLogger.F("workCell", stand.Cell),
                StrategyDebugLogger.F("shotDistance", stand.Distance),
                StrategyDebugLogger.F("campOrigin", hunterWorkplace != null ? hunterWorkplace.Origin : Vector2Int.zero));
            return true;
        }

        private bool TryFindHuntingStandCell(
            StrategyRabbitAgent rabbit,
            Vector2Int targetCell,
            out HuntingStandCandidate stand,
            out int checkedCandidates)
        {
            stand = default;
            checkedCandidates = 0;
            List<HuntingStandCandidate> candidates = new();
            int maxRadius = Mathf.CeilToInt(HuntingShotRange) + 1;
            for (int radius = Mathf.FloorToInt(HuntingMinimumShotRange); radius <= maxRadius; radius++)
            {
                GatherHuntingStandCandidates(rabbit, targetCell, radius, candidates);
            }

            while (candidates.Count > 0 && checkedCandidates < MaxHuntingStandPathChecks)
            {
                int bestIndex = GetBestHuntingStandIndex(candidates);
                stand = candidates[bestIndex];
                candidates.RemoveAt(bestIndex);
                checkedCandidates++;
                if (TryBuildPathTo(stand.Cell))
                {
                    return true;
                }
            }

            return false;
        }

        private void GatherHuntingStandCandidates(
            StrategyRabbitAgent rabbit,
            Vector2Int targetCell,
            int radius,
            List<HuntingStandCandidate> candidates)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    Vector2Int cell = targetCell + new Vector2Int(x, y);
                    if (!map.IsCellWalkable(cell))
                    {
                        continue;
                    }

                    Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
                    float distance = Vector2.Distance(world, rabbit.transform.position);
                    if (!IsValidHuntingShotDistance(distance))
                    {
                        continue;
                    }

                    float travel = Vector2.Distance(transform.position, world);
                    float score = Mathf.Abs(distance - HuntingPreferredShotRange) + travel * 0.06f + Random.Range(0f, 0.05f);
                    candidates.Add(new HuntingStandCandidate(cell, distance, score));
                }
            }
        }

        private bool CanShootCurrentHuntTarget(string phase)
        {
            if (activeHuntTarget == null
                || !activeHuntTarget.IsAlive
                || activeHuntTarget.IsCarcass
                || map == null
                || !map.TryWorldToCell(transform.position, out Vector2Int currentCell)
                || !map.IsCellWalkable(currentCell))
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, activeHuntTarget.transform.position);
            bool valid = IsValidHuntingShotDistance(distance);
            if (!valid && phase != "current_position")
            {
                StrategyDebugLogger.Info(
                    "Hunting",
                    "BowRangeRejected",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("phase", phase),
                    StrategyDebugLogger.F("distance", distance),
                    StrategyDebugLogger.F("min", HuntingMinimumShotRange),
                    StrategyDebugLogger.F("max", HuntingShotRange));
            }

            return valid;
        }

        private void ReleaseHuntingArrow()
        {
            if (!CanShootCurrentHuntTarget("release"))
            {
                ResetHunterWorkToIdle(true);
                return;
            }

            float shotDistance = Vector2.Distance(transform.position, activeHuntTarget.transform.position);
            bool willHit = Random.value >= HuntingMissChance;
            Vector3 rabbitWorld = activeHuntTarget.transform.position;
            StrategyHuntingArrowProjectile.Launch(GetBowWorldPosition(), activeHuntTarget, this, willHit);
            StrategyDebugLogger.Info(
                "Hunting",
                "ArrowReleased",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitWorld", rabbitWorld),
                StrategyDebugLogger.F("shotDistance", shotDistance),
                StrategyDebugLogger.F("willHit", willHit),
                StrategyDebugLogger.F("missChance", HuntingMissChance));
        }

        private static bool IsValidHuntingShotDistance(float distance)
        {
            return distance >= HuntingMinimumShotRange && distance <= HuntingShotRange;
        }

        private static int GetBestHuntingStandIndex(List<HuntingStandCandidate> candidates)
        {
            int bestIndex = 0;
            float bestScore = candidates[0].Score;
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].Score < bestScore)
                {
                    bestScore = candidates[i].Score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void RejectHuntMove(Vector2Int targetCell, string reason, int checkedCandidates)
        {
            activeHuntTarget = null;
            activity = ResidentActivity.Idle;
            huntingWorkCooldown = Random.Range(2.0f, 4.0f);
            if (Time.time < nextHuntMoveRejectedLogTime)
            {
                return;
            }

            nextHuntMoveRejectedLogTime = Time.time + HuntMoveRejectedLogCooldownSeconds;
            StrategyDebugLogger.Warn(
                "Hunting",
                "HuntMoveRejected",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("rabbitCell", targetCell),
                StrategyDebugLogger.F("checkedStandCandidates", checkedCandidates),
                StrategyDebugLogger.F("maxStandPathChecks", MaxHuntingStandPathChecks),
                StrategyDebugLogger.F("reason", reason));
        }

        private readonly struct HuntingStandCandidate
        {
            public HuntingStandCandidate(Vector2Int cell, float distance, float score)
            {
                Cell = cell;
                Distance = distance;
                Score = score;
            }

            public Vector2Int Cell { get; }
            public float Distance { get; }
            public float Score { get; }
        }
    }
}
