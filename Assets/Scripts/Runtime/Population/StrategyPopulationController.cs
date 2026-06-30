using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyPopulationController : MonoBehaviour
    {
        private const int InitialFamilyCount = 3;
        private const int InitialAdultChildrenMin = 1;
        private const int InitialAdultChildrenMax = 2;
        private const int InitialCampSpawnSlotCount = InitialFamilyCount * (2 + InitialAdultChildrenMax);
        private const float InitialFatherAgeMin = 34f;
        private const float InitialFatherAgeMax = 40f;
        private const float InitialMotherAgeMin = 33f;
        private const float InitialMotherAgeMax = 38f;
        private const float InitialAdultChildAgeMin = 16f;
        private const float InitialAdultChildAgeMax = 18f;
        private const float InitialParentChildAgeGapMin = 17f;
        private const int CampSpawnRadius = 3;
        private const int CampMinWaterDistance = 6;
        private const float HouseholdMigrationCheckInterval = 4f;
        private const int MortalityStartAgeYears = 1;
        private const int MortalityAccelerationAgeYears = 40;
        private const int MortalityHighRiskAgeYears = 50;
        private const float MortalityChanceAtAgeOne = 0.0002f;
        private const float MortalityChanceAtAccelerationAge = 0.002f;
        private const float MortalityChanceAtAgeFifty = 0.04f;
        private const float MortalityChanceAfterFiftyPerYear = 0.018f;
        private const float MortalityMaxAnnualChance = 0.70f;
        private const float MalnutritionMortalityMaxAnnualChance = 0.95f;

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
        private readonly Dictionary<string, int> familyNameUseCounts = new();
        private CityMapController map;
        private Transform residentRoot;
        private StrategyFuneralController funeralController;
        private StrategyHomelessCampController homelessCamp;
        private Vector2Int campCell;
        private Vector3 campWorld;
        private float householdMigrationTimer;
        private int nextResidentId = 1;
        private bool hasStarterCamp;

        public IReadOnlyList<StrategyResidentAgent> Residents => residents;
        internal IReadOnlyCollection<StrategyResidentFamilyRecord> FamilyRecords => familyRecordsById.Values;
        public int TotalResidentCount => CountResidents(false, false);
        public int AdultResidentCount => CountResidents(true, false);
        public int ChildResidentCount => CountResidents(false, true);
        public int CompletedHouseCount => CountRegisteredHouses();
        internal StrategyHomelessCampController HomelessCamp => homelessCamp;

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

            householdMigrationTimer -= Time.unscaledDeltaTime;
            if (householdMigrationTimer > 0f)
            {
                return;
            }

            householdMigrationTimer = HouseholdMigrationCheckInterval;
            TryAdoptOrphanedChildren();
            TryRejoinHomelessChildrenWithParents("migration_tick");
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
            float nutritionMultiplier = GetNutritionMortalityMultiplier(
                resident,
                out int nutritionSeverity,
                out Vector2Int malnutritionHouseOrigin);
            float annualChance = Mathf.Min(
                MalnutritionMortalityMaxAnnualChance,
                baseAnnualChance * nutritionMultiplier);
            if (annualChance <= 0f || Random.value >= annualChance)
            {
                return false;
            }

            string reason = nutritionMultiplier > 1f ? "malnutrition_mortality" : "annual_mortality";
            return HandleResidentDeath(
                resident,
                reason,
                annualChance,
                baseAnnualChance,
                nutritionMultiplier,
                nutritionSeverity,
                malnutritionHouseOrigin);
        }

        public bool TryKillResidentByWolf(StrategyResidentAgent resident, Vector3 attackWorld)
        {
            if (resident == null
                || resident.IsPendingRefugee
                || !residents.Contains(resident))
            {
                return false;
            }

            string residentName = resident.FullName;
            int residentId = resident.ResidentId;
            bool killed = HandleResidentDeath(
                resident,
                "wolf_attack",
                1f,
                0f,
                1f,
                0,
                Vector2Int.zero);
            if (killed)
            {
                StrategyDebugLogger.Info(
                    "Population",
                    "ResidentKilledByWolf",
                    StrategyDebugLogger.F("resident", residentName),
                    StrategyDebugLogger.F("residentId", residentId),
                    StrategyDebugLogger.F("attackWorld", attackWorld));
            }

            return killed;
        }

        private bool HandleResidentDeath(
            StrategyResidentAgent resident,
            string reason,
            float annualChance,
            float baseAnnualChance = 0f,
            float nutritionMultiplier = 1f,
            int nutritionSeverity = 0,
            Vector2Int malnutritionHouseOrigin = default)
        {
            if (resident == null || resident.IsPendingRefugee || !residents.Contains(resident))
            {
                return false;
            }

            if (resident.IsFuneralDutyActive)
            {
                StrategyDebugLogger.Info("Population", "ResidentDeathBlockedByFuneral", StrategyDebugLogger.F("resident", resident.FullName), StrategyDebugLogger.F("residentId", resident.ResidentId), StrategyDebugLogger.F("reason", reason));
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
            StrategyEventLogHudController.Notify("Died: " + residentName + ", age " + age, new Color(0.92f, 0.48f, 0.42f));
            TryAdoptOrphanedChildren();

            StrategyDebugLogger.Info(
                "Population",
                "ResidentDied",
                StrategyDebugLogger.F("resident", residentName),
                StrategyDebugLogger.F("residentId", residentId),
                StrategyDebugLogger.F("age", age),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("finalProfession", snapshot.FinalProfession),
                StrategyDebugLogger.F("familyRole", snapshot.FamilyRole),
                StrategyDebugLogger.F("annualChance", annualChance),
                StrategyDebugLogger.F("baseAnnualChance", baseAnnualChance),
                StrategyDebugLogger.F("nutritionMultiplier", nutritionMultiplier),
                StrategyDebugLogger.F("nutritionSeverity", nutritionSeverity),
                StrategyDebugLogger.F("malnutritionHouseOrigin", malnutritionHouseOrigin),
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
            ApplyMarriageSurname(male, female, house);
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

            bool assigned = TryDispatchSettlementBuildersToSite(site, true);
            if (!assigned)
            {
                StrategyDebugLogger.Warn(
                    "Population",
                    "ConstructionBuildersRejected",
                    StrategyDebugLogger.F("tool", site.Tool),
                    StrategyDebugLogger.F("origin", site.Origin),
                    StrategyDebugLogger.F("reason", "no_settlement_builders"));
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

            TryRejoinHomelessChildrenWithParents("house_completed");
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
    }
}
