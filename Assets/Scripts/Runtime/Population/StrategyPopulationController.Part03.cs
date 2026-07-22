using System.Collections.Generic;
using UnityEngine;
namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
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

            if (mapCell.IsWater || mapCell.IsShore || HasWaterNearCell(cell, CampMinWaterDistance))
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
            float angle = (Mathf.PI * 2f * spawnSlot / InitialCampSpawnSlotCount) + 0.35f;
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

        private static float GetNutritionMortalityMultiplier(
            StrategyResidentAgent resident,
            out int nutritionSeverity,
            out Vector2Int houseOrigin)
        {
            nutritionSeverity = 0;
            houseOrigin = Vector2Int.zero;
            StrategyPlacedBuilding home = resident != null ? resident.Home : null;
            if (home == null || home.Tool != StrategyBuildTool.House)
            {
                return 1f;
            }

            houseOrigin = home.Origin;
            nutritionSeverity = resident.NutritionSeverityLevel;
            return resident.NutritionMortalityMultiplier;
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
                GetFinalProfession(resident),
                GetFamilyRole(resident, home),
                householdIds.ToArray(),
                childIds.ToArray());
        }

        private static string GetFinalProfession(StrategyResidentAgent resident)
        {
            if (resident == null)
            {
                return "settler";
            }

            if (resident.LifeStage == StrategyResidentLifeStage.Child)
            {
                return "child";
            }

            if (resident.IsHouseholder)
            {
                return "householder";
            }

            if (resident.IsSettlementBuilder || resident.BuilderWorkplace != null || resident.ConstructionSite != null)
            {
                return "builder";
            }

            if (resident.Workplace != null)
            {
                return "lumberjack";
            }

            if (resident.StoneWorkplace != null)
            {
                return "stonecutter";
            }

            if (resident.MineWorkplace != null)
            {
                return "miner";
            }

            if (resident.CoalPitWorkplace != null)
            {
                return "coal miner";
            }

            if (resident.ClayPitWorkplace != null)
            {
                return "clay digger";
            }

            if (resident.SawmillWorkplace != null)
            {
                return "sawyer";
            }

            if (resident.KilnWorkplace != null)
            {
                return "potter";
            }

            if (resident.ForgeWorkplace != null)
            {
                return "blacksmith";
            }

            if (resident.HunterWorkplace != null)
            {
                return "hunter";
            }

            if (resident.FisherWorkplace != null)
            {
                return "fisher";
            }

            if (resident.ForagerWorkplace != null)
            {
                return "forager";
            }

            if (resident.ScoutWorkplace != null)
            {
                return "scout";
            }

            if (resident.IsSettlementHauler || resident.StorageWorkplace != null)
            {
                return "storekeeper";
            }

            if (resident.GranaryWorkplace != null)
            {
                return "granary worker";
            }

            return "settler";
        }

        private static string GetFamilyRole(StrategyResidentAgent resident, StrategyPlacedBuilding home)
        {
            if (resident == null)
            {
                return "settler";
            }

            if (resident.LifeStage == StrategyResidentLifeStage.Child)
            {
                return "child";
            }

            if (resident.ChildIds != null && resident.ChildIds.Count > 0)
            {
                return resident.Gender == StrategyResidentGender.Male ? "father" : "mother";
            }

            if (home != null && home.ResidentCount > 1)
            {
                return resident.Gender == StrategyResidentGender.Male ? "husband" : "wife";
            }

            return "settler";
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
    }
}
