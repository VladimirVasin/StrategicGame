using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFishAgent
    {

        private void UpdateFeeding()
        {
            stateTimer -= Time.deltaTime;
            AnimateFeed();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.9f));
            }
        }

        private void UpdateFleeing()
        {
            stateTimer -= Time.deltaTime;
            if (!hasTarget || pathIndex >= path.Count)
            {
                if (stateTimer > 0f && TryPickFleeTarget(lastThreatWorld))
                {
                    hasTarget = true;
                }
                else
                {
                    StartIdle(Random.Range(0.25f, 0.85f));
                    return;
                }
            }

            if (MoveAlongPath(FleeSpeed, true) && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.25f, 0.85f));
            }
        }

        private void UpdateTurning()
        {
            stateTimer -= Time.deltaTime;
            AnimateTurn();
            if (stateTimer <= 0f)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
                StartIdle(Random.Range(0.2f, 0.75f));
            }
        }

        private void UpdateHooked()
        {
            Vector3 previous = transform.position;
            float easedProgress = Mathf.SmoothStep(0f, 1f, reelProgress);
            Vector3 pullTarget = Vector3.Lerp(hookStartWorld, reelTargetWorld, easedProgress);
            float pullSpeed = Mathf.Lerp(SwimSpeed * 0.65f, FleeSpeed * 0.82f, easedProgress);
            transform.position = Vector3.MoveTowards(transform.position, pullTarget, pullSpeed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            float jitterScale = Mathf.Lerp(1f, 0.35f, easedProgress);
            float jitterX = Mathf.Sin((Time.time + bobPhase) * 18f) * 0.025f * jitterScale;
            float jitterY = Mathf.Cos((Time.time + bobPhase) * 15f) * 0.018f * jitterScale;
            transform.position = new Vector3(transform.position.x + jitterX * Time.deltaTime, transform.position.y + jitterY * Time.deltaTime, -0.068f);
            hookWorld = new Vector3(
                transform.position.x,
                transform.position.y + HookVisualLift + Mathf.Sin((Time.time + bobPhase) * 8.0f) * 0.025f,
                -0.11f);
            AnimateHooked();
        }

        private void PickRelaxedBehavior()
        {
            float roll = Random.value;
            if (roll < 0.24f)
            {
                StartFeeding();
                return;
            }

            if (roll < 0.90f && TryPickSwimTarget(false))
            {
                SetState(StrategyFishBehaviorState.Swimming, false, false);
                return;
            }

            if (roll < 0.96f)
            {
                StartTurning();
                return;
            }

            StartIdle(Random.Range(0.35f, 1.25f));
        }

        private void StartIdle(float duration)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = duration;
            SetState(StrategyFishBehaviorState.Idle, false, false);
        }

        private void StartFeeding()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.2f, 3.4f);
            SetState(StrategyFishBehaviorState.Feeding, false, false);
        }

        private void StartTurning()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(0.35f, 0.7f);
            SetState(StrategyFishBehaviorState.Turning, false, false);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.6f, 2.8f) : Random.Range(1.0f, 2.0f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyFishBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyFishBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyFishBehaviorState nextState, bool logImportant, bool noisyThreat)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            frame = 0;
            frameTimer = 0f;
            appliedFrame = -1;
            hasAppliedPose = false;
            transform.localRotation = Quaternion.identity;
            SetAnimatedScale(1f, 1f);

            if (logImportant)
            {
                string eventName = nextState == StrategyFishBehaviorState.Hooked
                    ? "FishHookedState"
                    : nextState == StrategyFishBehaviorState.Fleeing
                        ? "FishFleeing"
                        : "FishStateChanged";
                StrategyDebugLogger.Info(
                    "Wildlife",
                    eventName,
                    StrategyDebugLogger.F("species", species),
                    StrategyDebugLogger.F("shoal", shoalId),
                    StrategyDebugLogger.F("noisyThreat", noisyThreat),
                    StrategyDebugLogger.F("world", transform.position));
            }
        }

        private bool MoveAlongPath(float speed, bool fleeing)
        {
            if (pathIndex >= path.Count)
            {
                hasTarget = false;
                return true;
            }

            Vector3 targetWorld = path[pathIndex];
            if (Vector3.Distance(transform.position, targetWorld) <= TargetReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    hasTarget = false;
                    return true;
                }

                targetWorld = path[pathIndex];
            }

            Vector3 previous = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, speed * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                if (fleeing)
                {
                    AnimateFlee();
                }
                else
                {
                    AnimateSwim();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private void CompleteRiverRoute()
        {
            if (isCaught)
            {
                return;
            }

            isCaught = true;
            StrategyDebugLogger.Info(
                "Wildlife",
                "RiverFishDespawned",
                StrategyDebugLogger.F("species", species),
                StrategyDebugLogger.F("shoal", shoalId),
                StrategyDebugLogger.F("world", transform.position));
            Destroy(gameObject);
        }

        private bool TryPickSwimTarget(bool awayFromThreat)
        {
            if (!TryGetCurrentCell(out Vector2Int currentCell))
            {
                return false;
            }

            Vector2 away = Vector2.zero;
            if (awayFromThreat)
            {
                away = (Vector2)transform.position - (Vector2)lastThreatWorld;
                if (away.sqrMagnitude > 0.01f)
                {
                    away.Normalize();
                }
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 28; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));
                if (!IsRelaxedFishTarget(cell))
                {
                    continue;
                }

                Vector3 cellWorld = map.GetCellCenterWorld(cell.x, cell.y);
                float score = GetWaterPreference(cell) - Vector2Int.Distance(cell, homeCell) * 0.08f;
                if (awayFromThreat && away.sqrMagnitude > 0f)
                {
                    Vector2 direction = (Vector2)cellWorld - (Vector2)transform.position;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        score += Vector2.Dot(direction.normalized, away) * 3.0f;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                    found = true;
                }
            }

            if (!found || !TryBuildPathTo(bestCell))
            {
                return false;
            }

            hasTarget = path.Count > 0;
            return hasTarget;
        }

        private bool TryPickFleeTarget(Vector3 threatWorld)
        {
            if (!map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            Vector2 currentWorld = transform.position;
            Vector2 away = currentWorld - (Vector2)threatWorld;
            if (away.sqrMagnitude < 0.01f)
            {
                away = Random.insideUnitCircle;
                if (away.sqrMagnitude < 0.01f)
                {
                    away = Vector2.right;
                }
            }

            away.Normalize();

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 42; attempt++)
            {
                int distance = Random.Range(4, 11);
                Vector2 lateral = new Vector2(-away.y, away.x) * Random.Range(-3.0f, 3.0f);
                Vector2 candidateOffset = away * distance + lateral + Random.insideUnitCircle * 1.8f;
                Vector2Int candidate = currentCell + new Vector2Int(
                    Mathf.RoundToInt(candidateOffset.x),
                    Mathf.RoundToInt(candidateOffset.y));

                if (!IsFleeTarget(candidate))
                {
                    continue;
                }

                Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                float threatDistance = Vector2.Distance(candidateWorld, threatWorld);
                float homeDistance = Vector2Int.Distance(candidate, homeCell);
                float directionScore = Vector2.Dot(((Vector2)candidateWorld - currentWorld).normalized, away);
                float score = threatDistance * 1.35f + directionScore * 4.0f + GetWaterPreference(candidate) - homeDistance * 0.10f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }

            if (!found || !TryBuildPathTo(bestCell))
            {
                return false;
            }

            hasTarget = path.Count > 0;
            return hasTarget;
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (!map.TryWorldToCell(transform.position, out Vector2Int startCell)
                || !IsFishWaterCell(startCell)
                || !IsFishWaterCell(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.068f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 460)
            {
                Vector2Int current = open.Dequeue();
                if (current == targetCell)
                {
                    BuildWorldPath(startCell, targetCell, cameFrom);
                    return path.Count > 0;
                }

                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int next = current + CardinalDirections[i];
                    if (visited.Contains(next) || !IsFishWaterCell(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    cameFrom[next] = current;
                    open.Enqueue(next);
                }
            }

            return false;
        }

        private void BuildWorldPath(Vector2Int startCell, Vector2Int targetCell, Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new();
            Vector2Int current = targetCell;
            while (current != startCell)
            {
                cells.Add(current);
                if (!cameFrom.TryGetValue(current, out current))
                {
                    path.Clear();
                    pathIndex = 0;
                    return;
                }
            }

            cells.Reverse();
            path.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 center = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                if (i == cells.Count - 1)
                {
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.22f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.068f));
            }

            pathIndex = 0;
        }

        private bool TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat)
        {
            threatWorld = default;
            threatDistance = float.MaxValue;
            noisyThreat = false;

            IReadOnlyList<StrategyResidentAgent> residents = population != null ? population.Residents : null;
            if (residents == null || residents.Count <= 0)
            {
                return false;
            }

            float bestSqr = float.MaxValue;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null)
                {
                    continue;
                }

                bool residentIsNoisy = IsNoisyResidentActivity(resident.Activity);
                float radius = residentIsNoisy ? NoisyAlertRadius : AlertRadius;
                float sqr = (resident.transform.position - transform.position).sqrMagnitude;
                if (sqr > radius * radius || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                threatWorld = resident.transform.position;
                noisyThreat = residentIsNoisy;
            }

            if (bestSqr >= float.MaxValue)
            {
                return false;
            }

            threatDistance = Mathf.Sqrt(bestSqr);
            return true;
        }

        private static bool IsNoisyResidentActivity(StrategyResidentAgent.ResidentActivity activity)
        {
            return activity == StrategyResidentAgent.ResidentActivity.ChoppingTree
                || activity == StrategyResidentAgent.ResidentActivity.BuckingTree
                || activity == StrategyResidentAgent.ResidentActivity.MiningStone
                || activity == StrategyResidentAgent.ResidentActivity.BuildingConstruction
                || activity == StrategyResidentAgent.ResidentActivity.CastingFishingLine
                || activity == StrategyResidentAgent.ResidentActivity.ReelingFish
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }
    }
}
