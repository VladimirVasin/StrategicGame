using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyResidentFamilyRecord
    {
        private readonly List<int> childIds = new();

        public int ResidentId { get; private set; }
        public StrategyResidentGender Gender { get; private set; }
        public int FatherId { get; private set; }
        public int MotherId { get; private set; }
        public string FamilyName { get; private set; }
        public bool IsAlive { get; private set; }
        public IReadOnlyList<int> ChildIds => childIds;

        public void Configure(StrategyResidentAgent resident, bool isAlive)
        {
            if (resident == null)
            {
                return;
            }

            ResidentId = resident.ResidentId;
            Gender = resident.Gender;
            FatherId = resident.FatherId;
            MotherId = resident.MotherId;
            FamilyName = resident.FamilyName;
            IsAlive = isAlive;
            childIds.Clear();

            IReadOnlyList<int> residentChildren = resident.ChildIds;
            for (int i = 0; i < residentChildren.Count; i++)
            {
                int childId = residentChildren[i];
                if (childId > 0 && !childIds.Contains(childId))
                {
                    childIds.Add(childId);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class StrategyPopulationController : MonoBehaviour
    {
        private const int InitialMaleResidents = 3;
        private const int InitialFemaleResidents = 3;
        private const int CampSpawnRadius = 3;
        private const float HouseholdMigrationCheckInterval = 4f;
        private const int MortalityStartAgeYears = 1;
        private const int MortalityAccelerationAgeYears = 40;
        private const int MortalityHighRiskAgeYears = 50;
        private const float MortalityChanceAtAgeOne = 0.0004f;
        private const float MortalityChanceAtAccelerationAge = 0.008f;
        private const float MortalityChanceAtAgeFifty = 0.30f;
        private const float MortalityChanceAfterFiftyPerYear = 0.025f;
        private const float MortalityMaxAnnualChance = 0.80f;
        private const float StarvationMortalityMaxAnnualChance = 0.95f;

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
        private readonly List<StrategyPlacedBuilding> houses = new();
        private readonly Dictionary<int, StrategyResidentAgent> residentsById = new();
        private readonly Dictionary<int, StrategyResidentFamilyRecord> familyRecordsById = new();
        private CityMapController map;
        private Transform residentRoot;
        private StrategyFuneralController funeralController;
        private Vector2Int campCell;
        private Vector3 campWorld;
        private float householdMigrationTimer;
        private int nextResidentId = 1;
        private bool hasStarterCamp;

        public IReadOnlyList<StrategyResidentAgent> Residents => residents;
        public int TotalResidentCount => CountResidents(false, false);
        public int AdultResidentCount => CountResidents(true, false);
        public int ChildResidentCount => CountResidents(false, true);
        public int CompletedHouseCount => CountRegisteredHouses();

        public void Configure(CityMapController mapController)
        {
            map = mapController;
            EnsureResidentRoot();
            EnsureFuneralController();
            EnsureStarterCamp();
            StrategyDebugLogger.Info(
                "Population",
                "Configured",
                StrategyDebugLogger.F("residentCount", residents.Count),
                StrategyDebugLogger.F("hasStarterCamp", hasStarterCamp),
                StrategyDebugLogger.F("campCell", campCell));
        }

        private void Update()
        {
            if (map == null || houses.Count <= 0)
            {
                return;
            }

            householdMigrationTimer -= Time.deltaTime;
            if (householdMigrationTimer > 0f)
            {
                return;
            }

            householdMigrationTimer = HouseholdMigrationCheckInterval;
            TryPopulateAvailableHouses();
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

        public bool TryGetResidentById(int residentId, out StrategyResidentAgent resident)
        {
            if (residentId > 0
                && residentsById.TryGetValue(residentId, out resident)
                && resident != null)
            {
                return true;
            }

            resident = null;
            return false;
        }

        internal bool TryGetFamilyRecord(int residentId, out StrategyResidentFamilyRecord record)
        {
            record = null;
            return residentId > 0
                && familyRecordsById.TryGetValue(residentId, out record)
                && record != null;
        }

        public bool TryResolveAnnualMortality(StrategyResidentAgent resident, int ageYears)
        {
            if (resident == null
                || resident.IsPendingRefugee
                || !residents.Contains(resident))
            {
                return false;
            }

            float baseAnnualChance = GetAnnualDeathChance(ageYears);
            float starvationMultiplier = GetStarvationMortalityMultiplier(
                resident,
                out int starvationLevel,
                out Vector2Int starvingHouseOrigin);
            float annualChance = Mathf.Min(
                StarvationMortalityMaxAnnualChance,
                baseAnnualChance * starvationMultiplier);
            if (annualChance <= 0f || Random.value >= annualChance)
            {
                return false;
            }

            string reason = starvationMultiplier > 1f ? "starvation_mortality" : "annual_mortality";
            return HandleResidentDeath(
                resident,
                reason,
                annualChance,
                baseAnnualChance,
                starvationMultiplier,
                starvationLevel,
                starvingHouseOrigin);
        }

        private bool HandleResidentDeath(
            StrategyResidentAgent resident,
            string reason,
            float annualChance,
            float baseAnnualChance = 0f,
            float starvationMultiplier = 1f,
            int starvationLevel = 0,
            Vector2Int starvingHouseOrigin = default)
        {
            if (resident == null || resident.IsPendingRefugee || !residents.Contains(resident))
            {
                return false;
            }

            int residentId = resident.ResidentId;
            string residentName = resident.FullName;
            int age = resident.DisplayAgeYears;
            StrategyPlacedBuilding home = resident.Home;
            Vector2Int homeOrigin = home != null ? home.Origin : Vector2Int.zero;
            StrategyResidentDeathSnapshot snapshot = CreateDeathSnapshot(resident, home);

            UpsertFamilyRecord(resident, false);
            RemoveResidentFromAssignments(resident);
            home?.UnregisterResident(resident);
            resident.PrepareForDeath();
            residents.Remove(resident);
            if (residentId > 0)
            {
                residentsById.Remove(residentId);
            }

            ClearSelectionForResident(resident);
            EnsureFuneralController();
            funeralController?.NotifyResidentDeath(snapshot);
            Destroy(resident.gameObject);
            householdMigrationTimer = 0f;

            StrategyDebugLogger.Info(
                "Population",
                "ResidentDied",
                StrategyDebugLogger.F("resident", residentName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", age),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("annualChance", annualChance),
                StrategyDebugLogger.F("baseAnnualChance", baseAnnualChance),
                StrategyDebugLogger.F("starvationMultiplier", starvationMultiplier),
                StrategyDebugLogger.F("starvationLevel", starvationLevel),
                StrategyDebugLogger.F("starvingHouseOrigin", starvingHouseOrigin),
                StrategyDebugLogger.F("homeOrigin", homeOrigin),
                StrategyDebugLogger.F("totalResidents", TotalResidentCount),
                StrategyDebugLogger.F("adults", AdultResidentCount),
                StrategyDebugLogger.F("children", ChildResidentCount));
            return true;
        }

        public void RegisterHouse(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            if (!houses.Contains(house))
            {
                houses.Add(house);
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseRegistered",
                    StrategyDebugLogger.F("houseOrigin", house.Origin),
                    StrategyDebugLogger.F("capacity", house.ResidentCapacity));
            }

            ConfigureHousehold(house);
        }

        public void UnregisterHouse(StrategyPlacedBuilding house)
        {
            if (house == null)
            {
                return;
            }

            if (houses.Remove(house))
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "HouseUnregistered",
                    StrategyDebugLogger.F("houseOrigin", house.Origin));
            }
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

            RegisterHouse(house);

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
            ConfigureHousehold(house);
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
            if (site == null)
            {
                return false;
            }

            bool assigned = StrategyStorageYard.TryAssignBuildersToSite(site);
            if (!assigned)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ConstructionBuildersRejected",
                    StrategyDebugLogger.F("tool", site.Tool),
                    StrategyDebugLogger.F("origin", site.Origin),
                    StrategyDebugLogger.F("reason", "no_hired_storage_builders"));
                return false;
            }

            StrategyDebugLogger.Info(
                "Population",
                "ConstructionBuildersAssigned",
                StrategyDebugLogger.F("tool", site.Tool),
                StrategyDebugLogger.F("origin", site.Origin),
                StrategyDebugLogger.F("builderCount", site.BuilderCount),
                StrategyDebugLogger.F("futureHome", false));
            return true;
        }

        public void CompleteHouseConstruction(StrategyConstructionSite site, StrategyPlacedBuilding house)
        {
            if (site == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            RegisterHouse(house);

            bool assignedFamily = TryPopulateHomelessFamilyHouse(house);
            bool assignedPair = false;
            if (!assignedFamily)
            {
                assignedPair = AssignResidentsToHouse(house);
            }

            if (!assignedFamily && !assignedPair)
            {
                TryPopulateFreeHouse(house);
            }

            StrategyDebugLogger.Info(
                "Population",
                "ConstructionHouseResidentsBound",
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("assignedFamily", assignedFamily),
                StrategyDebugLogger.F("assignedPair", assignedPair),
                StrategyDebugLogger.F("residentCount", house.ResidentCount));
        }

        public bool TrySpawnChildForHouse(
            StrategyPlacedBuilding house,
            StrategyResidentAgent father,
            StrategyResidentAgent mother,
            out StrategyResidentAgent child)
        {
            child = null;
            if (map == null
                || house == null
                || house.Tool != StrategyBuildTool.House
                || !house.HasFreeResidentSlot
                || !StrategyKinshipUtility.CanFormCouple(father, mother, this))
            {
                return false;
            }

            HashSet<Vector2Int> usedCells = new();
            IReadOnlyList<StrategyResidentAgent> houseResidents = house.Residents;
            for (int i = 0; i < houseResidents.Count; i++)
            {
                StrategyResidentAgent resident = houseResidents[i];
                if (resident != null && map.TryWorldToCell(resident.transform.position, out Vector2Int cell))
                {
                    usedCells.Add(cell);
                }
            }

            int childId = AllocateResidentId();
            StrategyResidentGender gender = Random.value < 0.5f
                ? StrategyResidentGender.Male
                : StrategyResidentGender.Female;
            int visualVariant = Random.Range(0, StrategyResidentSpriteFactory.VariantCountPerGender);
            string familyName = !string.IsNullOrWhiteSpace(father.FamilyName)
                ? father.FamilyName
                : mother.FamilyName;
            string childName = GenerateResidentName(gender, familyName);
            Vector3 spawnWorld = GetHouseResidentTargetWorld(house, usedCells, houseResidents.Count);

            GameObject residentObject = new GameObject(childName);
            residentObject.transform.SetParent(residentRoot, false);

            SpriteRenderer renderer = residentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyResidentSpriteFactory.GetSprite(gender, visualVariant, StrategyResidentLifeStage.Child);

            child = residentObject.AddComponent<StrategyResidentAgent>();
            child.Configure(
                map,
                house,
                gender,
                visualVariant,
                childName,
                spawnWorld,
                renderer,
                house.Origin,
                house.Footprint,
                childId,
                0f,
                StrategyResidentLifeStage.Child,
                father.ResidentId,
                mother.ResidentId,
                familyName);

            father.AddChildId(childId);
            mother.AddChildId(childId);
            UpsertFamilyRecord(father, true);
            UpsertFamilyRecord(mother, true);
            RegisterResident(child);

            StrategyDebugLogger.Info(
                "Population",
                "ChildSpawned",
                StrategyDebugLogger.F("name", childName),
                StrategyDebugLogger.F("residentId", childId),
                StrategyDebugLogger.F("gender", gender),
                StrategyDebugLogger.F("variant", visualVariant),
                StrategyDebugLogger.F("fatherId", father.ResidentId),
                StrategyDebugLogger.F("motherId", mother.ResidentId),
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("spawnWorld", spawnWorld));
            return true;
        }

        public bool TryCreateRefugeeFamily(
            Vector3 spawnWorld,
            Vector2 formationAxis,
            Vector2Int temporaryIdleOrigin,
            int childCount,
            out List<StrategyResidentAgent> family)
        {
            family = new List<StrategyResidentAgent>();
            if (map == null)
            {
                return false;
            }

            EnsureResidentRoot();

            int normalizedChildCount = Mathf.Clamp(childCount, 1, 3);
            Vector2 axis = formationAxis.sqrMagnitude > 0.001f
                ? formationAxis.normalized
                : Vector2.right;
            string familyName = GetRandomFamilyName();
            int fatherId = AllocateResidentId();
            int motherId = AllocateResidentId();

            StrategyResidentAgent father = CreateRefugeeResident(
                StrategyResidentGender.Male,
                fatherId,
                0,
                0,
                familyName,
                Random.Range(24f, 42f),
                StrategyResidentLifeStage.Adult,
                spawnWorld + GetRefugeeFormationOffset(axis, 0) * map.CellSize,
                temporaryIdleOrigin);

            StrategyResidentAgent mother = CreateRefugeeResident(
                StrategyResidentGender.Female,
                motherId,
                0,
                0,
                familyName,
                Random.Range(22f, 39f),
                StrategyResidentLifeStage.Adult,
                spawnWorld + GetRefugeeFormationOffset(axis, 1) * map.CellSize,
                temporaryIdleOrigin);

            if (father == null || mother == null)
            {
                if (father != null)
                {
                    Destroy(father.gameObject);
                }

                if (mother != null)
                {
                    Destroy(mother.gameObject);
                }

                DestroyTemporaryResidents(family);
                return false;
            }

            family.Add(father);
            family.Add(mother);

            for (int i = 0; i < normalizedChildCount; i++)
            {
                StrategyResidentGender gender = Random.value < 0.5f
                    ? StrategyResidentGender.Male
                    : StrategyResidentGender.Female;
                int childId = AllocateResidentId();
                StrategyResidentAgent child = CreateRefugeeResident(
                    gender,
                    childId,
                    fatherId,
                    motherId,
                    familyName,
                    Random.Range(2f, 15f),
                    StrategyResidentLifeStage.Child,
                    spawnWorld + GetRefugeeFormationOffset(axis, i + 2) * map.CellSize,
                    temporaryIdleOrigin);

                if (child == null)
                {
                    continue;
                }

                father.AddChildId(childId);
                mother.AddChildId(childId);
                family.Add(child);
            }

            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyCreated",
                StrategyDebugLogger.F("family", familyName),
                StrategyDebugLogger.F("members", family.Count),
                StrategyDebugLogger.F("children", family.Count - 2),
                StrategyDebugLogger.F("spawnWorld", spawnWorld));
            return family.Count >= 3;
        }

        public void AcceptRefugeeFamily(IReadOnlyList<StrategyResidentAgent> family)
        {
            if (family == null)
            {
                return;
            }

            int accepted = 0;
            List<StrategyResidentAgent> acceptedFamily = new();
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent resident = family[i];
                if (resident == null)
                {
                    continue;
                }

                resident.SetPendingRefugee(false);
                resident.SetCampIdleOrigin(campCell);
                RegisterResident(resident);
                acceptedFamily.Add(resident);
                accepted++;
            }

            bool housedFamily = TryPlaceAcceptedRefugeeFamily(acceptedFamily);
            if (!housedFamily)
            {
                TryPopulateAvailableHouses();
            }

            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyAccepted",
                StrategyDebugLogger.F("accepted", accepted),
                StrategyDebugLogger.F("housedFamily", housedFamily),
                StrategyDebugLogger.F("totalResidents", TotalResidentCount),
                StrategyDebugLogger.F("adults", AdultResidentCount),
                StrategyDebugLogger.F("children", ChildResidentCount));
        }

        public void DestroyTemporaryResidents(IReadOnlyList<StrategyResidentAgent> temporaryResidents)
        {
            if (temporaryResidents == null)
            {
                return;
            }

            for (int i = 0; i < temporaryResidents.Count; i++)
            {
                StrategyResidentAgent resident = temporaryResidents[i];
                if (resident != null)
                {
                    Destroy(resident.gameObject);
                }
            }
        }

        public bool TryPopulateFreeHouse(StrategyPlacedBuilding house)
        {
            if (map == null || house == null || house.Tool != StrategyBuildTool.House)
            {
                return false;
            }

            RegisterHouse(house);

            if (house.ResidentCount <= 0)
            {
                if (IsHouseBlockedByFoodShortage(house))
                {
                    LogHouseMoveBlockedByFood(house, "empty_house_starving");
                    return false;
                }

                if (!TryFindEldestAdultChildLivingWithParents(house, out StrategyResidentAgent resident))
                {
                    return false;
                }

                StrategyPlacedBuilding previousHome = resident.Home;
                if (!MoveResidentToHouse(resident, house))
                {
                    return false;
                }

                StrategyDebugLogger.Info(
                    "Population",
                    "AdultChildMovedToFreeHouse",
                    StrategyDebugLogger.F("resident", resident.FullName),
                    StrategyDebugLogger.F("residentId", resident.ResidentId),
                    StrategyDebugLogger.F("age", resident.DisplayAgeYears),
                    StrategyDebugLogger.F("fromHome", previousHome != null ? previousHome.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("toHome", house.Origin));

                TryPopulateSingleResidentHouse(house);
                return true;
            }

            if (house.ResidentCount == 1)
            {
                return TryPopulateSingleResidentHouse(house);
            }

            return false;
        }

        private bool TryPopulateAvailableHouses()
        {
            RemoveMissingHouses();

            bool changed = false;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.Tool == StrategyBuildTool.House && house.ResidentCount <= 0)
                {
                    changed |= TryPopulateHomelessFamilyHouse(house) || TryPopulateFreeHouse(house);
                }
            }

            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.Tool == StrategyBuildTool.House && house.ResidentCount == 1)
                {
                    changed |= TryPopulateSingleResidentHouse(house);
                }
            }

            return changed;
        }

        private bool TryPlaceAcceptedRefugeeFamily(IReadOnlyList<StrategyResidentAgent> family)
        {
            if (family == null || family.Count <= 0)
            {
                return false;
            }

            RemoveMissingHouses();
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house == null
                    || house.Tool != StrategyBuildTool.House
                    || house.ResidentCount > 0
                    || IsHouseBlockedByFoodShortage(house)
                    || family.Count > house.ResidentCapacity)
                {
                    continue;
                }

                if (MoveFamilyToHouse(family, house, "RefugeeFamilyHoused"))
                {
                    return true;
                }
            }

            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyAcceptedWithoutHouse",
                StrategyDebugLogger.F("members", family.Count),
                StrategyDebugLogger.F("reason", "no_empty_house"));
            return false;
        }

        private bool TryPopulateHomelessFamilyHouse(StrategyPlacedBuilding house)
        {
            if (map == null
                || house == null
                || house.Tool != StrategyBuildTool.House
                || house.ResidentCount > 0
                || IsHouseBlockedByFoodShortage(house)
                || !TryFindHomelessFamilyForHouse(house, out List<StrategyResidentAgent> family))
            {
                return false;
            }

            return MoveFamilyToHouse(family, house, "HomelessFamilyMovedToHouse");
        }

        private bool TryFindHomelessFamilyForHouse(
            StrategyPlacedBuilding destinationHouse,
            out List<StrategyResidentAgent> family)
        {
            family = null;
            if (destinationHouse == null || destinationHouse.Tool != StrategyBuildTool.House)
            {
                return false;
            }

            HashSet<int> checkedCouples = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent child = residents[i];
                if (child == null
                    || child.IsPendingRefugee
                    || child.Home != null
                    || child.FatherId <= 0
                    || child.MotherId <= 0)
                {
                    continue;
                }

                int coupleKey = child.FatherId * 397 ^ child.MotherId;
                if (checkedCouples.Contains(coupleKey))
                {
                    continue;
                }

                checkedCouples.Add(coupleKey);
                if (!TryGetResidentById(child.FatherId, out StrategyResidentAgent father)
                    || !TryGetResidentById(child.MotherId, out StrategyResidentAgent mother)
                    || !CanMoveResidentToHouse(father, destinationHouse)
                    || !CanMoveResidentToHouse(mother, destinationHouse)
                    || father.Gender != StrategyResidentGender.Male
                    || mother.Gender != StrategyResidentGender.Female)
                {
                    continue;
                }

                List<StrategyResidentAgent> candidateFamily = new() { father, mother };
                for (int memberIndex = 0; memberIndex < residents.Count; memberIndex++)
                {
                    StrategyResidentAgent candidate = residents[memberIndex];
                    if (candidate == null
                        || candidate == father
                        || candidate == mother
                        || candidate.IsPendingRefugee
                        || candidate.Home != null
                        || candidate.FatherId != father.ResidentId
                        || candidate.MotherId != mother.ResidentId)
                    {
                        continue;
                    }

                    candidateFamily.Add(candidate);
                }

                if (candidateFamily.Count >= 3 && candidateFamily.Count <= destinationHouse.ResidentCapacity)
                {
                    family = candidateFamily;
                    return true;
                }
            }

            return false;
        }

        private bool MoveFamilyToHouse(
            IReadOnlyList<StrategyResidentAgent> family,
            StrategyPlacedBuilding house,
            string logEvent)
        {
            if (map == null
                || family == null
                || family.Count <= 0
                || house == null
                || house.Tool != StrategyBuildTool.House
                || house.ResidentCount > 0
                || family.Count > house.ResidentCapacity)
            {
                return false;
            }

            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent resident = family[i];
                if (resident == null || resident.IsPendingRefugee || resident.Home != null)
                {
                    return false;
                }
            }

            HashSet<Vector2Int> usedCells = new();
            for (int i = 0; i < family.Count; i++)
            {
                StrategyResidentAgent resident = family[i];
                Vector3 targetWorld = GetHouseResidentTargetWorld(house, usedCells, i);
                resident.AssignHome(house, targetWorld);
                if (resident.Home != house)
                {
                    return false;
                }
            }

            ConfigureHousehold(house);
            StrategyDebugLogger.Info(
                "Population",
                logEvent,
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("members", family.Count),
                StrategyDebugLogger.F("family", family[0] != null ? family[0].FamilyName : string.Empty));
            return true;
        }

        private bool TryPopulateSingleResidentHouse(StrategyPlacedBuilding house)
        {
            if (map == null
                || house == null
                || house.Tool != StrategyBuildTool.House
                || house.ResidentCount != 1
                || !house.HasFreeResidentSlot)
            {
                return false;
            }

            if (IsHouseBlockedByFoodShortage(house))
            {
                LogHouseMoveBlockedByFood(house, "single_house_starving");
                return false;
            }

            StrategyResidentAgent resident = house.Residents.Count > 0 ? house.Residents[0] : null;
            if (resident == null || !resident.IsAdult || resident.Home != house)
            {
                return false;
            }

            if (!TryFindPartnerForResident(resident, house, out StrategyResidentAgent partner))
            {
                return false;
            }

            StrategyPlacedBuilding previousHome = partner.Home;
            if (!MoveResidentToHouse(partner, house))
            {
                return false;
            }

            StrategyDebugLogger.Info(
                "Population",
                "ResidentMovedAsPartner",
                StrategyDebugLogger.F("resident", partner.FullName),
                StrategyDebugLogger.F("residentId", partner.ResidentId),
                StrategyDebugLogger.F("partner", resident.FullName),
                StrategyDebugLogger.F("partnerId", resident.ResidentId),
                StrategyDebugLogger.F("fromHome", previousHome != null ? previousHome.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("toHome", house.Origin),
                StrategyDebugLogger.F(
                    "kinshipDegree",
                    StrategyKinshipUtility.GetKinshipDegree(resident, partner, this, StrategyKinshipUtility.CloseRelativeDegree)));
            return true;
        }

        private bool TryFindEldestAdultChildLivingWithParents(
            StrategyPlacedBuilding destinationHouse,
            out StrategyResidentAgent resident)
        {
            resident = null;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (!CanMoveResidentToHouse(candidate, destinationHouse)
                    || !IsAdultChildLivingWithParent(candidate))
                {
                    continue;
                }

                if (resident == null
                    || candidate.AgeYears > resident.AgeYears + 0.01f
                    || (Mathf.Abs(candidate.AgeYears - resident.AgeYears) <= 0.01f
                        && candidate.ResidentId < resident.ResidentId))
                {
                    resident = candidate;
                }
            }

            return resident != null;
        }

        private bool TryFindPartnerForResident(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding destinationHouse,
            out StrategyResidentAgent partner)
        {
            partner = null;
            int bestPriority = int.MaxValue;
            StrategyResidentGender requiredGender = GetOppositeGender(resident.Gender);

            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (!CanMoveResidentToHouse(candidate, destinationHouse)
                    || candidate.Gender != requiredGender
                    || !StrategyKinshipUtility.CanFormCouple(resident, candidate, this))
                {
                    continue;
                }

                int priority = IsAdultChildLivingWithParent(candidate)
                    ? 0
                    : candidate.Home == null
                        ? 1
                        : int.MaxValue;
                if (priority == int.MaxValue)
                {
                    continue;
                }

                if (partner == null
                    || priority < bestPriority
                    || (priority == bestPriority && candidate.AgeYears > partner.AgeYears + 0.01f)
                    || (priority == bestPriority
                        && Mathf.Abs(candidate.AgeYears - partner.AgeYears) <= 0.01f
                        && candidate.ResidentId < partner.ResidentId))
                {
                    partner = candidate;
                    bestPriority = priority;
                }
            }

            return partner != null;
        }

        private bool MoveResidentToHouse(StrategyResidentAgent resident, StrategyPlacedBuilding house)
        {
            if (resident == null || house == null || house.Tool != StrategyBuildTool.House || !house.CanAcceptResident(resident))
            {
                return false;
            }

            StrategyPlacedBuilding previousHome = resident.Home;
            HashSet<Vector2Int> usedCells = new();
            IReadOnlyList<StrategyResidentAgent> houseResidents = house.Residents;
            for (int i = 0; i < houseResidents.Count; i++)
            {
                StrategyResidentAgent current = houseResidents[i];
                if (current != null && map.TryWorldToCell(current.transform.position, out Vector2Int cell))
                {
                    usedCells.Add(cell);
                }
            }

            Vector3 targetWorld = GetHouseResidentTargetWorld(house, usedCells, houseResidents.Count);
            resident.AssignHome(house, targetWorld);
            bool moved = resident.Home == house;
            if (moved)
            {
                ConfigureHousehold(house);
                if (previousHome != null && previousHome != house)
                {
                    ConfigureHousehold(previousHome);
                }
            }

            return moved;
        }

        private static bool IsHouseBlockedByFoodShortage(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House)
            {
                return false;
            }

            StrategyHouseholdFoodState food = house.GetComponent<StrategyHouseholdFoodState>();
            return food != null && food.IsStarving;
        }

        private static void LogHouseMoveBlockedByFood(StrategyPlacedBuilding house, string reason)
        {
            StrategyHouseholdFoodState food = house != null ? house.GetComponent<StrategyHouseholdFoodState>() : null;
            StrategyDebugLogger.Info(
                "Population",
                "HouseholdMoveBlockedFoodShortage",
                StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("starvation", food != null ? food.StarvationLevel : 0),
                StrategyDebugLogger.F("requiredFood", food != null ? food.LastRequiredFood : 0),
                StrategyDebugLogger.F("lastConsumedFood", food != null ? food.LastConsumedFood : 0));
        }

        private bool CanMoveResidentToHouse(StrategyResidentAgent resident, StrategyPlacedBuilding destinationHouse)
        {
            return resident != null
                && destinationHouse != null
                && resident.IsAdult
                && resident.Home != destinationHouse
                && destinationHouse.HasFreeResidentSlot;
        }

        private bool IsAdultChildLivingWithParent(StrategyResidentAgent resident)
        {
            if (resident == null
                || !resident.IsAdult
                || resident.Home == null
                || (resident.FatherId <= 0 && resident.MotherId <= 0))
            {
                return false;
            }

            return HouseContainsResidentId(resident.Home, resident.FatherId)
                || HouseContainsResidentId(resident.Home, resident.MotherId);
        }

        private static bool HouseContainsResidentId(StrategyPlacedBuilding house, int residentId)
        {
            if (house == null || residentId <= 0)
            {
                return false;
            }

            IReadOnlyList<StrategyResidentAgent> residentsInHouse = house.Residents;
            for (int i = 0; i < residentsInHouse.Count; i++)
            {
                StrategyResidentAgent resident = residentsInHouse[i];
                if (resident != null && resident.ResidentId == residentId)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveMissingHouses()
        {
            for (int i = houses.Count - 1; i >= 0; i--)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house == null || house.Tool != StrategyBuildTool.House)
                {
                    houses.RemoveAt(i);
                }
            }
        }

        private static StrategyResidentGender GetOppositeGender(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male
                ? StrategyResidentGender.Female
                : StrategyResidentGender.Male;
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
            animator.Configure(renderer, map, campCell);
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
            string familyName = GetRandomFamilyName();
            string residentName = GenerateResidentName(gender, familyName);
            int residentId = AllocateResidentId();
            float age = Random.Range(18f, 31f);

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
                Vector2Int.one,
                residentId,
                age,
                StrategyResidentLifeStage.Adult,
                0,
                0,
                familyName);
            RegisterResident(agent);
            StrategyDebugLogger.Info(
                "Population",
                "ResidentSpawned",
                StrategyDebugLogger.F("name", residentName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("gender", gender),
                StrategyDebugLogger.F("variant", visualVariant),
                StrategyDebugLogger.F("age", agent.DisplayAgeYears),
                StrategyDebugLogger.F("spawnCell", foundSpawnCell ? spawnCell : Vector2Int.zero),
                StrategyDebugLogger.F("spawnWorld", spawnWorld),
                StrategyDebugLogger.F("usedFallback", !foundSpawnCell));
        }

        private StrategyResidentAgent CreateRefugeeResident(
            StrategyResidentGender gender,
            int residentId,
            int fatherIdentifier,
            int motherIdentifier,
            string familyName,
            float age,
            StrategyResidentLifeStage lifeStage,
            Vector3 spawnWorld,
            Vector2Int temporaryIdleOrigin)
        {
            int visualVariant = Random.Range(0, StrategyResidentSpriteFactory.VariantCountPerGender);
            string residentName = GenerateResidentName(gender, familyName);

            GameObject residentObject = new GameObject(residentName);
            residentObject.transform.SetParent(residentRoot, false);

            SpriteRenderer renderer = residentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyResidentSpriteFactory.GetSprite(gender, visualVariant, lifeStage);

            StrategyResidentAgent agent = residentObject.AddComponent<StrategyResidentAgent>();
            agent.Configure(
                map,
                null,
                gender,
                visualVariant,
                residentName,
                spawnWorld,
                renderer,
                temporaryIdleOrigin,
                Vector2Int.one,
                residentId,
                age,
                lifeStage,
                fatherIdentifier,
                motherIdentifier,
                familyName);
            agent.SetPendingRefugee(true);
            return agent;
        }

        private bool TryFindAvailableResident(StrategyResidentGender gender, out StrategyResidentAgent resident)
        {
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (candidate != null
                    && candidate.Gender == gender
                    && candidate.CanWork
                    && candidate.Home == null)
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
                    && candidate.CanWork
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
            Vector2[] offsets =
            {
                new Vector2(-0.55f, -0.75f),
                new Vector2(0.55f, -0.75f),
                new Vector2(-0.25f, -1.10f),
                new Vector2(0.25f, -1.10f),
                new Vector2(0f, -1.42f)
            };
            Vector2 offset = offsets[spawnSlot % offsets.Length];
            return new Vector3(
                anchor.x + offset.x * map.CellSize,
                anchor.y + offset.y * map.CellSize,
                -0.08f);
        }

        private void ConfigureHousehold(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House)
            {
                return;
            }

            StrategyHouseholdState household = house.GetComponent<StrategyHouseholdState>();
            if (household == null)
            {
                household = house.gameObject.AddComponent<StrategyHouseholdState>();
            }

            household.Configure(this, house);

            StrategyHouseholdFoodState food = house.GetComponent<StrategyHouseholdFoodState>();
            if (food == null)
            {
                food = house.gameObject.AddComponent<StrategyHouseholdFoodState>();
            }

            food.Configure(this, house);
        }

        private static float GetStarvationMortalityMultiplier(
            StrategyResidentAgent resident,
            out int starvationLevel,
            out Vector2Int houseOrigin)
        {
            starvationLevel = 0;
            houseOrigin = Vector2Int.zero;
            StrategyPlacedBuilding home = resident != null ? resident.Home : null;
            if (home == null || home.Tool != StrategyBuildTool.House)
            {
                return 1f;
            }

            houseOrigin = home.Origin;
            StrategyHouseholdFoodState food = home.GetComponent<StrategyHouseholdFoodState>();
            if (food == null)
            {
                return 1f;
            }

            starvationLevel = food.StarvationLevel;
            return food.MortalityMultiplier;
        }

        private int CountResidents(bool adultsOnly, bool childrenOnly)
        {
            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null || resident.IsPendingRefugee)
                {
                    continue;
                }

                if (adultsOnly && !resident.IsAdult)
                {
                    continue;
                }

                if (childrenOnly && resident.IsAdult)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private int CountRegisteredHouses()
        {
            int count = 0;
            for (int i = 0; i < houses.Count; i++)
            {
                StrategyPlacedBuilding house = houses[i];
                if (house != null && house.Tool == StrategyBuildTool.House)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector3 GetRefugeeFormationOffset(Vector2 axis, int index)
        {
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
            Vector2 offset = axis * back + perpendicular * side;
            return new Vector3(offset.x, offset.y, 0f);
        }

        private int AllocateResidentId()
        {
            return nextResidentId++;
        }

        internal List<StrategyResidentAgent> CollectFuneralParticipants(
            StrategyResidentDeathSnapshot snapshot,
            int maxCount)
        {
            List<StrategyResidentAgent> participants = new();
            int limit = Mathf.Max(1, maxCount);

            AddFuneralParticipantIds(participants, snapshot.HouseholdResidentIds, limit);
            AddFuneralParticipantById(participants, snapshot.FatherId, limit);
            AddFuneralParticipantById(participants, snapshot.MotherId, limit);
            AddFuneralParticipantIds(participants, snapshot.ChildIds, limit);

            for (int i = 0; i < residents.Count && participants.Count < limit; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || resident.IsPendingRefugee
                    || resident.ResidentId == snapshot.ResidentId
                    || participants.Contains(resident))
                {
                    continue;
                }

                if (IsCloseFuneralRelative(resident, snapshot))
                {
                    participants.Add(resident);
                }
            }

            return participants;
        }

        private StrategyResidentDeathSnapshot CreateDeathSnapshot(
            StrategyResidentAgent resident,
            StrategyPlacedBuilding home)
        {
            Vector2Int deathCell = Vector2Int.zero;
            if (map != null)
            {
                map.TryWorldToCell(resident.transform.position, out deathCell);
            }

            List<int> householdIds = new();
            if (home != null)
            {
                IReadOnlyList<StrategyResidentAgent> homeResidents = home.Residents;
                for (int i = 0; i < homeResidents.Count; i++)
                {
                    StrategyResidentAgent homeResident = homeResidents[i];
                    if (homeResident != null
                        && homeResident != resident
                        && homeResident.ResidentId > 0
                        && !householdIds.Contains(homeResident.ResidentId))
                    {
                        householdIds.Add(homeResident.ResidentId);
                    }
                }
            }

            List<int> childIds = new();
            IReadOnlyList<int> residentChildren = resident.ChildIds;
            for (int i = 0; i < residentChildren.Count; i++)
            {
                int childId = residentChildren[i];
                if (childId > 0 && !childIds.Contains(childId))
                {
                    childIds.Add(childId);
                }
            }

            return new StrategyResidentDeathSnapshot(
                resident.ResidentId,
                resident.FullName,
                resident.Gender,
                resident.LifeStage,
                resident.VisualVariant,
                resident.DisplayAgeYears,
                resident.FatherId,
                resident.MotherId,
                resident.FamilyName,
                resident.transform.position,
                deathCell,
                home != null ? home.Origin : Vector2Int.zero,
                householdIds.ToArray(),
                childIds.ToArray());
        }

        private void AddFuneralParticipantIds(
            List<StrategyResidentAgent> participants,
            IReadOnlyList<int> residentIds,
            int limit)
        {
            if (residentIds == null)
            {
                return;
            }

            for (int i = 0; i < residentIds.Count && participants.Count < limit; i++)
            {
                AddFuneralParticipantById(participants, residentIds[i], limit);
            }
        }

        private void AddFuneralParticipantById(
            List<StrategyResidentAgent> participants,
            int residentId,
            int limit)
        {
            if (participants.Count >= limit
                || residentId <= 0
                || !TryGetResidentById(residentId, out StrategyResidentAgent resident)
                || resident == null
                || resident.IsPendingRefugee
                || participants.Contains(resident))
            {
                return;
            }

            participants.Add(resident);
        }

        private bool IsCloseFuneralRelative(
            StrategyResidentAgent resident,
            StrategyResidentDeathSnapshot snapshot)
        {
            if (resident.FatherId == snapshot.ResidentId
                || resident.MotherId == snapshot.ResidentId
                || resident.ResidentId == snapshot.FatherId
                || resident.ResidentId == snapshot.MotherId)
            {
                return true;
            }

            if (snapshot.FatherId > 0
                && resident.FatherId == snapshot.FatherId)
            {
                return true;
            }

            if (snapshot.MotherId > 0
                && resident.MotherId == snapshot.MotherId)
            {
                return true;
            }

            IReadOnlyList<int> childIds = resident.ChildIds;
            for (int i = 0; i < childIds.Count; i++)
            {
                if (childIds[i] == snapshot.ResidentId)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetAnnualDeathChance(int ageYears)
        {
            if (ageYears < MortalityStartAgeYears)
            {
                return 0f;
            }

            if (ageYears <= MortalityAccelerationAgeYears)
            {
                float t = Mathf.InverseLerp(
                    MortalityStartAgeYears,
                    MortalityAccelerationAgeYears,
                    ageYears);
                return Mathf.Lerp(MortalityChanceAtAgeOne, MortalityChanceAtAccelerationAge, t * t);
            }

            if (ageYears <= MortalityHighRiskAgeYears)
            {
                float t = Mathf.InverseLerp(
                    MortalityAccelerationAgeYears,
                    MortalityHighRiskAgeYears,
                    ageYears);
                return Mathf.Lerp(MortalityChanceAtAccelerationAge, MortalityChanceAtAgeFifty, t * t);
            }

            float lateAgeChance = MortalityChanceAtAgeFifty
                + (ageYears - MortalityHighRiskAgeYears) * MortalityChanceAfterFiftyPerYear;
            return Mathf.Min(MortalityMaxAnnualChance, lateAgeChance);
        }

        private static void RemoveResidentFromAssignments(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            UnassignFromLumberjackCamp(resident);
            UnassignFromStonecutterCamp(resident);
            UnassignFromHunterCamp(resident);
            UnassignFromFisherHut(resident);
            UnassignFromStorageWorkerRole(resident);
            UnassignFromStorageBuilderRole(resident);
            UnassignFromGranary(resident);
            resident.ClearConstructionSite(null);
        }

        private static void UnassignFromLumberjackCamp(StrategyResidentAgent resident)
        {
            StrategyLumberjackCamp camp = resident.Workplace;
            if (camp == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = camp.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    camp.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearWorkplace(camp);
        }

        private static void UnassignFromStonecutterCamp(StrategyResidentAgent resident)
        {
            StrategyStonecutterCamp camp = resident.StoneWorkplace;
            if (camp == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = camp.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    camp.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearStoneWorkplace(camp);
        }

        private static void UnassignFromHunterCamp(StrategyResidentAgent resident)
        {
            StrategyHunterCamp camp = resident.HunterWorkplace;
            if (camp == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = camp.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    camp.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearHunterWorkplace(camp);
        }

        private static void UnassignFromFisherHut(StrategyResidentAgent resident)
        {
            StrategyFisherHut hut = resident.FisherWorkplace;
            if (hut == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = hut.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    hut.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearFisherWorkplace(hut);
        }

        private static void UnassignFromStorageWorkerRole(StrategyResidentAgent resident)
        {
            StrategyStorageYard yard = resident.StorageWorkplace;
            if (yard == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = yard.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    yard.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearStorageWorkplace(yard);
        }

        private static void UnassignFromStorageBuilderRole(StrategyResidentAgent resident)
        {
            StrategyStorageYard yard = resident.BuilderWorkplace;
            if (yard == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> builders = yard.Builders;
            for (int i = builders.Count - 1; i >= 0; i--)
            {
                if (builders[i] == resident)
                {
                    yard.UnassignBuilderAt(i);
                    return;
                }
            }

            resident.ClearBuilderWorkplace(yard);
        }

        private static void UnassignFromGranary(StrategyResidentAgent resident)
        {
            StrategyGranary granary = resident.GranaryWorkplace;
            if (granary == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = granary.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    granary.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearGranaryWorkplace(granary);
        }

        private static void ClearSelectionForResident(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            StrategyWorldSelectionController[] controllers = Object.FindObjectsByType<StrategyWorldSelectionController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i]?.ClearSelectionIfTarget(resident);
            }
        }

        private void UpsertFamilyRecord(StrategyResidentAgent resident, bool isAlive)
        {
            if (resident == null || resident.ResidentId <= 0)
            {
                return;
            }

            if (!familyRecordsById.TryGetValue(resident.ResidentId, out StrategyResidentFamilyRecord record)
                || record == null)
            {
                record = new StrategyResidentFamilyRecord();
                familyRecordsById[resident.ResidentId] = record;
            }

            record.Configure(resident, isAlive);
        }

        private void RegisterResident(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return;
            }

            if (!residents.Contains(resident))
            {
                residents.Add(resident);
            }

            if (resident.ResidentId > 0)
            {
                residentsById[resident.ResidentId] = resident;
            }

            UpsertFamilyRecord(resident, true);
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

        private static string GenerateResidentName(StrategyResidentGender gender, string familyName = null)
        {
            string[] firstNames = gender == StrategyResidentGender.Male ? MaleFirstNames : FemaleFirstNames;
            return firstNames[Random.Range(0, firstNames.Length)]
                + " "
                + (string.IsNullOrWhiteSpace(familyName) ? GetRandomFamilyName() : familyName);
        }

        private static string GetRandomFamilyName()
        {
            return FamilyNames[Random.Range(0, FamilyNames.Length)];
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

        private void EnsureFuneralController()
        {
            if (funeralController == null)
            {
                funeralController = GetComponentInChildren<StrategyFuneralController>();
            }

            if (funeralController == null)
            {
                GameObject funeralObject = new GameObject("Funeral Controller");
                funeralObject.transform.SetParent(transform, false);
                funeralController = funeralObject.AddComponent<StrategyFuneralController>();
            }

            funeralController.Configure(map, this);
        }
    }
}
