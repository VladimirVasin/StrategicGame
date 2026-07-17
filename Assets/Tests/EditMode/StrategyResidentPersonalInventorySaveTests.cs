using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResidentPersonalInventorySaveTests
    {
        [Test]
        public void Version10MigrationInitializesEveryResidentWithAnEmptyInventory()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 10;
            legacy.residents[0].personalItems.Add(Item("legacy-test-item", 1));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.residents[0].personalItems, Is.Empty);
        }

        [Test]
        public void CurrentVersionRoundTripsItemsInsideTheirResidentRecord()
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].personalItems.Add(Item("test-item", 2));
            StrategyResidentItemCatalog residentCatalog = new(new[]
            {
                Definition("test-item", 2)
            });

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                StrategyCityItemCatalog.Production,
                residentCatalog,
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.residents, Has.Count.EqualTo(1));
            Assert.That(restored.residents[0].residentId, Is.EqualTo(11));
            Assert.That(restored.residents[0].personalItems, Has.Count.EqualTo(1));
            Assert.That(restored.residents[0].personalItems[0].itemId, Is.EqualTo("test-item"));
            Assert.That(restored.residents[0].personalItems[0].quantity, Is.EqualTo(2));
        }

        [Test]
        public void ProductionPreflightRejectsUnknownResidentItems()
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].personalItems.Add(Item("test-item", 1));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(reason, Is.EqualTo("unknown_resident_personal_item_0_0"));
            Assert.That(migrated, Is.False);
        }

        [Test]
        public void StructuralValidationRejectsItemsOwnedByAChild()
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].lifeStage = (int)StrategyResidentLifeStage.Child;
            save.residents[0].ageYears = 8f;
            save.residents[0].personalItems.Add(Item("test-item", 1));

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("ineligible_resident_personal_items_0"));
        }

        [Test]
        public void StructuralValidationRejectsMoreThanSixDistinctItems()
        {
            StrategySaveData save = CreateValidSave();
            for (int i = 0; i <= StrategyResidentPersonalInventory.SlotCapacity; i++)
            {
                save.residents[0].personalItems.Add(Item("test-item-" + i, 1));
            }

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo("resident_personal_item_limit_exceeded_0"));
        }

        [Test]
        public void CatalogPreflightRejectsOverStackQuantities()
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].personalItems.Add(Item("test-item", 2));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                StrategyCityItemCatalog.Production,
                new StrategyResidentItemCatalog(new[] { Definition("test-item", 1) }),
                out StrategySaveData restored,
                out string reason,
                out _);

            Assert.That(loaded, Is.False);
            Assert.That(restored, Is.Null);
            Assert.That(reason, Is.EqualTo("resident_personal_item_stack_exceeded_0_0"));
        }

        [Test]
        public void SaveConversionOrdersItemsByStableId()
        {
            List<StrategyResidentPersonalItemEntry> source = new()
            {
                new StrategyResidentPersonalItemEntry("test-z", 1),
                new StrategyResidentPersonalItemEntry("test-a", 2)
            };
            List<StrategyResidentItemSaveData> destination = new()
            {
                Item("stale", 1)
            };

            StrategySaveSystem.CopyResidentPersonalInventoryEntriesForSave(source, destination);

            Assert.That(destination, Has.Count.EqualTo(2));
            Assert.That(destination[0].itemId, Is.EqualTo("test-a"));
            Assert.That(destination[1].itemId, Is.EqualTo("test-z"));
        }

        [Test]
        public void RestoringOneResidentDoesNotAffectAnotherResident()
        {
            GameObject firstObject = new("First Resident");
            GameObject secondObject = new("Second Resident");
            try
            {
                StrategyResidentItemCatalog catalog = new(new[] { Definition("test-item", 2) });
                StrategyResidentAgent first = firstObject.AddComponent<StrategyResidentAgent>();
                StrategyResidentAgent second = secondObject.AddComponent<StrategyResidentAgent>();
                first.ConfigurePersonalItemCatalog(catalog);
                second.ConfigurePersonalItemCatalog(catalog);

                Assert.That(
                    StrategySaveSystem.TryRestoreResidentPersonalInventory(
                        first,
                        new[] { Item("test-item", 2) },
                        out StrategyResidentPersonalInventoryFailure failure),
                    Is.True,
                    failure.ToString());

                Assert.That(first.GetPersonalItemQuantity("test-item"), Is.EqualTo(2));
                Assert.That(second.GetPersonalItemQuantity("test-item"), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        private static StrategySaveData CreateValidSave()
        {
            StrategySaveData save = new()
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                elapsedSeconds = 10f,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
            save.residents.Add(new StrategyResidentSaveData
            {
                residentId = 11,
                gender = (int)StrategyResidentGender.Female,
                lifeStage = (int)StrategyResidentLifeStage.Adult,
                ageYears = 25f,
                worldX = 12f,
                worldY = 18f
            });
            return save;
        }

        private static StrategyResidentItemDefinition Definition(string id, int maxStack)
        {
            return new StrategyResidentItemDefinition(id, "Test Item", maxStack);
        }

        private static StrategyResidentItemSaveData Item(string id, int quantity)
        {
            return new StrategyResidentItemSaveData
            {
                itemId = id,
                quantity = quantity
            };
        }
    }
}
