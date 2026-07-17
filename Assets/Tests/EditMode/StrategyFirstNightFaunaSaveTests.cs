using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightFaunaSaveTests
    {
        [TestCase(0f, StrategyFirstNightFaunaStage.Dormant)]
        [TestCase(209.99f, StrategyFirstNightFaunaStage.Dormant)]
        [TestCase(210f, StrategyFirstNightFaunaStage.MiceVisible)]
        [TestCase(254.99f, StrategyFirstNightFaunaStage.MiceVisible)]
        [TestCase(255f, StrategyFirstNightFaunaStage.StoryCompleted)]
        [TestCase(720f, StrategyFirstNightFaunaStage.StoryCompleted)]
        public void Version6MigrationDerivesStageFromFirstNightBoundaries(
            float elapsedSeconds,
            StrategyFirstNightFaunaStage expected)
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 6;
            legacy.elapsedSeconds = elapsedSeconds;
            legacy.firstNightFaunaStage = (int)StrategyFirstNightFaunaStage.StoryCompleted;

            bool migrated = StrategySaveMigration.TryMigrate(legacy, out string reason);

            Assert.That(migrated, Is.True, reason);
            Assert.That(legacy.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(legacy.firstNightFaunaStage, Is.EqualTo((int)expected));
        }

        [Test]
        public void CurrentStageRoundTripsWithoutMigration()
        {
            StrategySaveData save = CreateValidSave();
            save.firstNightFaunaStage = (int)StrategyFirstNightFaunaStage.MiceVisible;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(
                restored.firstNightFaunaStage,
                Is.EqualTo((int)StrategyFirstNightFaunaStage.MiceVisible));
        }

        [TestCase(-9)]
        [TestCase(99)]
        public void ValidationRejectsInvalidStage(int savedStage)
        {
            StrategySaveData save = CreateValidSave();
            save.firstNightFaunaStage = savedStage;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out _,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.False);
            Assert.That(migrated, Is.False);
            Assert.That(reason, Is.EqualTo("invalid_first_night_fauna_stage"));
        }

        private static StrategySaveData CreateValidSave()
        {
            return new StrategySaveData
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                elapsedSeconds = 10f,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
        }
    }
}
