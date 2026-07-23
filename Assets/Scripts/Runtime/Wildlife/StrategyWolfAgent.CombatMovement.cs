using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private const float CombatApproachEpsilon = 0.001f;

        private void UpdateChasing()
        {
            if (!TryGetTargetWorld(out Vector3 targetWorld, out Vector2Int targetCell))
            {
                ReleaseTargets();
                StartIdle(Random.Range(0.3f, 0.8f));
                return;
            }

            float distance = Vector2.Distance(transform.position, targetWorld);
            if (distance > MaxChaseDistance
                || (!IsForcedCombatEncounter
                    && wildlife != null
                    && wildlife.IsWolfUnsafeSettlementCell(targetCell)))
            {
                ReleaseTargets();
                StartAvoidingSettlement();
                return;
            }

            FaceWorldPoint(targetWorld);
            if (TryStartOrHoldChasingAttack(distance))
            {
                return;
            }

            if (targetRefreshTimer <= 0f)
            {
                targetRefreshTimer = TargetRefreshInterval;
                TryPathNearTarget(targetCell);
            }

            if (path.Count > 0 && pathIndex < path.Count)
            {
                MoveAlongChasingPath(PounceSpeed, distance);
            }
            else
            {
                MoveDirectlyTowardChaseTarget(targetWorld, PounceSpeed, distance);
            }

            distance = Vector2.Distance(transform.position, targetWorld);
            if (TryStartOrHoldChasingAttack(distance))
            {
                return;
            }

            AnimateRunOrSwim();
        }

        private bool TryStartOrHoldChasingAttack(float distance)
        {
            if (distance > AttackReachDistance + CombatApproachEpsilon)
            {
                return false;
            }

            path.Clear();
            pathIndex = 0;
            if (CanStartCombatAttack())
            {
                StartAttack();
            }
            else
            {
                AnimateStalkOrSwim();
            }

            return true;
        }

        private void MoveAlongChasingPath(float speed, float distanceToTarget)
        {
            if (path.Count <= 0 || pathIndex >= path.Count)
            {
                return;
            }

            Vector3 waypoint = path[pathIndex];
            waypoint.z = transform.position.z;
            Vector3 previous = transform.position;
            float requestedTravel = StrategyWildlifeRiverCrossing.GetAdjustedSpeed(
                map,
                previous,
                waypoint,
                speed) * Time.deltaTime;
            float allowedTravel = StrategyCombatRules.ClampApproachTravel(
                distanceToTarget,
                AttackReachDistance,
                requestedTravel);
            transform.position = Vector3.MoveTowards(previous, waypoint, allowedTravel);
            Vector3 delta = transform.position - previous;
            TrackWolfMovementAttempt("combat_path", previous, transform.position, waypoint, speed);
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }

            if (Vector2.Distance(transform.position, waypoint) <= TargetReachDistance)
            {
                pathIndex++;
            }
        }

        private void MoveDirectlyTowardChaseTarget(
            Vector3 targetWorld,
            float speed,
            float distanceToTarget)
        {
            Vector3 target = new(targetWorld.x, targetWorld.y, transform.position.z);
            Vector3 previous = transform.position;
            float requestedTravel = StrategyWildlifeRiverCrossing.GetAdjustedSpeed(
                map,
                previous,
                target,
                speed) * Time.deltaTime;
            float allowedTravel = StrategyCombatRules.ClampApproachTravel(
                distanceToTarget,
                AttackReachDistance,
                requestedTravel);
            Vector3 next = Vector3.MoveTowards(previous, target, allowedTravel);
            if (!CanUseForcedCombatDirectStep(next))
            {
                TrackWolfMovementAttempt("combat_direct_blocked", previous, previous, target, speed);
                return;
            }

            transform.position = next;
            Vector3 delta = transform.position - previous;
            TrackWolfMovementAttempt("combat_direct", previous, transform.position, target, speed);
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.001f)
            {
                spriteRenderer.flipX = delta.x < 0f;
            }
        }
    }
}
