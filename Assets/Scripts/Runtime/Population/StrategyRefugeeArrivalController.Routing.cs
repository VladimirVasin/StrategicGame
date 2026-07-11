using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyRefugeeArrivalController
    {
        private Coroutine arrivalPreparationRoutine;

        private bool BeginPrepareArrival(
            bool firstArrival,
            Vector2Int campCell,
            int memberCount,
            int parentCount,
            int childCount)
        {
            if (arrivalPreparationRoutine != null || memberCount <= 0)
            {
                return false;
            }

            state = RefugeeArrivalState.PreparingRoutes;
            arrivalPreparationRoutine = StartCoroutine(PrepareArrivalRoutine(
                firstArrival,
                campCell,
                memberCount,
                parentCount,
                childCount));
            return true;
        }

        private IEnumerator PrepareArrivalRoutine(
            bool firstArrival,
            Vector2Int campCell,
            int memberCount,
            int parentCount,
            int childCount)
        {
            yield return null;

            if (!TryBuildCampArrivalTargets(
                    campCell,
                    out HashSet<Vector2Int> campArrivalTargets,
                    out HashSet<Vector2Int> campReachableCells))
            {
                FailArrivalPreparation("no_reachable_camp_targets");
                yield break;
            }

            yield return null;
            for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
            {
                if (!TryGetRandomArrivalEntryCell(out Vector2Int entryCell, out Vector2 outward)
                    || !campReachableCells.Contains(entryCell))
                {
                    if ((attempt + 1) % 4 == 0)
                    {
                        yield return null;
                    }

                    continue;
                }

                HashSet<Vector2Int> usedTargets = new();
                List<List<Vector3>> preparedRoutes = new(memberCount);
                bool allReady = true;
                for (int memberIndex = 0; memberIndex < memberCount; memberIndex++)
                {
                    if (!TryFindCampRoute(
                            entryCell,
                            campCell,
                            campArrivalTargets,
                            usedTargets,
                            memberIndex,
                            out List<Vector2Int> cellPath))
                    {
                        allReady = false;
                        break;
                    }

                    preparedRoutes.Add(ToWorldRoute(cellPath));
                    yield return null;
                }

                if (!allReady || preparedRoutes.Count != memberCount)
                {
                    continue;
                }

                activeEntryCell = entryCell;
                activeFormationAxis = -outward;
                Vector3 entryWorld = map.GetCellCenterWorld(entryCell.x, entryCell.y);
                activeOutsideBaseWorld = new Vector3(entryWorld.x, entryWorld.y, -0.08f);
                arrivalPreparationRoutine = null;
                CompletePreparedArrival(
                    firstArrival,
                    parentCount,
                    childCount,
                    preparedRoutes);
                yield break;
            }

            FailArrivalPreparation("no_connected_entry_route");
        }

        private bool TryFindCampRoute(
            Vector2Int entryCell,
            Vector2Int campCell,
            HashSet<Vector2Int> campArrivalTargets,
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
                        if (usedTargets.Contains(candidate)
                            || campArrivalTargets == null
                            || !campArrivalTargets.Contains(candidate)
                            || !map.IsCellWalkable(candidate))
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

        private void CompletePreparedArrival(
            bool firstArrival,
            int parentCount,
            int childCount,
            List<List<Vector3>> routes)
        {
            if (!population.TryCreateRefugeeFamily(
                    activeOutsideBaseWorld,
                    activeFormationAxis,
                    activeEntryCell,
                    parentCount,
                    childCount,
                    out List<StrategyResidentAgent> family))
            {
                FailArrivalPreparation("family_create_failed");
                return;
            }

            activeFamily.Clear();
            activeFamily.AddRange(family);
            familySequence++;
            if (firstArrival)
            {
                firstArrivalTriggered = true;
                lastDynamicArrivalRollDayIndex = StrategyDayNightCycleController.CurrentCalendarSnapshot.DayIndex;
            }

            for (int i = 0; i < activeFamily.Count; i++)
            {
                StrategyResidentAgent resident = activeFamily[i];
                if (resident != null)
                {
                    resident.FollowRefugeePath(routes[Mathf.Min(i, routes.Count - 1)], false);
                }
            }

            state = RefugeeArrivalState.WalkingToCamp;
            StrategyDebugLogger.Info(
                "Refugees",
                "FamilySpawned",
                StrategyDebugLogger.F("familyId", familySequence),
                StrategyDebugLogger.F("firstArrival", firstArrival),
                StrategyDebugLogger.F("completedHouses", population.CompletedHouseCount),
                StrategyDebugLogger.F("members", activeFamily.Count),
                StrategyDebugLogger.F("parents", parentCount),
                StrategyDebugLogger.F("children", childCount),
                StrategyDebugLogger.F("entryCell", activeEntryCell),
                StrategyDebugLogger.F("spawnWorld", activeOutsideBaseWorld));
        }

        private void FailArrivalPreparation(string reason)
        {
            arrivalPreparationRoutine = null;
            state = RefugeeArrivalState.Waiting;
            arrivalTimer = 60f;
            StrategyDebugLogger.Warn(
                "Refugees",
                "ArrivalDelayed",
                StrategyDebugLogger.F("reason", reason));
        }

        private void CancelArrivalPreparation()
        {
            if (arrivalPreparationRoutine != null)
            {
                StopCoroutine(arrivalPreparationRoutine);
                arrivalPreparationRoutine = null;
            }
        }
    }
}
