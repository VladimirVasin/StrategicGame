using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyDeerAgent
    {

        private void StartAlert(Vector3 threatWorld, bool noisyThreat)
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(1.5f, 3.2f) : Random.Range(0.9f, 2.2f);
            SetState(StrategyDeerBehaviorState.Alert, true, noisyThreat);
        }

        private void StartFleeing(Vector3 threatWorld, bool noisyThreat)
        {
            lastThreatWorld = threatWorld;
            stateTimer = noisyThreat ? Random.Range(2.1f, 3.8f) : Random.Range(1.5f, 2.8f);
            bool foundTarget = TryPickFleeTarget(threatWorld);
            if (!foundTarget && state == StrategyDeerBehaviorState.Fleeing)
            {
                return;
            }

            SetState(StrategyDeerBehaviorState.Fleeing, true, noisyThreat);
        }

        private void SetState(StrategyDeerBehaviorState nextState, bool logImportant, bool noisyThreat)
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
                    nextState == StrategyDeerBehaviorState.Fleeing ? "DeerFleeing" : "DeerAlert",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("herd", herdId),
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
                    AnimateWalk();
                }
            }
            else
            {
                AnimateIdle();
            }

            return false;
        }

        private bool TryPickWalkTarget()
        {
            for (int attempt = 0; attempt < 22; attempt++)
            {
                Vector2Int cell = homeCell + new Vector2Int(
                    Random.Range(-homeRadius, homeRadius + 1),
                    Random.Range(-homeRadius, homeRadius + 1));

                if (!IsRelaxedDeerTarget(cell))
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
                away = Random.insideUnitCircle.normalized;
            }
            else
            {
                away.Normalize();
            }

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;
            for (int attempt = 0; attempt < 42; attempt++)
            {
                int distance = Random.Range(5, 12);
                Vector2 randomArc = Random.insideUnitCircle * 3.2f;
                Vector2 candidateOffset = away * distance + randomArc;
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
                float score = threatDistance * 1.35f + directionScore * 5.0f + terrainScore - homeDistance * 0.16f;
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
                || !IsDeerWalkCell(startCell)
                || !IsDeerWalkCell(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.075f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            while (open.Count > 0 && visited.Count < 640)
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
                    if (visited.Contains(next) || !IsDeerWalkCell(next))
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
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.24f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.075f));
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
                || activity == StrategyResidentAgent.ResidentActivity.PlantingTree
                || activity == StrategyResidentAgent.ResidentActivity.DepositingLogs
                || activity == StrategyResidentAgent.ResidentActivity.DepositingStone
                || activity == StrategyResidentAgent.ResidentActivity.DepositingConstructionResource;
        }

        private bool IsRelaxedDeerTarget(Vector2Int cell)
        {
            return IsDeerWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius
                && GetTerrainPreference(cell) >= 0f;
        }

        private bool IsFleeTarget(Vector2Int cell)
        {
            return IsDeerWalkCell(cell)
                && Vector2Int.Distance(cell, homeCell) <= homeRadius + 14
                && GetTerrainPreference(cell) > -2f;
        }

        private bool IsDeerWalkCell(Vector2Int cell)
        {
            return map != null && map.IsCellWalkable(cell);
        }

        private float GetTerrainPreference(Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return -10f;
            }

            return mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 4f,
                CityMapCellKind.Grass => 2.5f,
                CityMapCellKind.Forest => 1.35f,
                CityMapCellKind.Dirt => 0.15f,
                CityMapCellKind.Shore => -0.5f,
                _ => -10f
            };
        }

        private void AnimateIdle()
        {
            AdvanceLoopingFrame(IdleAnimationRate, StrategyDeerSpriteFactory.IdleFrameCount);
            ApplySprite(StrategyDeerSpritePose.Idle, frame);
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 4.5f) * 0.025f;
            SetAnimatedScale(1f, pulse);
        }

        private void AnimateWalk()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(WalkAnimationRate, StrategyDeerSpriteFactory.WalkFrameCount);
            ApplySprite(StrategyDeerSpritePose.Walk, frame);
        }

        private void AnimateGraze()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(GrazeAnimationRate, StrategyDeerSpriteFactory.GrazeFrameCount);
            ApplySprite(StrategyDeerSpritePose.Graze, frame);
        }

        private void AnimateAlert()
        {
            float pulse = 1f + Mathf.Sin((Time.time + bobPhase) * 8.5f) * 0.018f;
            SetAnimatedScale(1f + (pulse - 1f) * 0.4f, pulse);
            AdvanceLoopingFrame(AlertAnimationRate, StrategyDeerSpriteFactory.AlertFrameCount);
            ApplySprite(StrategyDeerSpritePose.Alert, frame);
        }

        private void AnimateFlee()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(FleeAnimationRate, StrategyDeerSpriteFactory.RunFrameCount);
            ApplySprite(StrategyDeerSpritePose.Run, frame);
        }

        private void AnimateRest()
        {
            SetAnimatedScale(1f, 1f);
            AdvanceLoopingFrame(RestAnimationRate, StrategyDeerSpriteFactory.RestFrameCount);
            ApplySprite(StrategyDeerSpritePose.Rest, frame);
        }

        private void AdvanceLoopingFrame(float frameRate, int frameCount)
        {
            frameTimer += Time.deltaTime * frameRate;
            int frameSteps = Mathf.FloorToInt(frameTimer);
            if (frameSteps <= 0)
            {
                return;
            }

            frame = (frame + frameSteps) % frameCount;
            frameTimer -= frameSteps;
        }

        private void ApplySprite(StrategyDeerSpritePose pose, int spriteFrame)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (hasAppliedPose && appliedPose == pose && appliedFrame == spriteFrame)
            {
                return;
            }

            StrategyDeerSex spriteSex = lifeStage == StrategyDeerLifeStage.Fawn ? StrategyDeerSex.Female : sex;
            spriteRenderer.sprite = pose switch
            {
                StrategyDeerSpritePose.Walk => StrategyDeerSpriteFactory.GetWalkSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Graze => StrategyDeerSpriteFactory.GetGrazeSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Alert => StrategyDeerSpriteFactory.GetAlertSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Run => StrategyDeerSpriteFactory.GetRunSprite(spriteSex, spriteFrame),
                StrategyDeerSpritePose.Rest => StrategyDeerSpriteFactory.GetRestSprite(spriteSex, spriteFrame),
                _ => StrategyDeerSpriteFactory.GetIdleSprite(spriteSex, spriteFrame)
            };

            appliedPose = pose;
            appliedFrame = spriteFrame;
            hasAppliedPose = true;
            SyncReadabilityRenderers();
        }

        private void UpdateAge()
        {
            if (lifeStage != StrategyDeerLifeStage.Fawn)
            {
                return;
            }

            ageSeconds += Time.deltaTime;
            if (ageSeconds >= FawnMaturitySeconds)
            {
                lifeStage = StrategyDeerLifeStage.Adult;
                ageSeconds = FawnMaturitySeconds;
                UpdateVisualScale();
                hasAppliedPose = false;
                appliedFrame = -1;
                ApplySprite(appliedPose, frame);
                StrategyDebugLogger.Info(
                    "Wildlife",
                    "DeerGrownUp",
                    StrategyDebugLogger.F("sex", sex),
                    StrategyDebugLogger.F("herd", herdId),
                    StrategyDebugLogger.F("world", transform.position));
                return;
            }

            UpdateVisualScale();
        }

        private void UpdateVisualScale()
        {
            visualScale = lifeStage == StrategyDeerLifeStage.Fawn
                ? Mathf.Lerp(FawnStartScale, FawnMatureScale, Mathf.Clamp01(ageSeconds / FawnMaturitySeconds))
                : 1f;
        }

        private void SetAnimatedScale(float x, float y)
        {
            transform.localScale = new Vector3(visualScale * DeerGlobalScale * x, visualScale * DeerGlobalScale * y, 1f);
        }
    }
}
