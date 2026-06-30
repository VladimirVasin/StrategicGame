using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWildlifeController
    {
        public bool TryReserveWolfResidentTarget(
            StrategyWolfAgent wolf,
            Vector2Int center,
            out StrategyResidentAgent resident)
        {
            resident = null;
            if (wolf == null || population == null || map == null || IsWolfUnsafeSettlementCell(center))
            {
                return false;
            }

            RemoveMissingWolfResidentTargets();
            IReadOnlyList<StrategyResidentAgent> candidates = population.Residents;
            float radiusSqr = WolfResidentThreatRadius * WolfResidentThreatRadius;
            float bestScore = float.MaxValue;
            StrategyResidentAgent best = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                StrategyResidentAgent candidate = candidates[i];
                if (!CanWolfTargetResident(candidate)
                    || wolfResidentTargets.ContainsKey(candidate)
                    || !map.TryWorldToCell(candidate.transform.position, out Vector2Int cell)
                    || IsWolfUnsafeSettlementCell(cell))
                {
                    continue;
                }

                float sqr = (cell - center).sqrMagnitude;
                if (sqr > radiusSqr || sqr >= bestScore)
                {
                    continue;
                }

                bestScore = sqr;
                best = candidate;
            }

            if (best == null)
            {
                return false;
            }

            wolfResidentTargets[best] = wolf;
            resident = best;
            return true;
        }

        public void ReleaseWolfResidentTarget(StrategyWolfAgent wolf, StrategyResidentAgent resident)
        {
            if (wolf == null || resident == null)
            {
                return;
            }

            if (wolfResidentTargets.TryGetValue(resident, out StrategyWolfAgent owner) && owner == wolf)
            {
                wolfResidentTargets.Remove(resident);
            }
        }

        public bool TryFindWolfRoamCell(
            StrategyWolfAgent wolf,
            Vector2Int currentCell,
            bool preferSafety,
            out Vector2Int cell)
        {
            cell = default;
            if (wolf == null || map == null)
            {
                return false;
            }

            StrategyWolfPack pack = FindPackForWolf(wolf);
            Vector2Int center = pack != null ? pack.RoamCenterCell : currentCell;
            int radiusLimit = pack != null ? pack.HomeRadius : WolfHomeRadius;
            bool found = TryFindWolfRoamCellAround(wolf, currentCell, center, radiusLimit, preferSafety, preferSafety ? 5 : 2, out cell);
            bool usedCurrentFallback = false;
            if (!found && center != currentCell)
            {
                usedCurrentFallback = TryFindWolfRoamCellAround(wolf, currentCell, currentCell, radiusLimit, preferSafety, 1, out cell);
                found = usedCurrentFallback;
            }

            if (!found)
            {
                return false;
            }

            if (pack != null && (usedCurrentFallback || preferSafety || IsWolfUnsafeSettlementCell(center)))
            {
                ApplyWolfPackCenter(pack, cell);
            }
            else if (pack != null && Random.value < 0.22f)
            {
                pack.SetRoamCenter(cell);
            }

            return true;
        }

        public bool IsWolfUnsafeSettlementCell(Vector2Int cell)
        {
            return GetSettlementPressure(cell) >= WolfSettlementPressureLimit;
        }

        private void UpdateWildlifeMigration(float elapsedSeconds)
        {
            migrationTimer -= elapsedSeconds;
            if (migrationTimer <= 0f)
            {
                migrationTimer += MigrationUpdateInterval;
                pendingMigrationPasses = 1;
            }

            if (pendingMigrationPasses <= 0)
            {
                return;
            }

            UpdateNextMigrationCategory(MigrationUpdateInterval);
        }

        private void UpdateNextMigrationCategory(float elapsedSeconds)
        {
            switch (migrationSpeciesCursor)
            {
                case 0:
                    UpdateDeerMigration(elapsedSeconds);
                    break;
                case 1:
                    UpdateRabbitMigration(elapsedSeconds);
                    break;
                case 2:
                    UpdateWolfMigration(elapsedSeconds);
                    break;
                case 3:
                    UpdateBirdMigration(elapsedSeconds);
                    break;
                default:
                    UpdateFishMigration(elapsedSeconds);
                    break;
            }

            migrationSpeciesCursor++;
            if (migrationSpeciesCursor < 5)
            {
                return;
            }

            migrationSpeciesCursor = 0;
            pendingMigrationPasses = Mathf.Max(0, pendingMigrationPasses - 1);
        }

        private void UpdateDeerMigration(float elapsedSeconds)
        {
            RemoveMissingDeer();
            HashSet<int> processed = migrationProcessedIds;
            processed.Clear();
            for (int i = 0; i < deer.Count; i++)
            {
                StrategyDeerAgent representative = deer[i];
                if (representative == null || !representative.IsAlive || !processed.Add(representative.HerdId))
                {
                    continue;
                }

                MigrationState state = GetMigrationState(deerMigrations, representative.HerdId, DeerMigrationCooldownMin, DeerMigrationCooldownMax);
                if (TryAdvanceMigration(
                    elapsedSeconds,
                    state,
                    representative.HomeCell,
                    DeerMigrationStep,
                    DeerMigrationTargetMinDistance,
                    DeerMigrationCooldownMin,
                    DeerMigrationCooldownMax,
                    IsDeerMigrationCandidate,
                    GetDeerMigrationScore,
                    "DeerHerd",
                    representative.HerdId,
                    true,
                    out Vector2Int nextCenter))
                {
                    ApplyDeerHerdCenter(representative.HerdId, nextCenter);
                }
            }
        }

        private void UpdateRabbitMigration(float elapsedSeconds)
        {
            RemoveMissingRabbits();
            HashSet<int> processed = migrationProcessedIds;
            processed.Clear();
            for (int i = 0; i < rabbits.Count; i++)
            {
                StrategyRabbitAgent representative = rabbits[i];
                if (representative == null
                    || !representative.IsAlive
                    || representative.IsCarcass
                    || !processed.Add(representative.GroupId))
                {
                    continue;
                }

                MigrationState state = GetMigrationState(rabbitMigrations, representative.GroupId, RabbitMigrationCooldownMin, RabbitMigrationCooldownMax);
                if (TryAdvanceMigration(
                    elapsedSeconds,
                    state,
                    representative.HomeCell,
                    RabbitMigrationStep,
                    RabbitMigrationTargetMinDistance,
                    RabbitMigrationCooldownMin,
                    RabbitMigrationCooldownMax,
                    IsRabbitMigrationCandidate,
                    GetRabbitMigrationScore,
                    "RabbitGroup",
                    representative.GroupId,
                    true,
                    out Vector2Int nextCenter))
                {
                    ApplyRabbitGroupCenter(representative.GroupId, nextCenter);
                }
            }
        }

        private void UpdateWolfMigration(float elapsedSeconds)
        {
            RemoveMissingWolves();
            for (int i = 0; i < wolfPacks.Count; i++)
            {
                StrategyWolfPack pack = wolfPacks[i];
                if (pack == null || pack.MemberCount <= 0)
                {
                    continue;
                }

                MigrationState state = GetMigrationState(wolfMigrations, pack.PackId, WolfMigrationCooldownMin, WolfMigrationCooldownMax);
                if (TryAdvanceMigration(
                    elapsedSeconds,
                    state,
                    pack.RoamCenterCell,
                    WolfMigrationStep,
                    WolfMigrationTargetMinDistance,
                    WolfMigrationCooldownMin,
                    WolfMigrationCooldownMax,
                    IsWolfMigrationCandidate,
                    GetWolfMigrationScore,
                    "WolfPack",
                    pack.PackId,
                    true,
                    out Vector2Int nextCenter))
                {
                    ApplyWolfPackCenter(pack, nextCenter);
                }
            }
        }

        private void UpdateBirdMigration(float elapsedSeconds)
        {
            for (int i = birds.Count - 1; i >= 0; i--)
            {
                if (birds[i] == null)
                {
                    birds.RemoveAt(i);
                }
            }

            for (int i = 0; i < birds.Count; i++)
            {
                StrategyBirdAgent bird = birds[i];
                if (bird == null)
                {
                    continue;
                }

                MigrationState state = GetMigrationState(birdMigrations, bird.BirdId, BirdMigrationCooldownMin, BirdMigrationCooldownMax);
                if (TryAdvanceMigration(
                    elapsedSeconds,
                    state,
                    bird.HomeCell,
                    BirdMigrationStep,
                    BirdMigrationTargetMinDistance,
                    BirdMigrationCooldownMin,
                    BirdMigrationCooldownMax,
                    cell => IsBirdMigrationCandidate(bird.Species, cell),
                    cell => GetBirdMigrationScore(bird.Species, cell),
                    "BirdHome",
                    bird.BirdId,
                    false,
                    out Vector2Int nextCenter))
                {
                    bird.RetargetHomeCenter(nextCenter, BirdHomeRadius);
                }
            }
        }

        private void UpdateFishMigration(float elapsedSeconds)
        {
            RemoveMissingFish();
            HashSet<int> processed = migrationProcessedIds;
            processed.Clear();
            for (int i = 0; i < fish.Count; i++)
            {
                StrategyFishAgent representative = fish[i];
                if (representative == null
                    || !representative.IsLakeFish
                    || representative.IsCaught
                    || !processed.Add(representative.ShoalId))
                {
                    continue;
                }

                int regionId = representative.WaterRegionId;
                MigrationState state = GetMigrationState(fishMigrations, representative.ShoalId, FishMigrationCooldownMin, FishMigrationCooldownMax);
                if (TryAdvanceMigration(
                    elapsedSeconds,
                    state,
                    representative.HomeCell,
                    FishMigrationStep,
                    FishMigrationTargetMinDistance,
                    FishMigrationCooldownMin,
                    FishMigrationCooldownMax,
                    cell => IsLakeFishMigrationCandidate(cell, regionId),
                    GetFishMigrationScore,
                    "FishShoal",
                    representative.ShoalId,
                    false,
                    out Vector2Int nextCenter))
                {
                    ApplyFishShoalCenter(representative.ShoalId, nextCenter);
                }
            }
        }

        private MigrationState GetMigrationState(Dictionary<int, MigrationState> states, int id, float cooldownMin, float cooldownMax)
        {
            if (!states.TryGetValue(id, out MigrationState state) || state == null)
            {
                state = new MigrationState
                {
                    Cooldown = Random.Range(cooldownMin * 0.35f, cooldownMax * 0.75f)
                };
                states[id] = state;
            }

            return state;
        }

        private bool TryAdvanceMigration(
            float elapsedSeconds,
            MigrationState state,
            Vector2Int currentCenter,
            int step,
            int targetMinDistance,
            float cooldownMin,
            float cooldownMax,
            System.Func<Vector2Int, bool> isCandidate,
            System.Func<Vector2Int, float> scoreCandidate,
            string kind,
            int id,
            bool requireWalkableConnection,
            out Vector2Int nextCenter)
        {
            nextCenter = currentCenter;
            if (state == null || isCandidate == null || scoreCandidate == null)
            {
                return false;
            }

            if (!state.HasTarget)
            {
                state.Cooldown -= Mathf.Max(0.1f, elapsedSeconds);
                if (state.Cooldown > 0f)
                {
                    return false;
                }

                if (!TryPickMigrationTarget(currentCenter, step, targetMinDistance, isCandidate,
                    scoreCandidate, requireWalkableConnection, out state.Target))
                {
                    state.Cooldown = Random.Range(18f, 34f);
                    return false;
                }

                state.HasTarget = true;
                state.FailedSteps = 0;
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "MigrationStarted",
                    StrategyDebugLogger.F("kind", kind),
                    StrategyDebugLogger.F("id", id),
                    StrategyDebugLogger.F("from", currentCenter),
                    StrategyDebugLogger.F("target", state.Target));
            }

            if (Vector2Int.Distance(currentCenter, state.Target) <= Mathf.Max(1, step))
            {
                nextCenter = state.Target;
                state.HasTarget = false;
                state.Cooldown = Random.Range(cooldownMin, cooldownMax);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "MigrationCompleted",
                    StrategyDebugLogger.F("kind", kind),
                    StrategyDebugLogger.F("id", id),
                    StrategyDebugLogger.F("center", nextCenter),
                    StrategyDebugLogger.F("nextCooldown", state.Cooldown));
                return true;
            }

            if (TryPickMigrationStep(
                currentCenter,
                state.Target,
                step,
                isCandidate,
                scoreCandidate,
                requireWalkableConnection,
                out nextCenter))
            {
                state.FailedSteps = 0;
                return nextCenter != currentCenter;
            }

            state.FailedSteps++;
            if (state.FailedSteps >= 3)
            {
                if (ShouldLogMigrationAbort(kind, id))
                {
                    StrategyDebugLogger.Warn(
                        "Wildlife",
                        "MigrationAborted",
                        StrategyDebugLogger.F("kind", kind),
                        StrategyDebugLogger.F("id", id),
                        StrategyDebugLogger.F("center", currentCenter),
                        StrategyDebugLogger.F("target", state.Target));
                }

                RegisterMigrationTargetFailure(state.Target);
                state.HasTarget = false;
                state.Cooldown = Random.Range(12f, 28f);
                state.FailedSteps = 0;
            }

            return false;
        }

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
                Vector2Int candidate = new Vector2Int(Random.Range(0, map.Width), Random.Range(0, map.Height));
                float distance = Vector2Int.Distance(candidate, currentCenter);
                if (distance < minDistance || IsMigrationTargetCoolingDown(candidate) || !isCandidate(candidate))
                {
                    continue;
                }

                if (!IsViableMigrationTarget(
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
