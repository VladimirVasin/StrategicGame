using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private void SpawnInitialResidents()
        {
            HashSet<Vector2Int> usedCells = new();
            int spawnSlot = 0;
            int adultChildren = 0;
            for (int familyIndex = 0; familyIndex < InitialFamilyCount; familyIndex++)
            {
                string familyName = ReserveFamilyName();
                int fatherId = AllocateResidentId();
                int motherId = AllocateResidentId();
                float fatherAge = Random.Range(InitialFatherAgeMin, InitialFatherAgeMax);
                float motherAge = Random.Range(InitialMotherAgeMin, InitialMotherAgeMax);

                StrategyResidentAgent father = SpawnInitialFamilyResident(
                    StrategyResidentGender.Male,
                    spawnSlot++,
                    usedCells,
                    fatherId,
                    0,
                    0,
                    familyName,
                    fatherAge);
                StrategyResidentAgent mother = SpawnInitialFamilyResident(
                    StrategyResidentGender.Female,
                    spawnSlot++,
                    usedCells,
                    motherId,
                    0,
                    0,
                    familyName,
                    motherAge);

                int childCount = Random.Range(InitialAdultChildrenMin, InitialAdultChildrenMax + 1);
                for (int i = 0; i < childCount; i++)
                {
                    StrategyResidentGender childGender = Random.value < 0.5f
                        ? StrategyResidentGender.Male
                        : StrategyResidentGender.Female;
                    int childId = AllocateResidentId();
                    StrategyResidentAgent child = SpawnInitialFamilyResident(
                        childGender,
                        spawnSlot++,
                        usedCells,
                        childId,
                        fatherId,
                        motherId,
                        familyName,
                        GetInitialAdultChildAge(fatherAge, motherAge));
                    if (child == null)
                    {
                        continue;
                    }

                    father?.AddChildId(childId);
                    mother?.AddChildId(childId);
                    adultChildren++;
                }

                UpsertFamilyRecord(father, true);
                UpsertFamilyRecord(mother, true);
                StrategyDebugLogger.Info(
                    "Population",
                    "InitialFamilySpawned",
                    StrategyDebugLogger.F("family", familyName),
                    StrategyDebugLogger.F("fatherId", fatherId),
                    StrategyDebugLogger.F("motherId", motherId),
                    StrategyDebugLogger.F("adultChildren", childCount));
            }

            StrategyDebugLogger.Info(
                "Population",
                "InitialFamiliesSpawned",
                StrategyDebugLogger.F("families", InitialFamilyCount),
                StrategyDebugLogger.F("adultChildren", adultChildren),
                StrategyDebugLogger.F("residents", residents.Count));
        }

        private StrategyResidentAgent SpawnInitialFamilyResident(
            StrategyResidentGender gender,
            int spawnSlot,
            HashSet<Vector2Int> usedCells,
            int residentId,
            int fatherId,
            int motherId,
            string familyName,
            float age)
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
            string residentName = GenerateResidentName(gender, familyName);

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
                fatherId,
                motherId,
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
                StrategyDebugLogger.F("fatherId", fatherId),
                StrategyDebugLogger.F("motherId", motherId),
                StrategyDebugLogger.F("spawnCell", foundSpawnCell ? spawnCell : Vector2Int.zero),
                StrategyDebugLogger.F("spawnWorld", spawnWorld),
                StrategyDebugLogger.F("usedFallback", !foundSpawnCell));
            return agent;
        }

        private static float GetInitialAdultChildAge(float fatherAge, float motherAge)
        {
            float youngestParentAge = Mathf.Min(fatherAge, motherAge);
            float maxAge = Mathf.Min(
                InitialAdultChildAgeMax,
                youngestParentAge - InitialParentChildAgeGapMin);
            maxAge = Mathf.Max(InitialAdultChildAgeMin, maxAge);
            return Random.Range(InitialAdultChildAgeMin, maxAge + 0.99f);
        }
    }
}
