using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyScoutExpeditionSaveTests
    {
        [Test]
        public void Version9MigratesWithNoPersistedScoutLodges()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 9;
            legacy.scoutLodges = null;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.scoutLodges, Is.Empty);
        }

        [Test]
        public void ActiveExpeditionRoundTripsStableCrossReferences()
        {
            StrategySaveData save = CreateValidSave();
            save.scoutLodges.Add(CreateActiveExpedition("scout-lodge-a", 7));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.scoutLodges, Has.Count.EqualTo(1));
            Assert.That(restored.scoutLodges[0].residentId, Is.EqualTo(7));
            Assert.That(
                restored.scoutLodges[0].expeditionState,
                Is.EqualTo((int)StrategyScoutExpeditionState.Exploring));
            Assert.That(restored.scoutLodges[0].remainingFieldRations, Is.EqualTo(3f));
        }

        [Test]
        public void ScoutStateRejectsInvalidReferencesRationsAndTiming()
        {
            StrategySaveData save = CreateValidSave();
            StrategyScoutLodgeSaveData expedition =
                CreateActiveExpedition("house-a", 7);
            save.scoutLodges.Add(expedition);
            AssertInvalid(save, "invalid_scout_lodge_reference_");

            expedition.lodgeStableId = "scout-lodge-a";
            save.scoutLodges.Add(CreateActiveExpedition("scout-lodge-b", 7));
            AssertInvalid(save, "invalid_scout_lodge_resident_or_state_");

            save.scoutLodges.RemoveAt(1);
            expedition.remainingFieldRations = 8f;
            AssertInvalid(save, "invalid_scout_lodge_mission_");

            expedition.remainingFieldRations = 3f;
            expedition.endsElapsedSeconds += 1f;
            AssertInvalid(save, "invalid_scout_lodge_mission_");

            expedition.expeditionState = (int)StrategyScoutExpeditionState.Ready;
            expedition.plannedDays = 0;
            expedition.startedElapsedSeconds = 0f;
            expedition.endsElapsedSeconds = 0f;
            expedition.remainingFieldRations = 0f;
            expedition.lastProvisionedDayIndex = 0;
            AssertInvalid(save, "invalid_scout_lodge_mission_");
        }

        private static StrategySaveData CreateValidSave()
        {
            StrategySaveData save = new()
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                elapsedSeconds = 40f,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
            save.buildings.Add(CreateBuilding("house-a", StrategyBuildTool.House, 2, 3, 2, 2));
            save.buildings.Add(CreateBuilding("scout-lodge-a", StrategyBuildTool.ScoutLodge, 8, 8, 2, 4));
            save.buildings.Add(CreateBuilding("scout-lodge-b", StrategyBuildTool.ScoutLodge, 14, 8, 2, 4));
            save.residents.Add(new StrategyResidentSaveData
            {
                residentId = 7,
                homeStableId = "house-a",
                gender = (int)StrategyResidentGender.Male,
                lifeStage = (int)StrategyResidentLifeStage.Adult,
                ageYears = 27f,
                worldX = 8.5f,
                worldY = 8.5f
            });
            return save;
        }

        private static StrategyBuildingSaveData CreateBuilding(
            string stableId,
            StrategyBuildTool tool,
            int x,
            int y,
            int width,
            int height)
        {
            return new StrategyBuildingSaveData
            {
                stableId = stableId,
                tool = (int)tool,
                originX = x,
                originY = y,
                footprintX = width,
                footprintY = height
            };
        }

        private static StrategyScoutLodgeSaveData CreateActiveExpedition(
            string lodgeId,
            int residentId)
        {
            return new StrategyScoutLodgeSaveData
            {
                lodgeStableId = lodgeId,
                residentId = residentId,
                expeditionState = (int)StrategyScoutExpeditionState.Exploring,
                plannedDays = 3,
                startedElapsedSeconds = 20f,
                endsElapsedSeconds = 20f + StrategyDayNightCycleController.DayLengthSeconds * 3f,
                remainingFieldRations = 3f,
                provisionRationCredit = 0.25f,
                lastProvisionedDayIndex = -1
            };
        }

        private static void AssertInvalid(StrategySaveData save, string prefix)
        {
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith(prefix));
        }
    }
}
