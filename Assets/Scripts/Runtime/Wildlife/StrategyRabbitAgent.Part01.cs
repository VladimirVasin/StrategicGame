using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRabbitAgent
    {

        private void UpdateThreatAwareness()
        {
            threatCheckTimer -= Time.deltaTime;
            if (threatCheckTimer > 0f)
            {
                return;
            }

            threatCheckTimer = ThreatCheckInterval;
            if (!TryFindNearestThreat(out Vector3 threatWorld, out float threatDistance, out bool noisyThreat))
            {
                hasThreat = false;
                return;
            }

            hasThreat = true;
            lastThreatWorld = threatWorld;
            float fleeDistance = noisyThreat ? NoisyFleeRadius : FleeRadius;
            float alertDistance = noisyThreat ? NoisyAlertRadius : AlertRadius;

            if (threatDistance <= fleeDistance)
            {
                StartFleeing(threatWorld, noisyThreat);
                return;
            }

            if (threatDistance <= alertDistance && state != StrategyRabbitBehaviorState.Fleeing && !StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position))
            {
                StartAlert(threatWorld, noisyThreat);
            }
        }

        private void UpdateIdle()
        {
            waitTimer -= Time.deltaTime;
            AnimateIdle();
            if (waitTimer > 0f && !StrategyWildlifeRiverCrossing.IsRiverCell(map, transform.position))
            {
                return;
            }

            PickRelaxedBehavior();
        }

        private void UpdateHopping()
        {
            if (!hasTarget || pathIndex >= path.Count)
            {
                StartIdle(Random.Range(0.18f, 0.75f));
                return;
            }

            if (MoveAlongPath(HopSpeed, false))
            {
                StartIdle(Random.Range(0.15f, 0.65f));
            }
        }

        private void UpdateNibbling()
        {
            stateTimer -= Time.deltaTime;
            AnimateNibble();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.75f));
            }
        }

        private void UpdateAlert()
        {
            stateTimer -= Time.deltaTime;
            FaceWorldPoint(lastThreatWorld);
            AnimateAlert();
            if (!hasThreat && stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.8f));
                return;
            }

            if (stateTimer <= -0.8f)
            {
                PickRelaxedBehavior();
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
                    StartAlert(lastThreatWorld, false);
                    return;
                }
            }

            if (MoveAlongPath(FleeSpeed, true) && stateTimer <= 0f)
            {
                StartAlert(lastThreatWorld, false);
            }
        }

        private void UpdateGrooming()
        {
            stateTimer -= Time.deltaTime;
            AnimateGroom();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.7f));
            }
        }

        private void UpdateResting()
        {
            stateTimer -= Time.deltaTime;
            AnimateRest();
            if (stateTimer <= 0f)
            {
                StartIdle(Random.Range(0.2f, 0.85f));
            }
        }

        private void UpdateHunted()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer -= Time.deltaTime;
            AnimateAlert();
            if (stateTimer <= -1.0f)
            {
                stateTimer = Random.Range(0.2f, 0.65f);
            }
        }

        private void UpdateHuntDeath()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;

            if (state == StrategyRabbitBehaviorState.Hit)
            {
                AnimateHit();
                if (frame >= StrategyRabbitSpriteFactory.HitFrameCount - 1)
                {
                    SetState(StrategyRabbitBehaviorState.Dead, false, false);
                }

                return;
            }

            if (state == StrategyRabbitBehaviorState.Dead)
            {
                AnimateDeath();
                if (frame >= StrategyRabbitSpriteFactory.DeathFrameCount - 1)
                {
                    isCarcass = true;
                    SetState(StrategyRabbitBehaviorState.Carcass, false, false);
                    ApplySprite(StrategyRabbitSpritePose.Carcass, 0);
                    SetAnimatedScale(1f, 1f);
                }

                return;
            }

            isCarcass = true;
            ApplySprite(StrategyRabbitSpritePose.Carcass, 0);
            SetAnimatedScale(1f, 1f);
        }

        private void PickRelaxedBehavior()
        {
            float roll = Random.value;
            if (roll < 0.40f)
            {
                StartNibbling();
                return;
            }

            if (roll < 0.76f && TryPickHopTarget())
            {
                SetState(StrategyRabbitBehaviorState.Hopping, false, false);
                return;
            }

            if (roll < 0.90f)
            {
                StartGrooming();
                return;
            }

            if (roll < 0.97f)
            {
                StartResting();
                return;
            }

            StartIdle(Random.Range(0.25f, 1.0f));
        }

        private void StartIdle(float duration)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = duration;
            SetState(StrategyRabbitBehaviorState.Idle, false, false);
        }

        private void StartNibbling()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.7f, 4.2f);
            SetState(StrategyRabbitBehaviorState.Nibbling, false, false);
        }

        private void StartGrooming()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.0f, 2.4f);
            SetState(StrategyRabbitBehaviorState.Grooming, false, false);
        }

        private void StartResting()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            stateTimer = Random.Range(1.8f, 4.0f);
            SetState(StrategyRabbitBehaviorState.Resting, false, false);
        }

        private void StartAlert(Vector3 threatWorld, bool noisyThreat)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.0f, 2.3f) : Random.Range(0.6f, 1.5f);
            SetState(StrategyRabbitBehaviorState.Alert, true, noisyThreat);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.5f, 2.8f) : Random.Range(1.0f, 2.0f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyRabbitBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyRabbitBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyRabbitBehaviorState nextState, bool logImportant, bool noisyThreat)
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
                StrategyDebugLogger.Info(
                    "Wildlife",
                    nextState == StrategyRabbitBehaviorState.Fleeing ? "RabbitFleeing" : "RabbitAlert",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("group", groupId),
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
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorld,
                StrategyWildlifeRiverCrossing.GetAdjustedSpeed(map, previous, targetWorld, speed) * Time.deltaTime);
            Vector3 delta = transform.position - previous;
            if (Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (delta.sqrMagnitude > MovingThresholdSqr)
            {
                if (StrategyWildlifeRiverCrossing.IsSwimmingMove(map, previous, targetWorld))
                {
                    AnimateSwim();
                }
                else if (fleeing)
                {
                    AnimateFlee();
                }
                else
                {
                    AnimateHop();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private bool TryPickHopTarget()
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));

                if (!IsRelaxedRabbitTarget(cell))
                {
                    continue;
                }

                if (TryBuildPathTo(cell))
                {
                    hasTarget = path.Count > 0;
                    return hasTarget;
                }
            }

            return false;
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
            for (int attempt = 0; attempt < 38; attempt++)
            {
                int distance = Random.Range(4, 10);
                Vector2 lateral = new Vector2(-away.y, away.x) * Random.Range(-3.5f, 3.5f);
                Vector2 randomArc = Random.insideUnitCircle * 2.0f;
                Vector2 candidateOffset = away * distance + lateral + randomArc;
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
                float terrainScore = GetTerrainPreference(candidate);
                float score = threatDistance * 1.45f + directionScore * 4.5f + terrainScore - homeDistance * 0.12f;
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
                || !IsRabbitWalkCell(startCell, true)
                || !IsRabbitWalkCell(targetCell, landOnly: true))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.072f));
                pathIndex = 0;
                return true;
            }

            bool allowStructureBuffer = wildlife != null && wildlife.IsLandWildlifeStructureBufferCell(startCell);
            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 360)
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
                    if (visited.Contains(next) || !IsRabbitWalkCell(next, allowStructureBuffer))
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
    }
}
