using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCityInventorySaveTests
    {
        [Test]
        public void Version7MigrationStartsWithAnEmptyCityInventory()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 7;
            legacy.cityItems.Add(CreateItem("legacy-ghost", 1));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.cityItems, Is.Empty);
        }

        [Test]
        public void CurrentVersionRoundTripsStableItemIdsAndQuantities()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem("old-king-seal", 1));
            save.cityItems.Add(CreateItem("amber-shard", 4));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                new StrategyCityItemCatalog(new[]
                {
                    new StrategyCityItemDefinition("old-king-seal", "Old King's Seal", 1),
                    new StrategyCityItemDefinition("amber-shard", "Amber Shard", 4)
                }),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.cityItems, Has.Count.EqualTo(2));
            Assert.That(restored.cityItems[0].itemId, Is.EqualTo("old-king-seal"));
            Assert.That(restored.cityItems[0].quantity, Is.EqualTo(1));
            Assert.That(restored.cityItems[1].itemId, Is.EqualTo("amber-shard"));
            Assert.That(restored.cityItems[1].quantity, Is.EqualTo(4));
        }

        [Test]
        public void ProductionPreflightRejectsUnknownItemsBeforeGameplayLoad()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem("removed-content", 1));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(reason, Is.EqualTo("unknown_city_item_0"));
            Assert.That(migrated, Is.False);
        }

        [Test]
        public void CatalogPreflightRejectsOverStackItemsBeforeGameplayLoad()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem("amber-shard", 3));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                new StrategyCityItemCatalog(new[]
                {
                    new StrategyCityItemDefinition("amber-shard", "Amber Shard", 2)
                }),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(reason, Is.EqualTo("city_item_stack_exceeded_0"));
            Assert.That(migrated, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ValidationRejectsBlankItemIds(string itemId)
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem(itemId, 1));

            AssertInvalid(save, "invalid_city_item_id_0");
        }

        [Test]
        public void ValidationRejectsOverlongItemIds()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem(
                new string('x', StrategySaveSystem.MaxSaveCityItemIdLength + 1),
                1));

            AssertInvalid(save, "invalid_city_item_id_0");
        }

        [TestCase("AmberShard")]
        [TestCase("-amber-shard")]
        [TestCase("amber/shard")]
        public void ValidationRejectsItemIdsOutsideTheCatalogContract(string itemId)
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem(itemId, 1));

            AssertInvalid(save, "invalid_city_item_id_0");
        }

        [Test]
        public void ValidationRejectsDuplicateItemIds()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem("amber-shard", 1));
            save.cityItems.Add(CreateItem("amber-shard", 2));

            AssertInvalid(save, "duplicate_city_item_id_1");
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ValidationRejectsNonPositiveQuantities(int quantity)
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem("amber-shard", quantity));

            AssertInvalid(save, "invalid_city_item_quantity_0");
        }

        [Test]
        public void ValidationRejectsQuantitiesAboveSafetyCap()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems.Add(CreateItem(
                "amber-shard",
                StrategySaveSystem.MaxSaveCityItemQuantity + 1));

            AssertInvalid(save, "invalid_city_item_quantity_0");
        }

        [Test]
        public void ValidationRejectsTooManyItemEntries()
        {
            StrategySaveData save = CreateValidSave();
            for (int i = 0; i <= StrategySaveSystem.MaxSaveCityItems; i++)
            {
                save.cityItems.Add(CreateItem("item-" + i, 1));
            }

            AssertInvalid(save, "collection_limit_exceeded");
        }

        [Test]
        public void ValidationRejectsMissingItemCollection()
        {
            StrategySaveData save = CreateValidSave();
            save.cityItems = null;

            AssertInvalid(save, "missing_collections");
        }

        [Test]
        public void SaveConversionOrdersEntriesByStableItemId()
        {
            List<StrategyCityInventoryEntry> source = new()
            {
                new StrategyCityInventoryEntry("zeta-token", 2),
                new StrategyCityInventoryEntry("amber-shard", 4)
            };
            List<StrategyCityItemSaveData> destination = new()
            {
                CreateItem("stale-entry", 1)
            };

            StrategySaveSystem.CopyCityInventoryEntriesForSave(source, destination);

            Assert.That(destination, Has.Count.EqualTo(2));
            Assert.That(destination[0].itemId, Is.EqualTo("amber-shard"));
            Assert.That(destination[0].quantity, Is.EqualTo(4));
            Assert.That(destination[1].itemId, Is.EqualTo("zeta-token"));
            Assert.That(destination[1].quantity, Is.EqualTo(2));
        }

        [Test]
        public void RestoreRejectsCatalogFailuresWithoutMutatingCurrentInventory()
        {
            GameObject root = new("CityInventorySaveTest");
            try
            {
                StrategyCityInventory inventory = root.AddComponent<StrategyCityInventory>();
                inventory.Configure(new StrategyCityItemCatalog(new[]
                {
                    new StrategyCityItemDefinition("amber-shard", "Amber Shard", 2),
                    new StrategyCityItemDefinition("zeta-token", "Zeta Token", 3)
                }));
                Assert.That(inventory.TryAdd("amber-shard", 1), Is.True);
                long versionBeforeRestore = inventory.Version;

                bool unknownRestored = StrategySaveSystem.TryRestoreCityInventory(
                    inventory,
                    new[] { CreateItem("unknown-token", 1) },
                    out StrategyCityInventoryRestoreFailure unknownFailure);
                bool overStackRestored = StrategySaveSystem.TryRestoreCityInventory(
                    inventory,
                    new[] { CreateItem("amber-shard", 3) },
                    out StrategyCityInventoryRestoreFailure overStackFailure);

                Assert.That(unknownRestored, Is.False);
                Assert.That(unknownFailure, Is.EqualTo(StrategyCityInventoryRestoreFailure.UnknownItem));
                Assert.That(overStackRestored, Is.False);
                Assert.That(overStackFailure, Is.EqualTo(StrategyCityInventoryRestoreFailure.InvalidQuantity));
                Assert.That(inventory.GetQuantity("amber-shard"), Is.EqualTo(1));
                Assert.That(inventory.GetQuantity("zeta-token"), Is.Zero);
                Assert.That(inventory.Version, Is.EqualTo(versionBeforeRestore));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RestoreReplacesAllItemsInOneInventoryChange()
        {
            GameObject root = new("CityInventorySaveTest");
            try
            {
                StrategyCityInventory inventory = root.AddComponent<StrategyCityInventory>();
                inventory.Configure(new StrategyCityItemCatalog(new[]
                {
                    new StrategyCityItemDefinition("amber-shard", "Amber Shard", 4),
                    new StrategyCityItemDefinition("zeta-token", "Zeta Token", 3)
                }));
                Assert.That(inventory.TryAdd("amber-shard", 1), Is.True);
                int changedCount = 0;
                inventory.Changed += () => changedCount++;

                bool restored = StrategySaveSystem.TryRestoreCityInventory(
                    inventory,
                    new[] { CreateItem("zeta-token", 2) },
                    out StrategyCityInventoryRestoreFailure failure);

                Assert.That(restored, Is.True, failure.ToString());
                Assert.That(changedCount, Is.EqualTo(1));
                Assert.That(inventory.GetQuantity("amber-shard"), Is.Zero);
                Assert.That(inventory.GetQuantity("zeta-token"), Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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

        private static StrategyCityItemSaveData CreateItem(string itemId, int quantity)
        {
            return new StrategyCityItemSaveData
            {
                itemId = itemId,
                quantity = quantity
            };
        }

        private static void AssertInvalid(StrategySaveData save, string expectedReason)
        {
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(expectedReason));
        }
    }
}
