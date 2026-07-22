using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        public void ClearResidentsForLoad()
        {
            for (int i = residents.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null)
                {
                    continue;
                }

                resident.Home?.UnregisterResident(resident);
                resident.gameObject.SetActive(false);
                Destroy(resident.gameObject);
            }

            residents.Clear();
            residentsById.Clear();
            familyRecordsById.Clear();
            familyNameUseCounts.Clear();
            unsettledRefugeeFamilyByResidentId.Clear();
            unsettledRefugeeFamilyMembersByGroup.Clear();
            nextUnsettledRefugeeFamilyGroupId = 1;
            movingUnsettledRefugeeFamily = false;
            nextResidentId = 1;
        }

        public StrategyResidentAgent RestoreResident(
            StrategyResidentSaveData data,
            IReadOnlyDictionary<string, StrategyPlacedBuilding> buildingsById)
        {
            if (data == null || map == null)
            {
                return null;
            }

            StrategyPlacedBuilding home = null;
            if (!string.IsNullOrWhiteSpace(data.homeStableId) && buildingsById != null)
            {
                buildingsById.TryGetValue(data.homeStableId, out home);
            }

            StrategyResidentGender gender = (StrategyResidentGender)data.gender;
            StrategyResidentLifeStage lifeStage = (StrategyResidentLifeStage)data.lifeStage;
            int variant = Mathf.Abs(data.visualVariant) % Mathf.Max(1, StrategyResidentSpriteFactory.VariantCountPerGender);
            string residentName = string.IsNullOrWhiteSpace(data.fullName) ? "Unnamed Resident" : data.fullName;
            GameObject residentObject = new GameObject(residentName);
            residentObject.transform.SetParent(residentRoot, false);
            SpriteRenderer renderer = residentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = StrategyResidentSpriteFactory.GetSprite(gender, variant, lifeStage);

            StrategyResidentAgent agent = residentObject.AddComponent<StrategyResidentAgent>();
            agent.Configure(
                map,
                home,
                gender,
                variant,
                residentName,
                new Vector3(data.worldX, data.worldY, -0.08f),
                renderer,
                home != null ? home.Origin : campCell,
                home != null ? home.Footprint : Vector2Int.one,
                data.residentId,
                data.ageYears,
                lifeStage,
                data.fatherId,
                data.motherId,
                data.familyName);
            agent.RestorePersistentConditionState(
                data.nutritionDebt,
                data.daysHungry,
                data.lastNutritionDayIndex,
                data.coldExposure,
                data.lastColdResolutionDayIndex,
                data.childIds);
            agent.RestoreCombatState(
                data.combatHealth,
                data.lastCombatRecoveryDayIndex);
            if (!StrategySaveSystem.TryRestoreResidentPersonalInventory(
                    agent,
                    data.personalItems,
                    out StrategyResidentPersonalInventoryFailure inventoryFailure))
            {
                StrategyDebugLogger.Warn(
                    "Save",
                    "ResidentPersonalInventoryRestoreFailed",
                    StrategyDebugLogger.F("residentId", data.residentId),
                    StrategyDebugLogger.F("reason", inventoryFailure));
                agent.gameObject.SetActive(false);
                Destroy(agent.gameObject);
                return null;
            }

            RegisterResident(agent);
            nextResidentId = Mathf.Max(nextResidentId, data.residentId + 1);
            if (!string.IsNullOrWhiteSpace(agent.FamilyName))
            {
                familyNameUseCounts.TryGetValue(agent.FamilyName, out int useCount);
                familyNameUseCounts[agent.FamilyName] = useCount + 1;
            }

            return agent;
        }

        public void FinalizeResidentRestore()
        {
            for (int i = 0; i < houses.Count; i++)
            {
                houses[i]?.EnsureHouseholder();
            }

            householdMigrationTimer = HouseholdMigrationCheckInterval;
        }
    }
}
