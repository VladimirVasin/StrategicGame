using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int ResidentArrowDamage = 40;
        private const float CombatPreferredShotRange = 3.0f;
        private const float CombatMaximumShotRange = 4.6f;
        private const int MaxCombatStandPathChecks = 18;

        private IStrategyCombatant activeCombatTarget;
        private float combatWorkTimer;
        private bool combatShotReleased;
        public bool IsCombatEngaged => activeCombatTarget != null && IsCombatActivity(activity);
        public int CombatAttackPoints => IsAdult ? ResidentArrowDamage : 0;

        public bool CanStartCombatEngagement
        {
            get
            {
                if (!CanWork
                    || deathRequested
                    || IsOnScoutExpedition
                    || IsFuneralDutyActive
                    || HasAnyCarriedResource())
                {
                    return false;
                }

                return activity is ResidentActivity.Idle
                    or ResidentActivity.TendingHousehold
                    or ResidentActivity.StayingInsideHome
                    or ResidentActivity.MovingHome
                    or ResidentActivity.MovingToCampfireSleep
                    or ResidentActivity.LightingCampfire
                    or ResidentActivity.SleepingByCampfire;
            }
        }

        public bool TryStartCombatEngagement(IStrategyCombatant target)
        {
            if (!CanStartCombatEngagement || !IsValidCombatTarget(target))
            {
                return false;
            }

            LeaveNightRestForCombatDuty();
            CancelNightLightTask("combat");
            activeCombatTarget = target;
            taskState.BeginPlannedTask(StrategyResidentTaskKind.Combat);
            combatWorkTimer = 0f;
            combatShotReleased = false;

            bool started = TryMoveToCombatRange();
            if (!started)
            {
                CancelCombatEngagement(false);
                return false;
            }

            StrategyDebugLogger.Info(
                "Combat",
                "ResidentCombatStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("health", CurrentCombatHealth),
                StrategyDebugLogger.F("target", target));
            return true;
        }

        public bool TryPrepareForCombatDuty()
        {
            if (!CanStartCombatEngagement)
            {
                return false;
            }

            LeaveNightRestForCombatDuty();
            CancelNightLightTask("combat");
            return CanStartCombatEngagement;
        }

        public void CancelCombatEngagement(bool log = true)
        {
            bool wasEngaged = activeCombatTarget != null || IsCombatActivity(activity);
            activeCombatTarget = null;
            combatWorkTimer = 0f;
            combatShotReleased = false;
            if (IsCombatActivity(activity))
            {
                activity = GetRestingActivity();
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.65f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }

            if (log && wasEngaged)
            {
                StrategyDebugLogger.Info(
                    "Combat",
                    "ResidentCombatEnded",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("health", CurrentCombatHealth));
            }
        }

        private bool TryMoveToCombatRange()
        {
            if (!IsValidCombatTarget(activeCombatTarget) || map == null)
            {
                return false;
            }

            if (CanShootCurrentCombatTarget())
            {
                StartAimingCombatBow();
                return true;
            }

            if (!activeCombatTarget.TryGetCombatCell(out Vector2Int targetCell)
                || !TryFindCombatStandCell(targetCell, out Vector2Int standCell))
            {
                return false;
            }

            activity = ResidentActivity.MovingToCombatRange;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            UseIdleSprite();
            return hasTarget;
        }

        private bool TryHandleMovingToCombatRangeBeforeStep()
        {
            if (activity != ResidentActivity.MovingToCombatRange)
            {
                return false;
            }

            if (!IsValidCombatTarget(activeCombatTarget))
            {
                CancelCombatEngagement();
                return true;
            }

            if (!CanShootCurrentCombatTarget())
            {
                return false;
            }

            StartAimingCombatBow();
            return true;
        }

        private bool TryFindCombatStandCell(Vector2Int targetCell, out Vector2Int standCell)
        {
            standCell = default;
            List<CombatStandCandidate> candidates = new();
            int maxRadius = Mathf.CeilToInt(CombatMaximumShotRange) + 1;
            for (int y = -maxRadius; y <= maxRadius; y++)
            {
                for (int x = -maxRadius; x <= maxRadius; x++)
                {
                    Vector2Int cell = targetCell + new Vector2Int(x, y);
                    if (!map.IsCellWalkable(cell))
                    {
                        continue;
                    }

                    Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
                    float shotDistance = Vector2.Distance(world, activeCombatTarget.CombatWorldPosition);
                    if (shotDistance > CombatMaximumShotRange)
                    {
                        continue;
                    }

                    float travel = Vector2.Distance(transform.position, world);
                    float score = Mathf.Abs(shotDistance - CombatPreferredShotRange) + travel * 0.06f;
                    candidates.Add(new CombatStandCandidate(cell, score));
                }
            }

            int checks = 0;
            while (candidates.Count > 0 && checks < MaxCombatStandPathChecks)
            {
                int best = GetBestCombatStandIndex(candidates);
                standCell = candidates[best].Cell;
                candidates.RemoveAt(best);
                checks++;
                if (TryBuildPathTo(standCell))
                {
                    return true;
                }
            }

            return false;
        }

        private void StartAimingCombatBow()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            if (!IsValidCombatTarget(activeCombatTarget))
            {
                CancelCombatEngagement();
                return;
            }

            if (!CanShootCurrentCombatTarget())
            {
                if (!TryMoveToCombatRange())
                {
                    CancelCombatEngagement();
                }

                return;
            }

            activity = ResidentActivity.AimingCombatBow;
            workFrame = 0;
            workFrameTimer = 0f;
            appliedWorkFrame = -1;
            usingWorkSprite = false;
            combatShotReleased = false;
            combatWorkTimer = 0.28f;
            FaceWorldPoint(activeCombatTarget.CombatWorldPosition);
        }

        private void UpdateAimingCombatBow()
        {
            if (!IsValidCombatTarget(activeCombatTarget))
            {
                CancelCombatEngagement();
                return;
            }

            if (!CanShootCurrentCombatTarget())
            {
                if (!TryMoveToCombatRange())
                {
                    CancelCombatEngagement();
                }

                return;
            }

            FaceWorldPoint(activeCombatTarget.CombatWorldPosition);
            AnimateCombatBowWork();
            if (!combatShotReleased)
            {
                return;
            }

            combatWorkTimer -= Time.deltaTime;
            if (combatWorkTimer <= 0f)
            {
                activity = ResidentActivity.WaitingForCombatHit;
                combatWorkTimer = 0.75f;
            }
        }

        private void UpdateWaitingForCombatHit()
        {
            if (!IsValidCombatTarget(activeCombatTarget))
            {
                CancelCombatEngagement();
                return;
            }

            if (!CanShootCurrentCombatTarget())
            {
                if (!TryMoveToCombatRange())
                {
                    CancelCombatEngagement();
                }

                return;
            }

            FaceWorldPoint(activeCombatTarget.CombatWorldPosition);
            ApplyBowFrame(9);
            combatWorkTimer -= Time.deltaTime;
            if (combatWorkTimer <= 0f)
            {
                StartAimingCombatBow();
            }
        }

        private void AnimateCombatBowWork()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            usingWalkSprite = false;
            if (!usingWorkSprite)
            {
                usingWorkSprite = true;
                workFrame = 0;
                workFrameTimer = 0f;
                appliedWorkFrame = -1;
            }

            workFrameTimer += Time.deltaTime * BowAnimationFrameRate;
            int frameSteps = Mathf.FloorToInt(workFrameTimer);
            for (int i = 0; i < frameSteps; i++)
            {
                workFrame = (workFrame + 1) % StrategyResidentSpriteFactory.BowFrameCount;
                if (workFrame == BowReleaseFrame && !combatShotReleased)
                {
                    combatShotReleased = true;
                    combatWorkTimer = 0.28f;
                    PlayBowShotSfx();
                    StrategyCombatArrowProjectile.Launch(
                        GetBowWorldPosition(),
                        activeCombatTarget,
                        this,
                        CombatAttackPoints);
                }
            }

            if (frameSteps > 0)
            {
                workFrameTimer -= frameSteps;
            }

            ApplyBowFrame(workFrame);
        }

        private bool CanShootCurrentCombatTarget()
        {
            return IsValidCombatTarget(activeCombatTarget)
                && Vector2.Distance(transform.position, activeCombatTarget.CombatWorldPosition)
                    <= CombatMaximumShotRange;
        }

        private void LeaveNightRestForCombatDuty()
        {
            if (sleepingInsideHome)
            {
                ReleaseNightSleep(false);
            }
            else if (returningHomeToSleep)
            {
                CancelNightSleepReturn();
            }

            if (sleepingAtHomelessCamp || returningToHomelessCamp || relightingCampfire)
            {
                ReleaseHomelessCampSleep(false);
            }
        }

        private static bool IsValidCombatTarget(IStrategyCombatant target)
        {
            if (target == null || target is Object unityTarget && unityTarget == null)
            {
                return false;
            }

            return target.IsCombatAlive
                && target.CanBeCombatTargeted
                && StrategyCombatRules.AreHostile(
                    StrategyCombatFaction.Settlement,
                    target.CombatFaction);
        }

        private static bool IsCombatActivity(ResidentActivity residentActivity)
        {
            return residentActivity is >= ResidentActivity.MovingToCombatRange
                and <= ResidentActivity.WaitingForCombatHit;
        }

        private static int GetBestCombatStandIndex(List<CombatStandCandidate> candidates)
        {
            int best = 0;
            for (int i = 1; i < candidates.Count; i++)
            {
                if (candidates[i].Score < candidates[best].Score)
                {
                    best = i;
                }
            }

            return best;
        }

        private readonly struct CombatStandCandidate
        {
            public CombatStandCandidate(Vector2Int cell, float score)
            {
                Cell = cell;
                Score = score;
            }

            public Vector2Int Cell { get; }
            public float Score { get; }
        }
    }
}
