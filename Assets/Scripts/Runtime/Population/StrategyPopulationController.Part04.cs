using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {

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
