using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWolfAgent
    {
        private const float WolfPathLogCooldownSeconds = 2.75f;
        private const float WolfStallLogCooldownSeconds = 3.5f;
        private const float WolfStallSeconds = 1.35f;
        private const float WolfStallProgressSqr = 0.000004f;

        private float movementStallTimer;
        private float nextMovementStallLogTime;
        private float nextPathLogTime;
        private string lastMovementMode = string.Empty;

        private void SetWolfState(StrategyWolfBehaviorState nextState, string reason)
        {
            StrategyWolfBehaviorState previousState = state;
            state = nextState;
            if (previousState == nextState)
            {
                return;
            }

            ResetWolfMovementDiagnostics();
            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfStateChanged",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("from", previousState),
                StrategyDebugLogger.F("to", nextState),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("home", homeCell),
                StrategyDebugLogger.F("target", GetTargetDebugKind()),
                StrategyDebugLogger.F("targetName", GetTargetDebugName()),
                StrategyDebugLogger.F("pathIndex", pathIndex),
                StrategyDebugLogger.F("pathCount", path.Count));
        }

        private void TrackWolfMovementAttempt(string mode, Vector3 previous, Vector3 current, Vector3 target, float speed)
        {
            if (!IsWolfMovementState())
            {
                return;
            }

            if ((current - previous).sqrMagnitude > WolfStallProgressSqr)
            {
                ResetWolfMovementDiagnostics();
                return;
            }

            if (lastMovementMode != mode)
            {
                lastMovementMode = mode;
                movementStallTimer = 0f;
            }

            movementStallTimer += Time.deltaTime;
            if (movementStallTimer < WolfStallSeconds || Time.time < nextMovementStallLogTime)
            {
                return;
            }

            nextMovementStallLogTime = Time.time + WolfStallLogCooldownSeconds;
            StrategyDebugLogger.Warn(
                "Wildlife",
                "WolfMovementStalled",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("state", state),
                StrategyDebugLogger.F("mode", mode),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("world", current),
                StrategyDebugLogger.F("targetWorld", target),
                StrategyDebugLogger.F("target", GetTargetDebugKind()),
                StrategyDebugLogger.F("targetName", GetTargetDebugName()),
                StrategyDebugLogger.F("speed", speed),
                StrategyDebugLogger.F("pathIndex", pathIndex),
                StrategyDebugLogger.F("pathCount", path.Count));
        }

        private void LogWolfTargetAcquired(string targetKind, string targetName)
        {
            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfTargetAcquired",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("target", targetKind),
                StrategyDebugLogger.F("targetName", targetName),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("targetCell", GetTargetDebugCell()),
                StrategyDebugLogger.F("targetWorld", GetTargetDebugWorld()));
        }

        private void LogWolfResidentAttackResult(bool killed, string residentName, Vector3 attackWorld)
        {
            StrategyDebugLogger.Info(
                "Wildlife",
                killed ? "WolfResidentKilled" : "WolfResidentAttackFailed",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("resident", residentName),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("world", attackWorld));
        }

        private void LogWolfTargetReleased()
        {
            if (targetRabbit == null && targetDeer == null && targetResident == null)
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfTargetReleased",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("state", state),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("target", GetTargetDebugKind()),
                StrategyDebugLogger.F("targetName", GetTargetDebugName()),
                StrategyDebugLogger.F("targetCell", GetTargetDebugCell()),
                StrategyDebugLogger.F("targetWorld", GetTargetDebugWorld()));
        }

        private bool LogWolfPathFailed(string reason, Vector2Int targetCell)
        {
            if (Time.time < nextPathLogTime)
            {
                return false;
            }

            nextPathLogTime = Time.time + WolfPathLogCooldownSeconds;
            bool hasCurrentCell = TryGetCurrentCell(out Vector2Int currentCell);
            bool currentWalkable = hasCurrentCell
                && StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, currentCell);
            bool targetWalkable = StrategyWildlifeRiverCrossing.IsLandOrRiverCell(map, targetCell);
            StrategyDebugLogger.Warn(
                "Wildlife",
                "WolfPathFailed",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("state", state),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cell", hasCurrentCell ? currentCell : Vector2Int.zero),
                StrategyDebugLogger.F("currentWalkable", currentWalkable),
                StrategyDebugLogger.F("targetCell", targetCell),
                StrategyDebugLogger.F("targetWalkable", targetWalkable),
                StrategyDebugLogger.F("target", GetTargetDebugKind()),
                StrategyDebugLogger.F("pathIndex", pathIndex),
                StrategyDebugLogger.F("pathCount", path.Count));
            return false;
        }

        private bool LogWolfRoamFailed(string reason, Vector2Int currentCell, bool preferSafety)
        {
            if (Time.time < nextPathLogTime)
            {
                return false;
            }

            nextPathLogTime = Time.time + WolfPathLogCooldownSeconds;
            StrategyDebugLogger.Warn(
                "Wildlife",
                "WolfRoamFailed",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("state", state),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cell", currentCell),
                StrategyDebugLogger.F("home", homeCell),
                StrategyDebugLogger.F("homeRadius", homeRadius),
                StrategyDebugLogger.F("preferSafety", preferSafety));
            return false;
        }

        private void LogWolfPathReady(string reason, Vector2Int requestedCell, Vector2Int pathCell)
        {
            if (Time.time < nextPathLogTime)
            {
                return;
            }

            nextPathLogTime = Time.time + WolfPathLogCooldownSeconds;
            StrategyDebugLogger.Info(
                "Wildlife",
                "WolfPathReady",
                StrategyDebugLogger.F("wolf", GetEntityId()),
                StrategyDebugLogger.F("pack", PackId),
                StrategyDebugLogger.F("state", state),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("cell", GetDebugCell()),
                StrategyDebugLogger.F("requestedCell", requestedCell),
                StrategyDebugLogger.F("pathCell", pathCell),
                StrategyDebugLogger.F("target", GetTargetDebugKind()),
                StrategyDebugLogger.F("pathIndex", pathIndex),
                StrategyDebugLogger.F("pathCount", path.Count));
        }

        private Vector2Int GetDebugCell()
        {
            return TryGetCurrentCell(out Vector2Int currentCell) ? currentCell : Vector2Int.zero;
        }

        private Vector2Int GetTargetDebugCell()
        {
            return TryGetKnownTargetCell(out Vector2Int targetCell) ? targetCell : Vector2Int.zero;
        }

        private Vector3 GetTargetDebugWorld()
        {
            if (targetRabbit != null)
            {
                return targetRabbit.transform.position;
            }

            if (targetDeer != null)
            {
                return targetDeer.transform.position;
            }

            return targetResident != null ? targetResident.transform.position : Vector3.zero;
        }

        private bool TryGetKnownTargetCell(out Vector2Int cell)
        {
            cell = default;
            if (targetRabbit != null && targetRabbit.TryGetCurrentCell(out cell))
            {
                return true;
            }

            if (targetDeer != null && targetDeer.TryGetCurrentCell(out cell))
            {
                return true;
            }

            return targetResident != null
                && map != null
                && map.TryWorldToCell(targetResident.transform.position, out cell);
        }

        private string GetTargetDebugKind()
        {
            if (targetRabbit != null)
            {
                return "rabbit";
            }

            if (targetDeer != null)
            {
                return "deer";
            }

            return targetResident != null ? "resident" : "none";
        }

        private string GetTargetDebugName()
        {
            if (targetResident != null)
            {
                return targetResident.FullName;
            }

            if (targetRabbit != null)
            {
                return targetRabbit.GetEntityId().ToString();
            }

            return targetDeer != null ? targetDeer.GetEntityId().ToString() : "none";
        }

        private bool IsWolfMovementState()
        {
            return state == StrategyWolfBehaviorState.Roaming
                || state == StrategyWolfBehaviorState.Stalking
                || state == StrategyWolfBehaviorState.Chasing
                || state == StrategyWolfBehaviorState.AvoidingSettlement;
        }

        private void ResetWolfMovementDiagnostics()
        {
            movementStallTimer = 0f;
            lastMovementMode = string.Empty;
        }
    }
}
