using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private bool TryAcquireTarget()
        {
            if (huntSearchTimer > 0f || wildlife == null || !TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            huntSearchTimer = Random.Range(HuntSearchIntervalMin, HuntSearchIntervalMax);
            if (wildlife.TryReserveWolfPrey(this, currentCell, out StrategyRabbitAgent rabbit, out StrategyDeerAgent deer))
            {
                targetRabbit = rabbit;
                targetDeer = deer;
                SetWolfState(StrategyWolfBehaviorState.Stalking, "prey_target_acquired");
                targetRefreshTimer = 0f;
                LogWolfTargetAcquired(targetRabbit != null ? "rabbit" : "deer", GetTargetDebugName());
                return true;
            }

            return false;
        }

        private bool TryStartRoaming(bool preferSafety)
        {
            if (wildlife == null || !TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            bool urgentEscape = preferSafety && IsWolfUrgentEscapeCell(currentCell);
            if (!urgentEscape && ShouldSkipWolfRoamPathAttempt(preferSafety))
            {
                return false;
            }

            if (urgentEscape && TryBuildNearbyEscapePath(out Vector2Int escapeCell))
            {
                LogWolfPathReady("nearby_escape", escapeCell, escapeCell);
                SetWolfState(StrategyWolfBehaviorState.AvoidingSettlement, "nearby_escape_path_ready");
                stateTimer = Random.Range(1.0f, 2.2f);
                return true;
            }

            return TryStartReachableRoaming(currentCell, preferSafety);
        }

        private void StartAttack()
        {
            SetWolfState(StrategyWolfBehaviorState.Attacking, "target_in_attack_range");
            frame = 0;
            appliedFrame = -1;
            frameTimer = 0f;
            attackResolved = false;
            path.Clear();
            pathIndex = 0;
        }

        private void StartFeeding()
        {
            SetWolfState(StrategyWolfBehaviorState.Feeding, "animal_attack_resolved");
            stateTimer = Random.Range(4.0f, 7.0f);
            feedingWorld = transform.position;
            frame = 0;
            appliedFrame = -1;
            frameTimer = 0f;
        }

        private void StartAvoidingSettlement()
        {
            if (state == StrategyWolfBehaviorState.Attacking || state == StrategyWolfBehaviorState.Feeding)
            {
                return;
            }

            ReleaseTargets();
            if (TryStartRoaming(true))
            {
                return;
            }

            SetWolfState(StrategyWolfBehaviorState.AvoidingSettlement, "avoid_no_path");
            ScheduleWolfEscapeRetry();
            stateTimer = Random.Range(0.9f, 1.8f);
            path.Clear();
            pathIndex = 0;
        }

        private void StartResting()
        {
            ReleaseTargets();
            SetWolfState(StrategyWolfBehaviorState.Resting, "rest_selected");
            stateTimer = Random.Range(2.8f, 6.5f);
            path.Clear();
            pathIndex = 0;
        }

        private void StartHowling()
        {
            ReleaseTargets();
            SetWolfState(StrategyWolfBehaviorState.Howling, "howl_selected");
            stateTimer = Random.Range(1.4f, 2.6f);
            path.Clear();
            pathIndex = 0;
        }

        private void StartIdle(float seconds)
        {
            ReleaseTargets();
            SetWolfState(StrategyWolfBehaviorState.Idle, "idle_selected");
            stateTimer = seconds;
            path.Clear();
            pathIndex = 0;
        }

        private void ResolveAttack()
        {
            attackResolved = true;
            if (targetRabbit != null)
            {
                feedingWorld = targetRabbit.transform.position;
                targetRabbit.KillByPredator(this, transform.position);
                return;
            }

            if (targetDeer != null)
            {
                feedingWorld = targetDeer.transform.position;
                targetDeer.KillByPredator(this, transform.position);
                return;
            }

            if (targetResident != null && population != null)
            {
                Vector3 attackWorld = transform.position;
                string residentName = targetResident.FullName;
                bool killed = population.TryKillResidentByWolf(targetResident, attackWorld);
                wildlife?.ReleaseWolfResidentTarget(this, targetResident);
                targetResident = null;
                LogWolfResidentAttackResult(killed, residentName, attackWorld);
            }
        }

        private void ConsumeAnimalTarget()
        {
            if (targetRabbit != null)
            {
                targetRabbit.ConsumePredatorKill(this);
            }

            if (targetDeer != null)
            {
                targetDeer.ConsumePredatorKill(this);
            }
        }

        private void ReleaseTargets()
        {
            LogWolfTargetReleased();
            if (targetRabbit != null)
            {
                targetRabbit.ReleasePredatorReservation(this);
                targetRabbit = null;
            }

            if (targetDeer != null)
            {
                targetDeer.ReleasePredatorReservation(this);
                targetDeer = null;
            }

            if (targetResident != null)
            {
                wildlife?.ReleaseWolfResidentTarget(this, targetResident);
                targetResident = null;
            }
        }

        private bool TryGetTargetWorld(out Vector3 world, out Vector2Int cell)
        {
            world = Vector3.zero;
            cell = default;
            if (targetRabbit != null && targetRabbit.IsAlive && targetRabbit.TryGetCurrentCell(out cell))
            {
                world = targetRabbit.transform.position;
                return true;
            }

            if (targetDeer != null && targetDeer.IsAlive && targetDeer.TryGetCurrentCell(out cell))
            {
                world = targetDeer.transform.position;
                return true;
            }

            if (targetResident != null
                && !targetResident.IsPendingRefugee
                && map != null
                && map.TryWorldToCell(targetResident.transform.position, out cell))
            {
                world = targetResident.transform.position;
                return true;
            }

            return false;
        }

        private bool ShouldAvoidSettlementNow()
        {
            return Time.realtimeSinceStartup >= NextWolfEscapeAttemptTime
                && state != StrategyWolfBehaviorState.AvoidingSettlement
                && state != StrategyWolfBehaviorState.Attacking
                && state != StrategyWolfBehaviorState.Feeding
                && TryGetCurrentCell(out Vector2Int currentCell)
                && wildlife != null
                && wildlife.IsWolfUnsafeSettlementCell(currentCell);
        }

        private bool TryPathNearTarget(Vector2Int targetCell)
        {
            if (ShouldSkipWolfTargetPathAttempt(targetCell))
            {
                return false;
            }

            if (TryBuildPathTo(targetCell))
            {
                MarkWolfTargetPathSuccess();
                LogWolfPathReady("target_direct", targetCell, targetCell);
                return true;
            }

            if (lastPathBuildDeferred)
            {
                return false;
            }

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int candidate = targetCell + CardinalDirections[i];
                if (IsWolfTargetCell(candidate) && TryBuildPathTo(candidate))
                {
                    MarkWolfTargetPathSuccess();
                    LogWolfPathReady("target_adjacent", targetCell, candidate);
                    return true;
                }

                if (lastPathBuildDeferred)
                {
                    return false;
                }
            }

            MarkWolfTargetPathFailure(targetCell);
            return LogWolfPathFailed("target_path_failed", targetCell);
        }

        private bool MoveAlongPath(float speed)
        {
            if (path.Count <= 0 || pathIndex >= path.Count)
            {
                return false;
            }

            Vector3 targetWorld = path[pathIndex];
            targetWorld.z = transform.position.z;
            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorld,
                StrategyWildlifeRiverCrossing.GetAdjustedSpeed(map, previous, targetWorld, speed) * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            TrackWolfMovementAttempt("path", previous, transform.position, targetWorld, speed);
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude <= MovingThresholdSqr)
            {
                AnimateIdle();
            }

            if (Vector2.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
            }

            return pathIndex >= path.Count;
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            lastPathBuildDeferred = false;
            if (map == null
                || !TryGetPathStartCell(out Vector2Int startCell)
                || !IsWolfTargetCell(targetCell))
            {
                return LogWolfPathFailed("path_prerequisite_failed", targetCell);
            }

            if (startCell == targetCell)
            {
                path.Clear();
                pathIndex = 0;
                return true;
            }

            StrategyNavigationService navigation = StrategyNavigationService.Active;
            if (navigation == null)
            {
                return LogWolfPathFailed("navigation_missing", targetCell);
            }

            bool allowStructureBuffer = wildlife != null && wildlife.IsLandWildlifeStructureBufferCell(startCell);
            StrategyNavigationStatus status = navigation.TryBuildPath(
                new StrategyNavigationQuery(
                    startCell,
                    targetCell,
                    StrategyNavigationMode.WildlifeLand,
                    Mathf.Max(256, map.Width * map.Height),
                    wildlife,
                    allowStructureBuffer),
                navigationRawCells,
                navigationSmoothedCells);
            if (status == StrategyNavigationStatus.Deferred)
            {
                lastPathBuildDeferred = true;
                return false;
            }

            if (status != StrategyNavigationStatus.Success)
            {
                return LogWolfPathFailed("path_unreachable", targetCell);
            }

            BuildWorldPath(navigationSmoothedCells);
            return path.Count > 0 || LogWolfPathFailed("path_rebuild_empty", targetCell);
        }

        private bool TryGetPathStartCell(out Vector2Int startCell)
        {
            startCell = default;
            if (map == null || !map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            if (IsWolfTravelCell(currentCell, true))
            {
                startCell = currentCell;
                return true;
            }

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int radius = 1; radius <= 4; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = currentCell + new Vector2Int(x, y);
                        if (!IsWolfTravelCell(candidate, true))
                        {
                            continue;
                        }

                        float distance = Vector2Int.Distance(currentCell, candidate);
                        if (distance < bestDistance)
                        {
                            startCell = candidate;
                            bestDistance = distance;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildWorldPath(IReadOnlyList<Vector2Int> cells)
        {
            path.Clear();
            if (cells == null)
            {
                pathIndex = 0;
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 world = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                path.Add(new Vector3(world.x, world.y, transform.position.z));
            }

            pathIndex = 0;
        }

        private bool TryGetCurrentCell(out Vector2Int cell)
        {
            cell = default;
            return this != null && map != null && map.TryWorldToCell(transform.position, out cell);
        }

        private Vector2Int GetCurrentCellOrHome()
        {
            return TryGetCurrentCell(out Vector2Int currentCell) ? currentCell : homeCell;
        }

        private void FaceWorldPoint(Vector3 world)
        {
            if (spriteRenderer != null && Mathf.Abs(world.x - transform.position.x) > 0.03f)
            {
                spriteRenderer.flipX = world.x < transform.position.x;
            }
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(6.0f, StrategyWolfSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyWolfSpritePose.Idle, frame);
        }

        private void AnimateWalk()
        {
            AdvanceLoopingFrame(10.0f, StrategyWolfSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyWolfSpritePose.Walk, frame);
        }

        private void AnimateRun()
        {
            AdvanceLoopingFrame(16.0f, StrategyWolfSpriteFactory.RunFrameCount);
            ApplySprite(StrategyWolfSpritePose.Run, frame);
        }

        private void AnimateStalk()
        {
            AdvanceLoopingFrame(8.5f, StrategyWolfSpriteFactory.StalkFrameCount);
            ApplySprite(StrategyWolfSpritePose.Stalk, frame);
        }

        private void AnimateAttack()
        {
            AdvanceClampedFrame(15.5f, StrategyWolfSpriteFactory.AttackFrameCount);
            ApplySprite(StrategyWolfSpritePose.Attack, frame);
        }

        private void AnimateEat()
        {
            transform.position = new Vector3(feedingWorld.x, feedingWorld.y, transform.position.z);
            AdvanceLoopingFrame(9.0f, StrategyWolfSpriteFactory.EatFrameCount);
            ApplySprite(StrategyWolfSpritePose.Eat, frame);
        }

        private void AnimateHowl()
        {
            AdvanceLoopingFrame(7.0f, StrategyWolfSpriteFactory.HowlFrameCount);
            ApplySprite(StrategyWolfSpritePose.Howl, frame);
        }

        private void AdvanceLoopingFrame(float framesPerSecond, int count)
        {
            frameTimer += Time.deltaTime * framesPerSecond;
            int steps = Mathf.FloorToInt(frameTimer);
            if (steps <= 0)
            {
                return;
            }

            frame = (frame + steps) % Mathf.Max(1, count);
            frameTimer -= steps;
        }
    }
}
