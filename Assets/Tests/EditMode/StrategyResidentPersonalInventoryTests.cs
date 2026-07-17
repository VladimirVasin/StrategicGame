using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResidentPersonalInventoryTests
    {
        [Test]
        public void ProductionCatalogStartsEmpty()
        {
            Assert.That(StrategyResidentItemCatalog.Production.Count, Is.Zero);
            Assert.That(StrategyResidentItemCatalog.Production.Definitions, Is.Empty);
        }

        [Test]
        public void IneligibleOwnerCannotReceiveItemsButCanBecomeEligibleLater()
        {
            bool eligible = false;
            StrategyResidentPersonalInventory inventory = new(
                CreateCatalog("test-item"),
                () => eligible);

            Assert.That(
                inventory.TryAddExact(
                    "test-item",
                    1,
                    out StrategyResidentPersonalInventoryFailure blockedFailure),
                Is.False);
            Assert.That(blockedFailure, Is.EqualTo(StrategyResidentPersonalInventoryFailure.OwnerIneligible));
            Assert.That(inventory.DistinctItemCount, Is.Zero);

            eligible = true;

            Assert.That(inventory.TryAddExact("test-item", 1, out _), Is.True);
            Assert.That(inventory.GetQuantity("test-item"), Is.EqualTo(1));
        }

        [Test]
        public void ExactAddNeverPartiallyFillsAStack()
        {
            StrategyResidentPersonalInventory inventory = new(
                CreateCatalog("test-item", maxStack: 3));
            Assert.That(inventory.TryAddExact("test-item", 2, out _), Is.True);
            long versionBeforeFailure = inventory.Version;

            bool added = inventory.TryAddExact(
                "test-item",
                2,
                out StrategyResidentPersonalInventoryFailure failure);

            Assert.That(added, Is.False);
            Assert.That(failure, Is.EqualTo(StrategyResidentPersonalInventoryFailure.StackExceeded));
            Assert.That(inventory.GetQuantity("test-item"), Is.EqualTo(2));
            Assert.That(inventory.Version, Is.EqualTo(versionBeforeFailure));
        }

        [Test]
        public void DistinctItemCapacityIsSixAndExistingStacksRemainUsable()
        {
            List<StrategyResidentItemDefinition> definitions = new();
            for (int i = 0; i < StrategyResidentPersonalInventory.SlotCapacity + 1; i++)
            {
                definitions.Add(Item("test-item-" + i, 2));
            }

            StrategyResidentPersonalInventory inventory = new(new StrategyResidentItemCatalog(definitions));
            for (int i = 0; i < StrategyResidentPersonalInventory.SlotCapacity; i++)
            {
                Assert.That(inventory.TryAddExact("test-item-" + i, 1, out _), Is.True);
            }

            Assert.That(
                inventory.TryAddExact(
                    "test-item-6",
                    1,
                    out StrategyResidentPersonalInventoryFailure capacityFailure),
                Is.False);
            Assert.That(capacityFailure, Is.EqualTo(StrategyResidentPersonalInventoryFailure.CapacityExceeded));
            Assert.That(inventory.TryAddExact("test-item-0", 1, out _), Is.True);
            Assert.That(inventory.GetQuantity("test-item-0"), Is.EqualTo(2));
        }

        [Test]
        public void DifferentResidentsKeepIndependentInventories()
        {
            StrategyResidentItemCatalog catalog = CreateCatalog("test-item", maxStack: 3);
            StrategyResidentPersonalInventory first = new(catalog);
            StrategyResidentPersonalInventory second = new(catalog);

            Assert.That(first.TryAddExact("test-item", 2, out _), Is.True);

            Assert.That(first.GetQuantity("test-item"), Is.EqualTo(2));
            Assert.That(second.GetQuantity("test-item"), Is.Zero);
        }

        [Test]
        public void InvalidRestoreDoesNotMutateExistingState()
        {
            StrategyResidentPersonalInventory inventory = new(
                CreateCatalog("test-item", maxStack: 2));
            Assert.That(inventory.TryAddExact("test-item", 1, out _), Is.True);
            long versionBeforeRestore = inventory.Version;

            bool restored = inventory.TryRestore(
                new[]
                {
                    new StrategyResidentPersonalItemEntry("test-item", 2),
                    new StrategyResidentPersonalItemEntry("unknown-item", 1)
                },
                out StrategyResidentPersonalInventoryFailure failure);

            Assert.That(restored, Is.False);
            Assert.That(failure, Is.EqualTo(StrategyResidentPersonalInventoryFailure.UnknownItem));
            Assert.That(inventory.GetQuantity("test-item"), Is.EqualTo(1));
            Assert.That(inventory.Version, Is.EqualTo(versionBeforeRestore));
        }

        [Test]
        public void SnapshotIsDetachedAndUsesCatalogOrder()
        {
            StrategyResidentItemCatalog catalog = new(new[]
            {
                Item("test-z", 2, sortOrder: 20),
                Item("test-a", 2, sortOrder: 10)
            });
            StrategyResidentPersonalInventory inventory = new(catalog);
            inventory.TryAddExact("test-z", 1, out _);
            inventory.TryAddExact("test-a", 2, out _);

            StrategyResidentPersonalInventorySnapshot snapshot = inventory.CaptureSnapshot();
            inventory.TryRemoveExact("test-a", 1, out _);

            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(snapshot.Entries[0].ItemId, Is.EqualTo("test-a"));
            Assert.That(snapshot.Entries[0].Quantity, Is.EqualTo(2));
            Assert.That(snapshot.Entries[1].ItemId, Is.EqualTo("test-z"));
        }

        [Test]
        public void CatalogRejectsDuplicatesAndInvalidDefinitions()
        {
            Assert.Throws<ArgumentException>(() => new StrategyResidentItemCatalog(new[]
            {
                Item("test-item", 1),
                Item("test-item", 1)
            }));
            Assert.Throws<ArgumentException>(() => Item("Invalid Item", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Item("test-item", 0));
        }

        private static StrategyResidentItemCatalog CreateCatalog(string id, int maxStack = 1)
        {
            return new StrategyResidentItemCatalog(new[] { Item(id, maxStack) });
        }

        private static StrategyResidentItemDefinition Item(
            string id,
            int maxStack,
            int sortOrder = 0)
        {
            return new StrategyResidentItemDefinition(
                id,
                "Test Item",
                maxStack,
                sortOrder: sortOrder);
        }
    }
}
