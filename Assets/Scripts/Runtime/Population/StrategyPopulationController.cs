using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyPopulationController : MonoBehaviour
    {
        private const int InitialMaleResidents = 3;
        private const int InitialFemaleResidents = 3;
        private const int CampSpawnRadius = 3;

        private static readonly string[] MaleFirstNames =
        {
            "Alaric",
            "Aldric",
            "Asger",
            "Bjorn",
            "Dietrich",
            "Eirik",
            "Godric",
            "Gunther",
            "Hakon",
            "Harald",
            "Leif",
            "Oswin",
            "Ragnar",
            "Ragnvald",
            "Rorik",
            "Sigurd",
            "Soren",
            "Sten",
            "Torsten",
            "Wulfric"
        };

        private static readonly string[] FemaleFirstNames =
        {
            "Alfhild",
            "Astrid",
            "Brynhild",
            "Eira",
            "Elswyth",
            "Freydis",
            "Frida",
            "Gerda",
            "Gudrun",
            "Hilda",
            "Hilde",
            "Ingrid",
            "Maerwynn",
            "Runa",
            "Sigrid",
            "Signy",
            "Solveig",
            "Thyra",
            "Ylva",
            "Yrsa"
        };

        private static readonly string[] FamilyNames =
        {
            "Alderborn",
            "Ashenwald",
            "Bergson",
            "Eisental",
            "Falken",
            "Frosthelm",
            "Grimwald",
            "Hrafnsson",
            "Ironmark",
            "Nordheim",
            "Oakenspear",
            "Ravenshield",
            "Sablehorn",
            "Skaldsen",
            "Stonehall",
            "Stormgard",
            "Thornwick",
            "Wintermere",
            "Wolfhart",
            "Wulfbrand"
        };

        private readonly List<StrategyResidentAgent> residents = new();
        private CityMapController map;
        private Transform residentRoot;
        private Vector2Int campCell;
        private Vector3 campWorld;
        private bool hasStarterCamp;

        public IReadOnlyList<StrategyResidentAgent> Residents => residents;

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            EnsureResidentRoot();
            EnsureStarterCamp();
            StrategyDebugLogger.Info(
                "Population",
                "Configured",
                StrategyDebugLogger.F("residentCount", residents.Count),
                StrategyDebugLogger.F("hasStarterCamp", hasStarterCamp),
                StrategyDebugLogger.F("campCell", campCell));
        }

        public bool TryGetCampWorld(out Vector3 world)
        {
            world = campWorld;
            return hasStarterCamp;
        }

        public bool TryGetCampCell(out Vector2Int cell)
        {
            cell = campCell;
            return hasStarterCamp;
        }

        public bool AssignResidentsToHouse(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House || map == null)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "AssignHouseRejected",
                    StrategyDebugLogger.F("reason", "invalid_house"));
                return false;
            }

            if (!TryFindAvailableResident(StrategyResidentGender.Male, out StrategyResidentAgent male)
                || !TryFindAvailableResident(StrategyResidentGender.Female, out StrategyResidentAgent female))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "AssignHouseRejected",
                    StrategyDebugLogger.F("reason", "no_free_pair"),
                    StrategyDebugLogger.F("houseOrigin", house.Origin));
                return false;
            }

            HashSet<Vector2Int> usedCells = new();
            Vector3 maleWorld = GetHouseResidentTargetWorld(house, usedCells, 0);
            Vector3 femaleWorld = GetHouseResidentTargetWorld(house, usedCells, 1);

            male.AssignHome(house, maleWorld);
            female.AssignHome(house, femaleWorld);
            StrategyDebugLogger.Info(
                "Population",
                "HouseResidentsAssigned",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("male", male.FullName),
                StrategyDebugLogger.F("female", female.FullName),
                StrategyDebugLogger.F("maleTarget", maleWorld),
                StrategyDebugLogger.F("femaleTarget", femaleWorld));
            return true;
        }

        public bool TryAssignConstructionBuilders(StrategyConstructionSite site)
        {
            if (site == null || map == null)
            {
                return false;
            }

            if (site.Tool == StrategyBuildTool.House)
            {
                if (!TryFindAvailableResident(StrategyResidentGender.Male, out StrategyResidentAgent male)
                    || !TryFindAvailableResident(StrategyResidentGender.Female, out StrategyResidentAgent female))
                {
                    StrategyDebugLogger.Warn(
                        "Population",
                        "ConstructionBuildersRejected",
                        StrategyDebugLogger.F("tool", site.Tool),
                        StrategyDebugLogger.F("origin", site.Origin),
                        StrategyDebugLogger.F("reason", "no_free_house_pair"));
                    return false;
                }

                site.RegisterBuilder(male);
                site.RegisterBuilder(female);
                male.AssignConstructionSite(site, true);
                female.AssignConstructionSite(site, true);
                StrategyDebugLogger.Info(
                    "Population",
                    "ConstructionBuildersAssigned",
                    StrategyDebugLogger.F("tool", site.Tool),
                    StrategyDebugLogger.F("origin", site.Origin),
                    StrategyDebugLogger.F("builderA", male.FullName),
                    StrategyDebugLogger.F("builderB", female.FullName),
                    StrategyDebugLogger.F("futureHome", true));
                return true;
            }

            if (!TryFindAvailableConstructionWorkers(StrategyConstructionSite.MaxBuilders, out List<StrategyResidentAgent> builders))
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ConstructionBuildersRejected",
                    StrategyDebugLogger.F("tool", site.Tool),
                    StrategyDebugLogger.F("origin", site.Origin),
                    StrategyDebugLogger.F("reason", "no_free_workers"));
                return false;
            }

            for (int i = 0; i < builders.Count; i++)
            {
                site.RegisterBuilder(builders[i]);
                builders[i].AssignConstructionSite(site, false);
            }

            StrategyDebugLogger.Info(
                "Population",
                "ConstructionBuildersAssigned",
                StrategyDebugLogger.F("tool", site.Tool),
                StrategyDebugLogger.F("origin", site.Origin),
                StrategyDebugLogger.F("builderCount", builders.Count),
                StrategyDebugLogger.F("futureHome", false));
            return true;
        }

        public void CompleteHouseConstruction(StrategyConstructionSite site, StrategyPlacedBuilding house)
        {
            if (site == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            HashSet<Vector2Int> usedCells = new();
            int assignedCount = 0;
            IReadOnlyList<StrategyResidentAgent> builders = site.Builders;
            for (int i = 0; i < builders.Count; i++)
            {
                StrategyResidentAgent resident = builders[i];
                if (resident == null)
                {
                    continue;
                }

                Vector3 targetWorld = GetHouseResidentTargetWorld(house, usedCells, assignedCount);
                resident.AssignHome(house, targetWorld);
                assignedCount++;
            }

            StrategyDebugLogger.Info(
                "Population",
                "ConstructionHouseResidentsBound",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("residentCount", assignedCount));
        }

        private void EnsureStarterCamp()
        {
            if (hasStarterCamp || map == null)
            {
                return;
            }

            if (!TryFindCampCell(true, out campCell) && !TryFindCampCell(false, out campCell))
            {
                campCell = new Vector2Int(map.Width / 2, map.Height / 2);
            }

            campWorld = map.GetCellCenterWorld(campCell.x, campCell.y);
            CreateCampfire();
            SpawnInitialResidents();
            hasStarterCamp = true;
            StrategyDebugLogger.Info(
                "Population",
                "StarterCampCreated",
                StrategyDebugLogger.F("cell", campCell),
                StrategyDebugLogger.F("world", campWorld),
                StrategyDebugLogger.F("residents", residents.Count));
        }

        private void CreateCampfire()
        {
            GameObject campObject = new GameObject("Starter Campfire");
            campObject.transform.SetParent(residentRoot, false);
            campObject.transform.position = new Vector3(campWorld.x, campWorld.y, -0.12f);

            SpriteRenderer renderer = campObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyCampfireSpriteFactory.GetFrame(0);
            StrategyWorldSorting.Apply(renderer, campObject.transform.position);

            StrategyCampfireAnimator animator = campObject.AddComponent<StrategyCampfireAnimator>();
            animator.Configure(renderer);
        }

        private void SpawnInitialResidents()
        {
            HashSet<Vector2Int> usedCells = new();
            int spawnSlot = 0;
            for (int i = 0; i < InitialMaleResidents; i++)
            {
                SpawnCampResident(StrategyResidentGender.Male, spawnSlot, usedCells);
                spawnSlot++;
            }

            for (int i = 0; i < InitialFemaleResidents; i++)
            {
                SpawnCampResident(StrategyResidentGender.Female, spawnSlot, usedCells);
                spawnSlot++;
            }
        }

        private void SpawnCampResident(StrategyResidentGender gender, int spawnSlot, HashSet<Vector2Int> usedCells)
        {
            bool foundSpawnCell = TryFindCampSpawnCell(usedCells, spawnSlot, out Vector2Int spawnCell);
            Vector3 spawnWorld = foundSpawnCell
                ? map.GetCellCenterWorld(spawnCell.x, spawnCell.y)
                : GetFallbackCampSpawnWorld(spawnSlot);

            if (foundSpawnCell)
            {
                usedCells.Add(spawnCell);
            }

            int visualVariant = Random.Range(0, StrategyResidentSpriteFactory.VariantCountPerGender);
            string residentName = GenerateResidentName(gender);

            GameObject residentObject = new GameObject(residentName);
            residentObject.transform.SetParent(residentRoot, false);

            SpriteRenderer renderer = residentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyResidentSpriteFactory.GetSprite(gender, visualVariant);

            StrategyResidentAgent agent = residentObject.AddComponent<StrategyResidentAgent>();
            agent.Configure(
                map,
                null,
                gender,
                visualVariant,
                residentName,
                spawnWorld,
                renderer,
                campCell,
                Vector2Int.one);
            residents.Add(agent);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSpawned",
                StrategyDebugLogger.F("name", residentName),
                StrategyDebugLogger.F("gender", gender),
                StrategyDebugLogger.F("variant", visualVariant),
                StrategyDebugLogger.F("spawnCell", foundSpawnCell ? spawnCell : Vector2Int.zero),
                StrategyDebugLogger.F("spawnWorld", spawnWorld),
                StrategyDebugLogger.F("usedFallback", !foundSpawnCell));
        }

        private bool TryFindAvailableResident(StrategyResidentGender gender, out StrategyResidentAgent resident)
        {
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (candidate != null
                    && candidate.Gender == gender
                    && candidate.Home == null
                    && !candidate.HasWorkplace
                    && !candidate.HasConstructionAssignment)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count <= 0)
            {
                resident = null;
                return false;
            }

            resident = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        private bool TryFindAvailableConstructionWorkers(int count, out List<StrategyResidentAgent> builders)
        {
            builders = new List<StrategyResidentAgent>();
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (candidate != null
                    && !candidate.HasWorkplace
                    && !candidate.HasConstructionAssignment)
                {
                    candidates.Add(candidate);
                }
            }

            while (builders.Count < count && candidates.Count > 0)
            {
                int index = Random.Range(0, candidates.Count);
                builders.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return builders.Count >= count;
        }

        private bool TryFindCampCell(bool preferOpenLand, out Vector2Int cell)
        {
            Vector2Int center = new Vector2Int(map.Width / 2, map.Height / 2);
            List<Vector2Int> candidates = new();
            int maxRadius = Mathf.Max(map.Width, map.Height);

            for (int radius = 0; radius <= maxRadius; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (radius > 0 && Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = center + new Vector2Int(x, y);
                        if (IsCampCellCandidate(candidate, preferOpenLand))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[StableIndex(candidates.Count, radius + (preferOpenLand ? 11 : 23))];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool TryFindCampSpawnCell(HashSet<Vector2Int> usedCells, int spawnSlot, out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= CampSpawnRadius; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        {
                            continue;
                        }

                        Vector2Int candidate = campCell + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !usedCells.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[StableIndex(candidates.Count, spawnSlot + radius * 31)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private Vector3 GetHouseResidentTargetWorld(StrategyPlacedBuilding house, HashSet<Vector2Int> usedCells, int spawnSlot)
        {
            bool foundSpawnCell = TryFindHouseResidentSpawnCell(house, usedCells, spawnSlot, out Vector2Int spawnCell);
            if (foundSpawnCell)
            {
                usedCells.Add(spawnCell);
                return map.GetCellCenterWorld(spawnCell.x, spawnCell.y);
            }

            return GetFallbackHouseResidentSpawnWorld(house, spawnSlot);
        }

        private bool TryFindHouseResidentSpawnCell(
            StrategyPlacedBuilding house,
            HashSet<Vector2Int> usedCells,
            int spawnSlot,
            out Vector2Int cell)
        {
            List<Vector2Int> candidates = new();
            for (int radius = 1; radius <= 6; radius++)
            {
                candidates.Clear();
                for (int y = -radius; y < house.Footprint.y + radius; y++)
                {
                    for (int x = -radius; x < house.Footprint.x + radius; x++)
                    {
                        bool isEdge = x == -radius
                            || y == -radius
                            || x == house.Footprint.x + radius - 1
                            || y == house.Footprint.y + radius - 1;

                        if (!isEdge)
                        {
                            continue;
                        }

                        Vector2Int candidate = house.Origin + new Vector2Int(x, y);
                        if (map.IsCellWalkable(candidate) && !usedCells.Contains(candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates[StableIndex(candidates.Count, house.Origin.x * 31 + house.Origin.y * 17 + spawnSlot * 7 + radius)];
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool IsCampCellCandidate(Vector2Int cell, bool preferOpenLand)
        {
            if (!map.TryGetCell(cell.x, cell.y, out CityMapCell mapCell) || !map.IsCellWalkable(cell))
            {
                return false;
            }

            return !preferOpenLand
                || mapCell.Kind == CityMapCellKind.Grass
                || mapCell.Kind == CityMapCellKind.Meadow
                || mapCell.Kind == CityMapCellKind.Dirt;
        }

        private Vector3 GetFallbackCampSpawnWorld(int spawnSlot)
        {
            float angle = (Mathf.PI * 2f * spawnSlot / (InitialMaleResidents + InitialFemaleResidents)) + 0.35f;
            return new Vector3(
                campWorld.x + Mathf.Cos(angle) * map.CellSize * 1.55f,
                campWorld.y + Mathf.Sin(angle) * map.CellSize * 1.10f,
                -0.08f);
        }

        private Vector3 GetFallbackHouseResidentSpawnWorld(StrategyPlacedBuilding house, int spawnSlot)
        {
            Vector3 anchor = house.HomeAnchor;
            float side = spawnSlot == 0 ? -0.55f : 0.55f;
            return new Vector3(anchor.x + side * map.CellSize, anchor.y - map.CellSize * 0.75f, -0.08f);
        }

        private int StableIndex(int count, int salt)
        {
            if (count <= 1)
            {
                return 0;
            }

            uint hash = (uint)Mathf.Max(1, map != null ? map.ActiveSeed : 1);
            hash ^= (uint)salt * 2654435761u;
            hash ^= hash >> 16;
            return (int)(hash % (uint)count);
        }

        private static string GenerateResidentName(StrategyResidentGender gender)
        {
            string[] firstNames = gender == StrategyResidentGender.Male ? MaleFirstNames : FemaleFirstNames;
            return firstNames[Random.Range(0, firstNames.Length)]
                + " "
                + FamilyNames[Random.Range(0, FamilyNames.Length)];
        }

        private void EnsureResidentRoot()
        {
            if (residentRoot != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Residents");
            rootObject.transform.SetParent(transform, false);
            residentRoot = rootObject.transform;
        }
    }
}
