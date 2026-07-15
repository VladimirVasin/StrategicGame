using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategySaveLooseResourceTests
    {
        [Test]
        public void Version4MigratesLooseResourcesWithoutDishMetadata()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 4;
            legacy.looseResources.Add(new StrategyLooseResourceSaveData
            {
                originX = 9,
                originY = 9,
                resource = (int)StrategyResourceType.Dish,
                amount = 2
            });

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy), out StrategySaveData restored, out string reason, out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.looseResources[0].preparedDishPile, Is.False);
            Assert.That(restored.looseResources[0].preparedDishRecipeId, Is.Empty);
        }

        [Test]
        public void LoosePreparedDishPayloadRoundTripsAndRequiresDishResource()
        {
            StrategySaveData save = CreateValidSave();
            save.looseResources.Add(new StrategyLooseResourceSaveData
            {
                originX = 9,
                originY = 9,
                resource = (int)StrategyResourceType.Dish,
                amount = 2,
                preparedDishPile = true,
                preparedDishRecipeId = "root_soup",
                preparedDishAmount = 2,
                preparedDishLeftoverRations = 0.35f
            });

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save), out StrategySaveData restored, out string reason, out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.looseResources[0].preparedDishRecipeId, Is.EqualTo("root_soup"));
            Assert.That(restored.looseResources[0].preparedDishLeftoverRations, Is.EqualTo(0.35f));

            save.looseResources[0].resource = (int)StrategyResourceType.Iron;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string invalidReason), Is.False);
            Assert.That(invalidReason, Does.StartWith("invalid_loose_prepared_dish_"));
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
