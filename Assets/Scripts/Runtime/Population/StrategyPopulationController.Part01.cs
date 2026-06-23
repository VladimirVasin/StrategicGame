using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
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
            StrategyEventLogHudController.Notify("Born: " + childName, new Color(0.64f, 0.88f, 0.52f));
            return true;
        }

        public bool TryCreateRefugeeFamily(
            Vector3 spawnWorld,
            Vector2 formationAxis,
            Vector2Int temporaryIdleOrigin,
            int parentCount,
            int childCount,
            out List<StrategyResidentAgent> family)
        {
            family = new List<StrategyResidentAgent>();
            if (map == null)
            {
                return false;
            }

            EnsureResidentRoot();

            int normalizedParentCount = Mathf.Clamp(parentCount, 1, 2);
            int normalizedChildCount = Mathf.Clamp(childCount, 0, 3 - normalizedParentCount);
            Vector2 axis = formationAxis.sqrMagnitude > 0.001f
                ? formationAxis.normalized
                : Vector2.right;
            string familyName = ReserveFamilyName();
            StrategyResidentAgent father = null;
            StrategyResidentAgent mother = null;

            for (int i = 0; i < normalizedParentCount; i++)
            {
                StrategyResidentGender gender = normalizedParentCount == 2
                    ? i == 0 ? StrategyResidentGender.Male : StrategyResidentGender.Female
                    : Random.value < 0.5f ? StrategyResidentGender.Male : StrategyResidentGender.Female;
                int parentId = AllocateResidentId();
                StrategyResidentAgent parent = CreateRefugeeResident(
                    gender,
                    parentId,
                    0,
                    0,
                    familyName,
                    GetRefugeeParentAge(gender),
                    StrategyResidentLifeStage.Adult,
                    spawnWorld + GetRefugeeFormationOffset(axis, i) * map.CellSize,
                    temporaryIdleOrigin);

                if (parent == null)
                {
                    DestroyTemporaryResidents(family);
                    return false;
                }

                family.Add(parent);
                if (gender == StrategyResidentGender.Male)
                {
                    father = parent;
                }
                else
                {
                    mother = parent;
                }
            }

            for (int i = 0; i < normalizedChildCount; i++)
            {
                StrategyResidentGender gender = Random.value < 0.5f
                    ? StrategyResidentGender.Male
                    : StrategyResidentGender.Female;
                int childId = AllocateResidentId();
                StrategyResidentAgent child = CreateRefugeeResident(
                    gender,
                    childId,
                    father != null ? father.ResidentId : 0,
                    mother != null ? mother.ResidentId : 0,
                    familyName,
                    Random.Range(2f, 15f),
                    StrategyResidentLifeStage.Child,
                    spawnWorld + GetRefugeeFormationOffset(axis, i + normalizedParentCount) * map.CellSize,
                    temporaryIdleOrigin);

                if (child == null)
                {
                    continue;
                }

                father?.AddChildId(childId);
                mother?.AddChildId(childId);
                family.Add(child);
            }

            StrategyDebugLogger.Info(
                "Refugees",
                "FamilyCreated",
                StrategyDebugLogger.F("family", familyName),
                StrategyDebugLogger.F("members", family.Count),
                StrategyDebugLogger.F("parents", normalizedParentCount),
                StrategyDebugLogger.F("children", normalizedChildCount),
                StrategyDebugLogger.F("spawnWorld", spawnWorld));
            return family.Count >= 1;
        }

        private static float GetRefugeeParentAge(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male
                ? Random.Range(24f, 42f)
                : Random.Range(22f, 39f);
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
                TrackUnsettledRefugeeFamily(acceptedFamily);
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

            if (TryFindUnsettledFamilyForHouse(destinationHouse, out family))
            {
                return true;
            }

            HashSet<int> checkedFamilies = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent child = residents[i];
                if (child == null
                    || child.IsPendingRefugee
                    || child.Home != null
                    || (child.FatherId <= 0 && child.MotherId <= 0))
                {
                    continue;
                }

                int familyKey = child.FatherId * 397 ^ child.MotherId;
                if (checkedFamilies.Contains(familyKey))
                {
                    continue;
                }

                checkedFamilies.Add(familyKey);
                int fatherId = child.FatherId;
                int motherId = child.MotherId;
                List<StrategyResidentAgent> candidateFamily = new();
                if (fatherId > 0
                    && TryGetResidentById(fatherId, out StrategyResidentAgent father)
                    && father.Gender == StrategyResidentGender.Male
                    && father.Home == null
                    && CanMoveResidentToHouse(father, destinationHouse))
                {
                    candidateFamily.Add(father);
                }

                if (motherId > 0
                    && TryGetResidentById(motherId, out StrategyResidentAgent mother)
                    && mother.Gender == StrategyResidentGender.Female
                    && mother.Home == null
                    && CanMoveResidentToHouse(mother, destinationHouse))
                {
                    candidateFamily.Add(mother);
                }

                if (candidateFamily.Count <= 0)
                {
                    continue;
                }

                for (int memberIndex = 0; memberIndex < residents.Count; memberIndex++)
                {
                    StrategyResidentAgent candidate = residents[memberIndex];
                    if (candidate == null
                        || candidate.IsPendingRefugee
                        || candidate.Home != null
                        || candidate.FatherId != fatherId
                        || candidate.MotherId != motherId
                        || candidateFamily.Contains(candidate))
                    {
                        continue;
                    }

                    candidateFamily.Add(candidate);
                }

                if (candidateFamily.Count >= 2 && candidateFamily.Count <= destinationHouse.ResidentCapacity)
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
            TryClearUnsettledRefugeeFamilyIfSettled(family[0], house);
            StrategyDebugLogger.Info(
                "Population",
                logEvent,
                StrategyDebugLogger.F("houseOrigin", house.Origin),
                StrategyDebugLogger.F("members", family.Count),
                StrategyDebugLogger.F("family", family[0] != null ? family[0].FamilyName : string.Empty));
            return true;
        }
    }
}
