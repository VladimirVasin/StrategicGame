using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyResourceQueryServiceTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Resource Query Service Test Root");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PopulateSnapshotFiltersStoresByScope()
        {
            StrategyResourceStore settlement = CreateStore(
                "Settlement Store",
                StrategyResourceStoreScope.Settlement);
            StrategyResourceStore production = CreateStore(
                "Production Store",
                StrategyResourceStoreScope.Production);
            settlement.Add(StrategyResourceType.Logs, 5);
            production.Add(StrategyResourceType.Logs, 7);

            StrategyResourceSnapshot snapshot = new();
            StrategyResourceQueryService.PopulateSnapshot(
                snapshot,
                StrategyResourceStoreScope.Settlement);

            Assert.That(snapshot.GetStored(StrategyResourceType.Logs), Is.EqualTo(5));
            Assert.That(snapshot.GetAvailable(StrategyResourceType.Logs), Is.EqualTo(5));

            StrategyResourceQueryService.PopulateSnapshot(snapshot);

            Assert.That(snapshot.GetStored(StrategyResourceType.Logs), Is.EqualTo(12));
            Assert.That(snapshot.GetAvailable(StrategyResourceType.Logs), Is.EqualTo(12));
        }

        [Test]
        public void PopulateSnapshotSeparatesStoredAndReservedAmounts()
        {
            StrategyResourceStore store = CreateStore(
                "Reserved Store",
                StrategyResourceStoreScope.Settlement);
            store.Add(StrategyResourceType.Stone, 10);
            Assert.That(
                store.TryReserve(
                    new object(),
                    StrategyResourceType.Stone,
                    4,
                    StrategyResourceReservationChannel.Construction,
                    out int reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(4));

            StrategyResourceSnapshot snapshot = new();
            StrategyResourceQueryService.PopulateSnapshot(snapshot);

            Assert.That(snapshot.GetStored(StrategyResourceType.Stone), Is.EqualTo(10));
            Assert.That(snapshot.GetAvailable(StrategyResourceType.Stone), Is.EqualTo(6));
        }

        [Test]
        public void PopulateSnapshotIncludesHousePreparedDishes()
        {
            GameObject houseObject = new("House Resource Store");
            houseObject.transform.SetParent(root.transform, false);
            StrategyHouseResourceStore house = houseObject.AddComponent<StrategyHouseResourceStore>();
            house.ResourceStore.Bind(house, StrategyResourceStoreScope.Household);
            house.AddResource(StrategyResourceType.Dish, 3);

            StrategyResourceSnapshot snapshot = new();
            StrategyResourceQueryService.PopulateSnapshot(
                snapshot,
                StrategyResourceStoreScope.Household);

            Assert.That(house.ResourceStore.GetStored(StrategyResourceType.Dish), Is.Zero);
            Assert.That(snapshot.GetStored(StrategyResourceType.Dish), Is.EqualTo(3));
            Assert.That(snapshot.GetAvailable(StrategyResourceType.Dish), Is.EqualTo(3));
        }

        private StrategyResourceStore CreateStore(
            string ownerName,
            StrategyResourceStoreScope scope)
        {
            GameObject owner = new(ownerName);
            owner.transform.SetParent(root.transform, false);
            StrategyResourceStore store = new();
            store.Bind(owner, scope);
            return store;
        }
    }
}
