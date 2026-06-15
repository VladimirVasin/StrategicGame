using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {

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

            ApplyMarriageSurname(resident, partner, house);
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

        private void ApplyMarriageSurname(
            StrategyResidentAgent first,
            StrategyResidentAgent second,
            StrategyPlacedBuilding house)
        {
            if (first == null
                || second == null
                || first.Gender == second.Gender)
            {
                return;
            }

            StrategyResidentAgent husband = first.Gender == StrategyResidentGender.Male
                ? first
                : second;
            StrategyResidentAgent wife = first.Gender == StrategyResidentGender.Female
                ? first
                : second;
            string husbandFamily = husband.FamilyName;
            if (string.IsNullOrWhiteSpace(husbandFamily) || wife.FamilyName == husbandFamily)
            {
                return;
            }

            string previousName = wife.FullName;
            string previousFamily = wife.FamilyName;
            if (!wife.TryChangeFamilyName(husbandFamily))
            {
                return;
            }

            UpsertFamilyRecord(wife, true);
            StrategyDebugLogger.Info(
                "Population",
                "MarriageSurnameChanged",
                StrategyDebugLogger.F("wife", wife.FullName),
                StrategyDebugLogger.F("wifeId", wife.ResidentId),
                StrategyDebugLogger.F("husband", husband.FullName),
                StrategyDebugLogger.F("husbandId", husband.ResidentId),
                StrategyDebugLogger.F("previousName", previousName),
                StrategyDebugLogger.F("previousFamily", previousFamily),
                StrategyDebugLogger.F("newFamily", husbandFamily),
                StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero));
        }

        private static bool IsHouseBlockedByFoodShortage(StrategyPlacedBuilding house)
        {
            if (house == null || house.Tool != StrategyBuildTool.House)
            {
                return false;
            }

            StrategyHouseholdFoodState food = house.GetComponent<StrategyHouseholdFoodState>();
            return food != null && food.IsBirthBlocked;
        }

        private static void LogHouseMoveBlockedByFood(StrategyPlacedBuilding house, string reason)
        {
            StrategyHouseholdFoodState food = house != null ? house.GetComponent<StrategyHouseholdFoodState>() : null;
            StrategyDebugLogger.Info(
                "Population",
                "HouseholdMoveBlockedFoodShortage",
                StrategyDebugLogger.F("houseOrigin", house != null ? house.Origin : Vector2Int.zero),
                StrategyDebugLogger.F("reason", reason),
                StrategyDebugLogger.F("nutritionSeverity", food != null ? food.NutritionSeverityLevel : 0),
                StrategyDebugLogger.F("requiredRations", food != null ? food.LastRequiredRations : 0f),
                StrategyDebugLogger.F("suppliedRations", food != null ? food.LastSuppliedRations : 0f),
                StrategyDebugLogger.F("lastConsumedFoodUnits", food != null ? food.LastConsumedFood : 0));
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
                    && candidate.CanAcceptWorkAssignment
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
    }
}
