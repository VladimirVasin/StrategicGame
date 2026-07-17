using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCityInventoryTests
    {
        private GameObject inventoryObject;
        private StrategyCityInventory inventory;

        [SetUp]
        public void SetUp()
        {
            inventoryObject = new GameObject("City Inventory Test");
            inventory = inventoryObject.AddComponent<StrategyCityInventory>();
            inventory.Configure(CreateCatalog());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(inventoryObject);
        }

        [Test]
        public void ProductionCatalogStartsEmpty()
        {
            Assert.That(StrategyCityItemCatalog.Production.Count, Is.Zero);

            GameObject productionObject = new("Production Inventory Test");
            try
            {
                StrategyCityInventory productionInventory =
                    productionObject.AddComponent<StrategyCityInventory>();

                Assert.That(productionInventory.DistinctItemCount, Is.Zero);
                Assert.That(productionInventory.TotalQuantity, Is.Zero);
                Assert.That(productionInventory.CaptureSnapshot().Count, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(productionObject);
            }
        }

        [Test]
        public void ItemDefinitionsRequireStableLowercaseIdsAndPositiveStacks()
        {
            Assert.Throws<ArgumentException>(() => Item("UpperCase", 2));
            Assert.Throws<ArgumentException>(() => Item("-leading", 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Item("valid", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Item("too-large", StrategyCityItemDefinition.MaximumQuantity + 1));

            StrategyCityItemDefinition definition = Item("map.fragment_1", 2);
            Assert.That(definition.Id, Is.EqualTo("map.fragment_1"));
        }

        [Test]
        public void CatalogRejectsDuplicateIdsAndUsesDeterministicOrder()
        {
            Assert.Throws<ArgumentException>(() => new StrategyCityItemCatalog(new[]
            {
                Item("duplicate", 1),
                Item("duplicate", 3)
            }));

            StrategyCityItemCatalog catalog = new(new[]
            {
                Item("z-last", 1, sortOrder: 20),
                Item("b-second", 1, sortOrder: 10),
                Item("a-first", 1, sortOrder: 10)
            });

            Assert.That(
                new[]
                {
                    catalog.Definitions[0].Id,
                    catalog.Definitions[1].Id,
                    catalog.Definitions[2].Id
                },
                Is.EqualTo(new[] { "a-first", "b-second", "z-last" }));
        }

        [Test]
        public void TryAddMergesAndCapsStackWithOneNotificationPerMutation()
        {
            int notifications = 0;
            inventory.Changed += () => notifications++;

            Assert.That(inventory.TryAdd("relic", 3, out int firstAdded), Is.True);
            Assert.That(firstAdded, Is.EqualTo(3));
            Assert.That(inventory.TryAdd("relic", 10, out int secondAdded), Is.True);
            Assert.That(secondAdded, Is.EqualTo(2));
            Assert.That(inventory.TryAdd("relic", 1, out int rejectedAdded), Is.False);

            Assert.That(rejectedAdded, Is.Zero);
            Assert.That(inventory.GetQuantity("relic"), Is.EqualTo(5));
            Assert.That(inventory.Version, Is.EqualTo(2));
            Assert.That(notifications, Is.EqualTo(2));
        }

        [Test]
        public void InvalidOrUnknownAddsDoNotMutateInventory()
        {
            int notifications = 0;
            inventory.Changed += () => notifications++;

            Assert.That(inventory.TryAdd("missing", 1), Is.False);
            Assert.That(inventory.TryAdd("relic", 0), Is.False);
            Assert.That(inventory.TryAdd(null, 1), Is.False);

            Assert.That(inventory.Version, Is.Zero);
            Assert.That(notifications, Is.Zero);
            Assert.That(inventory.DistinctItemCount, Is.Zero);
        }

        [Test]
        public void TryRemoveSupportsPartialAndFullRemoval()
        {
            inventory.TryAdd("relic", 5);
            int notifications = 0;
            inventory.Changed += () => notifications++;

            Assert.That(inventory.TryRemove("relic", 2, out int firstRemoved), Is.True);
            Assert.That(firstRemoved, Is.EqualTo(2));
            Assert.That(inventory.TryRemove("relic", 99, out int secondRemoved), Is.True);
            Assert.That(secondRemoved, Is.EqualTo(3));
            Assert.That(inventory.TryRemove("relic", 1), Is.False);

            Assert.That(inventory.Contains("relic"), Is.False);
            Assert.That(inventory.DistinctItemCount, Is.Zero);
            Assert.That(inventory.Version, Is.EqualTo(3));
            Assert.That(notifications, Is.EqualTo(2));
        }

        [Test]
        public void SnapshotAndCopyOwnedItemsAreDeterministicAndDetached()
        {
            inventory.TryAdd("relic", 2);
            inventory.TryAdd("charter", 1);
            StrategyCityInventorySnapshot snapshot = inventory.CaptureSnapshot();
            List<StrategyCityInventoryEntry> copy = new()
            {
                new StrategyCityInventoryEntry("stale", 99)
            };

            inventory.CopyOwnedItems(copy);
            inventory.TryRemove("relic", 1);

            Assert.That(snapshot.Version, Is.EqualTo(2));
            AssertEntries(snapshot.Entries, Entry("charter", 1), Entry("relic", 2));
            AssertEntries(copy, Entry("charter", 1), Entry("relic", 2));
            Assert.That(inventory.GetQuantity("relic"), Is.EqualTo(1));
        }

        [TestCase("unknown", 1, StrategyCityInventoryRestoreFailure.UnknownItem)]
        [TestCase("relic", 0, StrategyCityInventoryRestoreFailure.InvalidQuantity)]
        [TestCase("relic", 6, StrategyCityInventoryRestoreFailure.InvalidQuantity)]
        public void InvalidRestoreDoesNotPartiallyMutate(
            string invalidId,
            int invalidQuantity,
            StrategyCityInventoryRestoreFailure expectedFailure)
        {
            inventory.TryAdd("charter", 1);
            long versionBeforeRestore = inventory.Version;
            int notifications = 0;
            inventory.Changed += () => notifications++;
            StrategyCityInventoryEntry[] entries =
            {
                Entry("relic", 2),
                Entry(invalidId, invalidQuantity)
            };

            bool restored = inventory.TryRestore(entries, out StrategyCityInventoryRestoreFailure failure);

            Assert.That(restored, Is.False);
            Assert.That(failure, Is.EqualTo(expectedFailure));
            Assert.That(inventory.GetQuantity("charter"), Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("relic"), Is.Zero);
            Assert.That(inventory.Version, Is.EqualTo(versionBeforeRestore));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void DuplicateRestoreDoesNotPartiallyMutate()
        {
            inventory.TryAdd("charter", 1);
            long versionBeforeRestore = inventory.Version;

            bool restored = inventory.TryRestore(
                new[] { Entry("relic", 1), Entry("relic", 2) },
                out StrategyCityInventoryRestoreFailure failure);

            Assert.That(restored, Is.False);
            Assert.That(failure, Is.EqualTo(StrategyCityInventoryRestoreFailure.DuplicateItem));
            Assert.That(inventory.GetQuantity("charter"), Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("relic"), Is.Zero);
            Assert.That(inventory.Version, Is.EqualTo(versionBeforeRestore));
        }

        [Test]
        public void ValidRestoreReplacesStateWithOneNotification()
        {
            inventory.TryAdd("relic", 1);
            int notifications = 0;
            inventory.Changed += () => notifications++;

            bool restored = inventory.Restore(new[]
            {
                Entry("charter", 1),
                Entry("relic", 4)
            });

            Assert.That(restored, Is.True);
            Assert.That(inventory.GetQuantity("charter"), Is.EqualTo(1));
            Assert.That(inventory.GetQuantity("relic"), Is.EqualTo(4));
            Assert.That(inventory.Version, Is.EqualTo(2));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test]
        public void IdenticalRestoreSucceedsWithoutNotification()
        {
            StrategyCityInventoryEntry[] entries =
            {
                Entry("relic", 2),
                Entry("charter", 1)
            };
            Assert.That(inventory.Restore(entries), Is.True);
            long versionBeforeRestore = inventory.Version;
            int notifications = 0;
            inventory.Changed += () => notifications++;

            Assert.That(inventory.Restore(entries), Is.True);

            Assert.That(inventory.Version, Is.EqualTo(versionBeforeRestore));
            Assert.That(notifications, Is.Zero);
        }

        private static StrategyCityItemCatalog CreateCatalog()
        {
            return new StrategyCityItemCatalog(new[]
            {
                Item("relic", 5, sortOrder: 20),
                Item("charter", 1, sortOrder: 10)
            });
        }

        private static StrategyCityItemDefinition Item(
            string id,
            int maxStack,
            int sortOrder = 0)
        {
            return new StrategyCityItemDefinition(id, id, maxStack, sortOrder: sortOrder);
        }

        private static StrategyCityInventoryEntry Entry(string id, int quantity)
        {
            return new StrategyCityInventoryEntry(id, quantity);
        }

        private static void AssertEntries(
            IReadOnlyList<StrategyCityInventoryEntry> actual,
            params StrategyCityInventoryEntry[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));
            for (int index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index], Is.EqualTo(expected[index]));
            }
        }
    }
}
