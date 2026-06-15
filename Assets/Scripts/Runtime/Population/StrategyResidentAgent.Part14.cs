using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {

        private void ResetFisherWorkToIdle(bool releaseReservation)
        {
            if (releaseReservation && activeFishTarget != null)
            {
                activeFishTarget.ReleaseFishingReservation(this);
            }

            activeFishTarget = null;
            fishingLineCast = false;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (carriedFishAmount > 0 && TryStartCarriedResourceReturn("fisher_work_cancelled"))
            {
                return;
            }

            carriedFishAmount = 0;
            SetCarriedFishVisible(false);
            SetFishingLineVisible(false);
            UseIdleSprite();
            fishingWorkCooldown = Random.Range(2.0f, 4.5f);
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private void StartPlantingTree()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            activity = ResidentActivity.PlantingTree;
            lumberWorkTimer = Random.Range(LumberPlantSecondsMin, LumberPlantSecondsMax);
            if (map != null)
            {
                FaceWorldPoint(map.GetCellCenterWorld(plantingCell.x, plantingCell.y));
            }

            StrategyDebugLogger.Info(
                "Population",
                "PlantingStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", plantingCell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
        }

        private void UpdatePlantingTree()
        {
            lumberWorkTimer -= Time.deltaTime;
            AnimateLumberWork(6.4f, 4.5f);
            if (lumberWorkTimer > 0f)
            {
                return;
            }

            bool planted = workplace != null && workplace.TryPlantTree(plantingCell);
            StrategyDebugLogger.Info(
                "Population",
                planted ? "TreePlanted" : "TreePlantFailed",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("plantCell", plantingCell),
                StrategyDebugLogger.F("campOrigin", workplace != null ? workplace.Origin : Vector2Int.zero));
            activity = ResidentActivity.Idle;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            lumberWorkCooldown = Random.Range(5.0f, 9.0f);
            waitTimer = Random.Range(0.35f, 0.9f);
        }

        private void PickNextIdleTarget()
        {
            for (int attempt = 0; attempt < 18; attempt++)
            {
                int minX = idleOrigin.x - IdleRadius;
                int maxX = idleOrigin.x + idleFootprint.x + IdleRadius - 1;
                int minY = idleOrigin.y - IdleRadius;
                int maxY = idleOrigin.y + idleFootprint.y + IdleRadius - 1;
                Vector2Int cell = new Vector2Int(
                    Random.Range(minX, maxX + 1),
                    Random.Range(minY, maxY + 1));

                if (!map.IsCellWalkable(cell))
                {
                    continue;
                }

                if (TryBuildPathTo(cell))
                {
                    activity = GetRestingActivity();
                    hasTarget = true;
                    waitTimer = Random.Range(0.15f, 0.55f);
                    return;
                }
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.35f, 0.85f);
        }

        private bool TryBuildPathTo(Vector2Int targetCell)
        {
            if (map == null
                || !TryGetPathStartCell(out Vector2Int startCell)
                || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path.Clear();
                path.Add(new Vector3(transform.position.x, transform.position.y, -0.08f));
                pathIndex = 0;
                return true;
            }

            Queue<Vector2Int> open = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();
            HashSet<Vector2Int> visited = new();

            open.Enqueue(startCell);
            visited.Add(startCell);

            int visitLimit = Mathf.Max(256, map.Width * map.Height);
            while (open.Count > 0 && visited.Count < visitLimit)
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
                    if (visited.Contains(next) || !map.IsCellWalkable(next))
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

        private bool TryGetPathStartCell(out Vector2Int startCell)
        {
            startCell = default;
            if (map == null || !map.TryWorldToCell(transform.position, out Vector2Int currentCell))
            {
                return false;
            }

            if (map.IsCellWalkable(currentCell))
            {
                startCell = currentCell;
                return true;
            }

            Vector2Int bestCell = currentCell;
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
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 candidateWorld = map.GetCellCenterWorld(candidate.x, candidate.y);
                        float distance = (candidateWorld - transform.position).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestCell = candidate;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            Vector3 recoveryWorld = map.GetCellCenterWorld(bestCell.x, bestCell.y);
            transform.position = new Vector3(recoveryWorld.x, recoveryWorld.y, transform.position.z);
            startCell = bestCell;
            StrategyDebugLogger.Warn(
                "Resident",
                "PathStartRecovered",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("fromCell", currentCell),
                StrategyDebugLogger.F("toCell", bestCell));
            return true;
        }

        private void StartMovingHome(Vector3 targetWorld)
        {
            bool hasGridPath = map != null
                && map.TryWorldToCell(targetWorld, out Vector2Int targetCell)
                && TryBuildPathTo(targetCell);

            if (!hasGridPath)
            {
                BuildDirectWorldPath(targetWorld);
            }

            activity = ResidentActivity.MovingHome;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
        }

        private void UpdateHomeboundChild()
        {
            if (!hiddenInsideHome || activity != ResidentActivity.StayingInsideHome)
            {
                EnterHomeboundChildState(!hiddenInsideHome);
            }

            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            footstepAudio?.ResetStepPhase();
        }

        private void EnterHomeboundChildState(bool log)
        {
            if (home == null)
            {
                return;
            }

            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.45f, 1.15f);
            activity = ResidentActivity.StayingInsideHome;
            activeGarden = null;
            usingWalkSprite = false;
            usingWorkSprite = false;
            appliedWalkFrame = -1;
            appliedWorkFrame = -1;
            carriedLogAmount = 0;
            carriedStoneAmount = 0;
            carriedGameAmount = 0;
            carriedFishAmount = 0;
            carriedForageAmount = 0;
            carriedForageResource = StrategyResourceType.None;
            SetCarriedLogsVisible(false);
            SetCarriedStoneVisible(false);
            SetCarriedGameVisible(false);
            SetCarriedFishVisible(false);
            SetCarriedForageVisible(false);
            SetFishingLineVisible(false);
            transform.position = GetHomeInteriorWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetWorldPresenceVisible(false);
            hiddenInsideHome = true;

            if (log)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentChildStayedInsideHome",
                    StrategyDebugLogger.F("resident", FullName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("age", ageYears),
                    StrategyDebugLogger.F("homeOrigin", home.Origin));
            }
        }

        private void ReleaseHomeboundChild()
        {
            hiddenInsideHome = false;
            SetWorldPresenceVisible(true);
            transform.position = GetHomeExitWorld();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            activity = ResidentActivity.Idle;
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = Random.Range(0.45f, 1.15f);
            UseIdleSprite();
            UpdateWorldSorting();
            StrategyDebugLogger.Info(
                "Population",
                "ResidentChildLeftHome",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", ageYears),
                StrategyDebugLogger.F("homeOrigin", home != null ? home.Origin : Vector2Int.zero));
        }

        private Vector3 GetHomeInteriorWorld()
        {
            if (map != null && home != null)
            {
                Bounds homeBounds = map.GetCellRectWorld(home.Origin, home.Footprint);
                return new Vector3(homeBounds.center.x, homeBounds.center.y, -0.08f);
            }

            return new Vector3(transform.position.x, transform.position.y, -0.08f);
        }

        private Vector3 GetHomeExitWorld()
        {
            if (TryFindHomeExitWorld(out Vector3 exitWorld))
            {
                return exitWorld;
            }

            return GetHomeInteriorWorld();
        }

        private bool TryFindHomeExitWorld(out Vector3 exitWorld)
        {
            if (map == null || home == null)
            {
                exitWorld = default;
                return false;
            }

            Vector2Int origin = home.Origin;
            Vector2Int footprint = home.Footprint;
            Vector3 chosen = default;
            int found = 0;
            for (int radius = 1; radius <= IdleRadius; radius++)
            {
                int minX = origin.x - radius;
                int maxX = origin.x + footprint.x + radius - 1;
                int minY = origin.y - radius;
                int maxY = origin.y + footprint.y + radius - 1;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (x != minX && x != maxX && y != minY && y != maxY)
                        {
                            continue;
                        }

                        Vector2Int candidate = new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        Vector3 center = map.GetCellCenterWorld(candidate.x, candidate.y);
                        Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                        center.x += jitter.x;
                        center.y += jitter.y;
                        center.z = -0.08f;
                        found++;
                        if (Random.Range(0, found) == 0)
                        {
                            chosen = center;
                        }
                    }
                }

                if (found > 0)
                {
                    exitWorld = chosen;
                    return true;
                }
            }

            exitWorld = default;
            return false;
        }

        private void SetWorldPresenceVisible(bool visible)
        {
            if (visible)
            {
                EnsureClickCollider();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = visible;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = visible;
            }

            Collider2D[] colliders = GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = visible && !deathRequested;
            }

            if (!visible)
            {
                SetCarriedLogsVisible(false);
                SetCarriedStoneVisible(false);
                SetCarriedGameVisible(false);
                SetCarriedFishVisible(false);
                SetCarriedForageVisible(false);
                SetFishingLineVisible(false);
            }
        }

        private void BuildDirectWorldPath(Vector3 targetWorld)
        {
            path.Clear();
            path.Add(new Vector3(targetWorld.x, targetWorld.y, -0.08f));
            pathIndex = 0;
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
                    Vector2 jitter = Random.insideUnitCircle * (map.CellSize * 0.18f);
                    center.x += jitter.x;
                    center.y += jitter.y;
                }

                path.Add(new Vector3(center.x, center.y, -0.08f));
            }

            pathIndex = 0;
        }
    }
}
