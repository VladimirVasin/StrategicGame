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

            if (ageYears <= MortalityOldAgeYears)
            {
                float t = Mathf.InverseLerp(
                    MortalityAccelerationAgeYears,
                    MortalityOldAgeYears,
                    ageYears);
                float shapedT = 0.25f * t + 0.75f * t * t;
                return Mathf.Lerp(MortalityChanceAtAccelerationAge, MortalityChanceAtOldAge, shapedT);
            }

            if (ageYears <= MortalityHighRiskAgeYears)
            {
                float t = Mathf.InverseLerp(
                    MortalityOldAgeYears,
                    MortalityHighRiskAgeYears,
                    ageYears);
                float shapedT = (t + 10f * t * t) / 11f;
                return Mathf.Lerp(MortalityChanceAtOldAge, MortalityChanceAtHighRiskAge, shapedT);
            }

            if (ageYears <= MortalitySevereRiskAgeYears)
            {
                float t = Mathf.InverseLerp(
                    MortalityHighRiskAgeYears,
                    MortalitySevereRiskAgeYears,
                    ageYears);
                float shapedT = 0.5f * t + 0.5f * t * t;
                return Mathf.Lerp(MortalityChanceAtHighRiskAge, MortalityChanceAtSevereRiskAge, shapedT);
            }

            float lateAgeChance = MortalityChanceAtSevereRiskAge
                + (ageYears - MortalitySevereRiskAgeYears) * MortalityChanceAfterSevereRiskPerYear;
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
            UnassignFromMine(resident);
            UnassignFromCoalPit(resident);
            UnassignFromSawmill(resident);
            UnassignFromHunterCamp(resident);
            UnassignFromFisherHut(resident);
            UnassignFromStorageWorkerRole(resident);
            UnassignFromStorageBuilderRole(resident);
            resident.ClearSettlementHaulerRole();
            resident.ClearSettlementBuilderRole();
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

        private static void UnassignFromMine(StrategyResidentAgent resident)
        {
            StrategyMine mine = resident.MineWorkplace;
            if (mine == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = mine.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    mine.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearMineWorkplace(mine);
        }

        private static void UnassignFromCoalPit(StrategyResidentAgent resident)
        {
            StrategyCoalPit pit = resident.CoalPitWorkplace;
            if (pit == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = pit.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    pit.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearCoalPitWorkplace(pit);
        }

        private static void UnassignFromSawmill(StrategyResidentAgent resident)
        {
            StrategySawmill sawmill = resident.SawmillWorkplace;
            if (sawmill == null)
            {
                return;
            }

            IReadOnlyList<StrategyResidentAgent> workers = sawmill.Workers;
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                if (workers[i] == resident)
                {
                    sawmill.UnassignWorkerAt(i);
                    return;
                }
            }

            resident.ClearSawmillWorkplace(sawmill);
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

        private string GenerateResidentName(StrategyResidentGender gender, string familyName = null)
        {
            string[] firstNames = gender == StrategyResidentGender.Male ? MaleFirstNames : FemaleFirstNames;
            return firstNames[Random.Range(0, firstNames.Length)]
                + " "
                + (string.IsNullOrWhiteSpace(familyName) ? ReserveFamilyName() : familyName);
        }

        private static string GetRandomFamilyName()
        {
            return FamilyNames[Random.Range(0, FamilyNames.Length)];
        }

        private string ReserveFamilyName()
        {
            int lowestUseCount = int.MaxValue;
            List<string> candidates = new();
            for (int i = 0; i < FamilyNames.Length; i++)
            {
                string familyName = FamilyNames[i];
                familyNameUseCounts.TryGetValue(familyName, out int useCount);
                if (useCount < lowestUseCount)
                {
                    lowestUseCount = useCount;
                    candidates.Clear();
                }

                if (useCount == lowestUseCount)
                {
                    candidates.Add(familyName);
                }
            }

            if (candidates.Count <= 0)
            {
                return GetRandomFamilyName();
            }

            string selected = candidates[Random.Range(0, candidates.Count)];
            familyNameUseCounts[selected] = lowestUseCount + 1;
            return selected;
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
