using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyCemeteryController : MonoBehaviour
    {
        private const int PreferredMinimumCampDistance = 18;
        private const int PreferredTargetCampDistance = 36;
        private const int PreferredMaximumCampDistance = 58;
        private const int PreferredMinimumBuildingDistance = 10;
        private const int PreferredEdgePadding = 6;
        private const int GraveSearchRadius = 18;
        private static readonly Vector2Int[] GraveStandOffsets =
        {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, 1)
        };

        private readonly List<Vector2Int> graves = new();
        private readonly HashSet<Vector2Int> reservedGraves = new();
        private CityMapController map;
        private StrategyPopulationController population;
        private Transform graveRoot;
        private Vector2Int centerCell;
        private bool hasCenter;

        public bool HasCemetery => hasCenter;
        public Vector2Int CenterCell => centerCell;

        public void Configure(CityMapController mapController, StrategyPopulationController populationController)
        {
            map = mapController;
            population = populationController;
            EnsureGraveRoot();
        }

        public bool TryReserveGraveCell(out Vector2Int cell, out Vector3 world)
        {
            return TryReserveGraveCell(null, 0, out cell, out world);
        }

        public bool TryReserveGraveCell(
            HashSet<Vector2Int> reachableCells,
            int requiredStandCells,
            out Vector2Int cell,
            out Vector3 world)
        {
            cell = default;
            world = default;
            if (map == null)
            {
                return false;
            }

            if (!hasCenter && !TryFindInitialCemeteryCell(reachableCells, requiredStandCells, out centerCell))
            {
                return false;
            }

            hasCenter = true;
            if (!TryFindNextGraveCell(reachableCells, requiredStandCells, out cell))
            {
                return false;
            }

            reservedGraves.Add(cell);
            world = map.GetCellCenterWorld(cell.x, cell.y);
            return true;
        }

        public void ReleaseGraveReservation(Vector2Int cell)
        {
            reservedGraves.Remove(cell);
        }

        public bool TryCreateGrave(StrategyResidentDeathSnapshot snapshot, Vector2Int cell, out string failureReason)
        {
            failureReason = null;
            if (map == null)
            {
                failureReason = "map_unavailable";
                return false;
            }

            if (!IsValidGraveCell(cell))
            {
                failureReason = "grave_cell_invalid";
                return false;
            }

            if (IsOccupiedByResident(cell))
            {
                failureReason = "grave_cell_occupied";
                return false;
            }

            EnsureGraveRoot();
            reservedGraves.Remove(cell);
            graves.Add(cell);
            map.SetCellsWalkable(cell, Vector2Int.one, false);

            Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
            GameObject graveObject = new GameObject("Grave - " + snapshot.FullName);
            graveObject.transform.SetParent(graveRoot, false);
            graveObject.transform.position = new Vector3(world.x, world.y, -0.11f);

            SpriteRenderer renderer = graveObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyFuneralSpriteFactory.GetGraveSprite(graves.Count - 1);
            StrategyWorldSorting.Apply(renderer, graveObject.transform.position, 1);

            StrategyGraveMarker grave = graveObject.AddComponent<StrategyGraveMarker>();
            grave.Configure(snapshot, renderer, graves.Count - 1);

            StrategyDebugLogger.Info(
                "Funeral",
                "GraveCreated",
                StrategyDebugLogger.F("resident", snapshot.FullName),
                StrategyDebugLogger.F("residentId", snapshot.ResidentId),
                StrategyDebugLogger.F("profession", snapshot.FinalProfession),
                StrategyDebugLogger.F("familyRole", snapshot.FamilyRole),
                StrategyDebugLogger.F("graveCell", cell),
                StrategyDebugLogger.F("cemeteryCenter", centerCell),
                StrategyDebugLogger.F("graveCount", graves.Count));
            return true;
        }

        private bool IsOccupiedByResident(Vector2Int cell)
        {
            if (population == null || map == null)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || resident.IsPendingRefugee
                    || resident.IsSleepingInsideHome
                    || resident.IsHomeboundYoungChild
                    || resident.Activity == StrategyResidentAgent.ResidentActivity.StayingInsideHome
                    || !resident.gameObject.activeInHierarchy
                    || !map.TryWorldToCell(resident.transform.position, out Vector2Int residentCell))
                {
                    continue;
                }

                if (residentCell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryFindInitialCemeteryCell(
            HashSet<Vector2Int> reachableCells,
            int requiredStandCells,
            out Vector2Int bestCell)
        {
            bestCell = default;
            float bestScore = float.MinValue;
            bool found = false;
            float searchStartedAt = Time.realtimeSinceStartup;
            Vector2Int campCell = default;
            bool hasCampCell = population != null && population.TryGetCampCell(out campCell);
            List<Vector2Int> buildingOrigins = CollectBuildingOrigins();
            int testedCells = 0;

            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        if (!IsValidGraveCell(candidate)
                            || !HasReachableStandCells(candidate, reachableCells, requiredStandCells))
                        {
                            continue;
                        }

                        testedCells++;
                        float campDistance = GetCampDistance(candidate, hasCampCell, campCell);
                        float buildingDistance = GetNearestBuildingDistance(candidate, buildingOrigins);
                        if (pass == 0
                            && (campDistance < PreferredMinimumCampDistance
                                || buildingDistance < PreferredMinimumBuildingDistance))
                        {
                            continue;
                        }

                        float distanceScore = -Mathf.Abs(campDistance - PreferredTargetCampDistance) * 2.2f;
                        float farPenalty = campDistance > PreferredMaximumCampDistance
                            ? (campDistance - PreferredMaximumCampDistance) * 5.5f
                            : 0f;
                        float buildingScore = Mathf.Min(buildingDistance, PreferredMinimumBuildingDistance * 2f) * 1.8f;
                        float score = distanceScore
                            + buildingScore
                            + GetTerrainScore(candidate)
                            - GetMapEdgePenalty(candidate)
                            - farPenalty;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCell = candidate;
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    StrategyDebugLogger.Info(
                        "Funeral",
                        "CemeteryFounded",
                        StrategyDebugLogger.F("cell", bestCell),
                        StrategyDebugLogger.F("pass", pass),
                        StrategyDebugLogger.F("campDistance", GetCampDistance(bestCell, hasCampCell, campCell)),
                        StrategyDebugLogger.F("buildingDistance", GetNearestBuildingDistance(bestCell, buildingOrigins)),
                        StrategyDebugLogger.F("edgeDistance", GetMapEdgeDistance(bestCell)),
                        StrategyDebugLogger.F("score", bestScore),
                        StrategyDebugLogger.F("testedCells", testedCells),
                        StrategyDebugLogger.F("buildingCount", buildingOrigins.Count),
                        StrategyDebugLogger.F("searchMs", Mathf.RoundToInt((Time.realtimeSinceStartup - searchStartedAt) * 1000f)));
                    return true;
                }
            }

            return false;
        }

        private bool TryFindNextGraveCell(
            HashSet<Vector2Int> reachableCells,
            int requiredStandCells,
            out Vector2Int cell)
        {
            if (IsValidGraveCell(centerCell)
                && !graves.Contains(centerCell)
                && !reservedGraves.Contains(centerCell)
                && HasReachableStandCells(centerCell, reachableCells, requiredStandCells))
            {
                cell = centerCell;
                return true;
            }

            for (int radius = 1; radius <= GraveSearchRadius; radius++)
            {
                List<Vector2Int> candidates = new();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = centerCell + new Vector2Int(x, y);
                        if (IsValidGraveCell(candidate)
                            && !graves.Contains(candidate)
                            && !reservedGraves.Contains(candidate)
                            && HasReachableStandCells(candidate, reachableCells, requiredStandCells))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[Random.Range(0, candidates.Count)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsValidGraveCell(Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return false;
            }

            if (mapCell.Kind == CityMapCellKind.Water
                || mapCell.Kind == CityMapCellKind.Shore
                || mapCell.Kind == CityMapCellKind.Forest)
            {
                return false;
            }

            return map.IsCellWalkable(cell)
                && StrategyPointOfInterestController.Active?.HasPointAt(cell) != true;
        }

        private bool HasReachableStandCells(
            Vector2Int graveCell,
            HashSet<Vector2Int> reachableCells,
            int requiredStandCells)
        {
            if (reachableCells == null || reachableCells.Count <= 0 || requiredStandCells <= 0)
            {
                return true;
            }

            int count = 0;
            for (int i = 0; i < GraveStandOffsets.Length; i++)
            {
                Vector2Int standCell = graveCell + GraveStandOffsets[i];
                if (reachableCells.Contains(standCell) && map.IsCellWalkable(standCell))
                {
                    count++;
                    if (count >= requiredStandCells)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float GetCampDistance(Vector2Int cell, bool hasCampCell, Vector2Int campCell)
        {
            return hasCampCell ? Vector2Int.Distance(cell, campCell) : 0f;
        }

        private List<Vector2Int> CollectBuildingOrigins()
        {
            List<Vector2Int> origins = new();
            StrategyPlacedBuilding[] buildings = Object.FindObjectsByType<StrategyPlacedBuilding>();
            if (buildings == null)
            {
                return origins;
            }

            for (int i = 0; i < buildings.Length; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null)
                {
                    origins.Add(building.Origin);
                }
            }

            return origins;
        }

        private float GetNearestBuildingDistance(Vector2Int cell, IReadOnlyList<Vector2Int> buildingOrigins)
        {
            if (buildingOrigins == null || buildingOrigins.Count <= 0)
            {
                return map != null ? Mathf.Max(map.Width, map.Height) : 0f;
            }

            float best = float.MaxValue;
            for (int i = 0; i < buildingOrigins.Count; i++)
            {
                best = Mathf.Min(best, Vector2Int.Distance(cell, buildingOrigins[i]));
            }

            return best == float.MaxValue ? 0f : best;
        }

        private float GetTerrainScore(Vector2Int cell)
        {
            if (map == null || !map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell))
            {
                return 0f;
            }

            return mapCell.Kind switch
            {
                CityMapCellKind.Meadow => 5f,
                CityMapCellKind.Grass => 3f,
                CityMapCellKind.Dirt => 1f,
                _ => 0f
            };
        }

        private float GetMapEdgePenalty(Vector2Int cell)
        {
            int edgeDistance = GetMapEdgeDistance(cell);
            if (edgeDistance >= PreferredEdgePadding)
            {
                return 0f;
            }

            int deficit = PreferredEdgePadding - edgeDistance;
            return deficit * deficit * 7f;
        }

        private int GetMapEdgeDistance(Vector2Int cell)
        {
            if (map == null)
            {
                return 0;
            }

            return Mathf.Min(
                Mathf.Min(cell.x, cell.y),
                Mathf.Min(map.Width - 1 - cell.x, map.Height - 1 - cell.y));
        }

        private void EnsureGraveRoot()
        {
            if (graveRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Cemetery");
            root.transform.SetParent(transform, false);
            graveRoot = root.transform;
        }
    }
}
