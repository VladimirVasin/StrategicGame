using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyRefugeeArrivalController : MonoBehaviour
    {
        private const int FirstArrivalHouseRequirement = 3;
        private const float RepeatArrivalMinSeconds = 420f;
        private const float RepeatArrivalMaxSeconds = 720f;
        private const int MaxRouteAttempts = 96;
        private const int MaxCampGatherRadius = 5;

        private readonly List<StrategyResidentAgent> activeFamily = new();
        private readonly List<Vector3> exitTargets = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private StrategyTimeScaleController timeScale;
        private StrategyRefugeeDialogController dialog;
        private RefugeeArrivalState state;
        private Vector2Int activeEntryCell;
        private Vector3 activeOutsideBaseWorld;
        private Vector2 activeFormationAxis = Vector2.right;
        private float arrivalTimer;
        private int familySequence;
        private bool firstArrivalTriggered;
        private bool loggedWaitingForFirstArrival;
        private bool pauseHeld;

        private enum RefugeeArrivalState
        {
            Waiting,
            WalkingToCamp,
            WaitingForDecision,
            Leaving
        }

        public void Configure(
            CityMapController mapController,
            StrategyPopulationController populationController,
            StrategyTimeScaleController timeScaleController,
            StrategyRefugeeDialogController dialogController)
        {
            map = mapController;
            population = populationController;
            timeScale = timeScaleController;
            dialog = dialogController;
            dialog?.Configure();
            if (state == RefugeeArrivalState.Waiting && !firstArrivalTriggered && !loggedWaitingForFirstArrival)
            {
                LogFirstArrivalWaiting();
            }
            else if (state == RefugeeArrivalState.Waiting && firstArrivalTriggered && arrivalTimer <= 0f)
            {
                ScheduleNextArrival();
            }
        }

        private void Update()
        {
            if (map == null || population == null)
            {
                return;
            }

            if (state == RefugeeArrivalState.Waiting)
            {
                if (!firstArrivalTriggered)
                {
                    UpdateFirstArrivalGate();
                    return;
                }

                arrivalTimer -= Time.deltaTime;
                if (arrivalTimer <= 0f)
                {
                    TryStartArrival(false);
                }

                return;
            }

            if (state == RefugeeArrivalState.WalkingToCamp)
            {
                if (HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity.ArrivingAsRefugee))
                {
                    OpenDecisionDialog();
                }

                return;
            }

            if (state == RefugeeArrivalState.Leaving
                && HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity.LeavingSettlement))
            {
                FinishRejectedDeparture();
            }
        }

        private void UpdateFirstArrivalGate()
        {
            if (population.CompletedHouseCount < FirstArrivalHouseRequirement)
            {
                return;
            }

            arrivalTimer -= Time.deltaTime;
            if (arrivalTimer > 0f)
            {
                return;
            }

            TryStartArrival(true);
        }

        private void TryStartArrival(bool firstArrival)
        {
            if (!population.TryGetCampCell(out Vector2Int campCell)
                || !population.TryGetCampWorld(out _))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "no_camp"));
                arrivalTimer = 45f;
                return;
            }

            int childCount = Random.Range(1, 4);
            int memberCount = 2 + childCount;
            if (!TryPrepareArrivalRoutes(memberCount, campCell, out List<List<Vector3>> routes))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "no_route"));
                arrivalTimer = 60f;
                return;
            }

            if (!population.TryCreateRefugeeFamily(
                    activeOutsideBaseWorld,
                    activeFormationAxis,
                    activeEntryCell,
                    childCount,
                    out List<StrategyResidentAgent> family))
            {
                StrategyDebugLogger.Warn("Refugees", "ArrivalDelayed", StrategyDebugLogger.F("reason", "family_create_failed"));
                arrivalTimer = 60f;
                return;
            }

            activeFamily.Clear();
            activeFamily.AddRange(family);
            familySequence++;
            if (firstArrival)
            {
                firstArrivalTriggered = true;
            }

            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                List<Vector3> route = routes[Mathf.Min(i, routes.Count - 1)];
                resident.FollowRefugeePath(route, false);
            }

            state = RefugeeArrivalState.WalkingToCamp;
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilySpawned",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("firstArrival", firstArrival),
                StrategyDebugLogger.F("completedHouses", population.CompletedHouseCount),
                StrategyDebugLogger.F("members", activeFamily.Count),
                StrategyDebugLogger.F("children", childCount),
                StrategyDebugLogger.F("entryCell", activeEntryCell),
                StrategyDebugLogger.F("outsideWorld", activeOutsideBaseWorld));
        }

        private void OpenDecisionDialog()
        {
            state = RefugeeArrivalState.WaitingForDecision;
            PushPause();
            if (dialog == null)
            {
                StrategyDebugLogger.Warn("Refugees", "DecisionAutoAccepted", StrategyDebugLogger.F("reason", "no_dialog"));
                HandleDecision(true);
                return;
            }

            dialog.Show(activeFamily, HandleDecision);
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivedAtCamp",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
        }

        private void HandleDecision(bool accepted)
        {
            if (state != RefugeeArrivalState.WaitingForDecision)
            {
                return;
            }

            if (accepted)
            {
                population.AcceptRefugeeFamily(activeFamily);
                activeFamily.Clear();
                exitTargets.Clear();
                ReleasePause();
                ScheduleNextArrival();
                return;
            }

            ReleasePause();
            StartRejectedDeparture();
        }

        private void StartRejectedDeparture()
        {
            exitTargets.Clear();
            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                List<Vector3> route = BuildDepartureRoute(resident, i);
                resident.FollowRefugeePath(route, true);
            }

            state = RefugeeArrivalState.Leaving;
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyRejected",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
        }

        private void FinishRejectedDeparture()
        {
            population.DestroyTemporaryResidents(activeFamily);
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyLeftMap",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("members", activeFamily.Count));
            activeFamily.Clear();
            exitTargets.Clear();
            ScheduleNextArrival();
        }

        private void ScheduleNextArrival()
        {
            arrivalTimer = Random.Range(RepeatArrivalMinSeconds, RepeatArrivalMaxSeconds);
            state = RefugeeArrivalState.Waiting;
            StrategyDebugLogger.Info(
                "Refugees",
                "ArrivalScheduled",
                StrategyDebugLogger.F("initial", false),
                StrategyDebugLogger.F("seconds", arrivalTimer));
        }

        private void LogFirstArrivalWaiting()
        {
            loggedWaitingForFirstArrival = true;
            arrivalTimer = 0f;
            StrategyDebugLogger.Info(
                "Refugees",
                "FirstArrivalWaitingForHouses",
                StrategyDebugLogger.F("requiredHouses", FirstArrivalHouseRequirement),
                StrategyDebugLogger.F("completedHouses", population != null ? population.CompletedHouseCount : 0));
        }

        private bool HasFamilyFinishedTravel(StrategyResidentAgent.ResidentActivity travelActivity)
        {
            bool hasLivingMember = false;
            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident == null)
                {
                    continue;
                }

                hasLivingMember = true;
                if (resident.Activity == travelActivity)
                {
                    return false;
                }
            }

            return hasLivingMember;
        }

        private bool TryPrepareArrivalRoutes(
            int memberCount,
            Vector2Int campCell,
            out List<List<Vector3>> routes)
        {
            routes = null;
            if (map == null || memberCount <= 0)
            {
                return false;
            }

            for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
            {
                Vector2Int entryCell = GetRandomEdgeCell();
                if (!map.IsCellWalkable(entryCell))
                {
                    continue;
                }

                HashSet<Vector2Int> usedTargets = new();
                List<List<Vector3>> preparedRoutes = new();
                bool allReady = true;
                for (int i = 0; i < memberCount; i++)
                {
                    if (!TryFindCampRoute(entryCell, campCell, usedTargets, i, out List<Vector2Int> cellPath))
                    {
                        allReady = false;
                        break;
                    }

                    preparedRoutes.Add(ToWorldRoute(cellPath));
                }

                if (!allReady || preparedRoutes.Count <= 0)
                {
                    continue;
                }

                Vector2 outward = GetOutwardDirection(entryCell);
                activeEntryCell = entryCell;
                activeFormationAxis = -outward;
                Vector3 entryWorld = map.GetCellCenterWorld(entryCell.x, entryCell.y);
                activeOutsideBaseWorld = new Vector3(
                    entryWorld.x + outward.x * map.CellSize * 2.8f,
                    entryWorld.y + outward.y * map.CellSize * 2.8f,
                    -0.08f);
                routes = preparedRoutes;
                return true;
            }

            return false;
        }

        private bool TryFindCampRoute(
            Vector2Int entryCell,
            Vector2Int campCell,
            HashSet<Vector2Int> usedTargets,
            int salt,
            out List<Vector2Int> cellPath)
        {
            cellPath = null;
            for (int radius = 1; radius <= MaxCampGatherRadius; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (usedTargets.Contains(candidate) || !map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        candidates.Add(candidate);
                    }
                }

                while (candidates.Count > 0)
                {
                    int index = Mathf.Abs(Random.Range(0, candidates.Count) + salt) % candidates.Count;
                    Vector2Int target = candidates[index];
                    candidates.RemoveAt(index);
                    if (TryBuildCellPath(entryCell, target, out cellPath))
                    {
                        usedTargets.Add(target);
                        return true;
                    }
                }
            }

            return false;
        }

        private List<Vector3> BuildDepartureRoute(StrategyResidentAgent resident, int memberIndex)
        {
            List<Vector3> route = new();
            if (resident != null
                && map.TryWorldToCell(resident.transform.position, out Vector2Int currentCell)
                && map.IsCellWalkable(currentCell)
                && TryBuildCellPath(currentCell, activeEntryCell, out List<Vector2Int> cellPath))
            {
                route.AddRange(ToWorldRoute(cellPath));
            }

            Vector3 offset = GetFormationOffset(memberIndex);
            Vector3 exitWorld = activeOutsideBaseWorld + offset;
            route.Add(exitWorld);
            exitTargets.Add(exitWorld);
            return route;
        }

        private bool TryBuildCellPath(Vector2Int startCell, Vector2Int targetCell, out List<Vector2Int> path)
        {
            path = null;
            if (!map.IsCellWalkable(startCell) || !map.IsCellWalkable(targetCell))
            {
                return false;
            }

            if (startCell == targetCell)
            {
                path = new List<Vector2Int> { startCell };
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
                    path = BuildCellPath(startCell, targetCell, cameFrom);
                    return path.Count > 0;
                }

                TryVisitNeighbor(current + Vector2Int.right, current, open, visited, cameFrom);
                TryVisitNeighbor(current + Vector2Int.left, current, open, visited, cameFrom);
                TryVisitNeighbor(current + Vector2Int.up, current, open, visited, cameFrom);
                TryVisitNeighbor(current + Vector2Int.down, current, open, visited, cameFrom);
            }

            return false;
        }

        private void TryVisitNeighbor(
            Vector2Int candidate,
            Vector2Int current,
            Queue<Vector2Int> open,
            HashSet<Vector2Int> visited,
            Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            if (visited.Contains(candidate) || !map.IsCellWalkable(candidate))
            {
                return;
            }

            visited.Add(candidate);
            cameFrom[candidate] = current;
            open.Enqueue(candidate);
        }

        private static List<Vector2Int> BuildCellPath(
            Vector2Int startCell,
            Vector2Int targetCell,
            Dictionary<Vector2Int, Vector2Int> cameFrom)
        {
            List<Vector2Int> cells = new();
            Vector2Int current = targetCell;
            cells.Add(current);
            while (current != startCell)
            {
                if (!cameFrom.TryGetValue(current, out current))
                {
                    cells.Clear();
                    return cells;
                }

                cells.Add(current);
            }

            cells.Reverse();
            return cells;
        }

        private List<Vector3> ToWorldRoute(IReadOnlyList<Vector2Int> cells)
        {
            List<Vector3> route = new();
            if (cells == null)
            {
                return route;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector3 world = map.GetCellCenterWorld(cells[i].x, cells[i].y);
                route.Add(new Vector3(world.x, world.y, -0.08f));
            }

            return route;
        }

        private Vector2Int GetRandomEdgeCell()
        {
            int side = Random.Range(0, 4);
            return side switch
            {
                0 => new Vector2Int(0, Random.Range(0, map.Height)),
                1 => new Vector2Int(map.Width - 1, Random.Range(0, map.Height)),
                2 => new Vector2Int(Random.Range(0, map.Width), 0),
                _ => new Vector2Int(Random.Range(0, map.Width), map.Height - 1)
            };
        }

        private Vector2 GetOutwardDirection(Vector2Int entryCell)
        {
            if (entryCell.x <= 0)
            {
                return Vector2.left;
            }

            if (entryCell.x >= map.Width - 1)
            {
                return Vector2.right;
            }

            if (entryCell.y <= 0)
            {
                return Vector2.down;
            }

            return Vector2.up;
        }

        private Vector3 GetFormationOffset(int index)
        {
            Vector2 axis = activeFormationAxis.sqrMagnitude > 0.001f
                ? activeFormationAxis.normalized
                : Vector2.right;
            float side = index switch
            {
                0 => -0.35f,
                1 => 0.35f,
                2 => -0.75f,
                3 => 0.75f,
                _ => 0f
            };
            float back = index <= 1 ? 0f : -0.42f - (index - 2) * 0.18f;
            Vector2 perpendicular = new Vector2(-axis.y, axis.x);
            Vector2 offset = (axis * back + perpendicular * side) * map.CellSize;
            return new Vector3(offset.x, offset.y, 0f);
        }

        private void PushPause()
        {
            if (pauseHeld)
            {
                return;
            }

            pauseHeld = true;
            timeScale?.PushPauseLock("RefugeeDecision");
        }

        private void ReleasePause()
        {
            if (!pauseHeld)
            {
                return;
            }

            pauseHeld = false;
            timeScale?.PopPauseLock("RefugeeDecision");
        }

        private void OnDisable()
        {
            ReleasePause();
        }
    }
}
