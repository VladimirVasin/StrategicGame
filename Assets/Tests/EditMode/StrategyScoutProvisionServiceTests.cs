using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.Tests
{
    public sealed class StrategyScoutProvisionServiceTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            StrategyScoutProvisionService.GetAvailableRations();
            root = new GameObject("Scout Provision Tests");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            StrategyScoutProvisionService.GetAvailableRations();
        }

        [Test]
        public void AvailabilityIncludesOnlySupplyScopesAndRespectsReservations()
        {
            StrategyScoutProvisionTestStore settlement = CreateStore(
                "Settlement",
                StrategyResourceStoreScope.Settlement);
            StrategyScoutProvisionTestStore temporary = CreateStore(
                "Temporary",
                StrategyResourceStoreScope.TemporarySettlement);
            StrategyScoutProvisionTestStore production = CreateStore(
                "Production",
                StrategyResourceStoreScope.Production);
            StrategyScoutProvisionTestStore household = CreateStore(
                "Household",
                StrategyResourceStoreScope.Household);
            StrategyScoutProvisionTestStore loose = CreateStore(
                "Loose",
                StrategyResourceStoreScope.Loose);
            StrategyScoutProvisionTestStore mixedExcluded = CreateStore(
                "Mixed Excluded",
                StrategyResourceStoreScope.Settlement
                    | StrategyResourceStoreScope.Household);

            settlement.ResourceStore.Add(StrategyResourceType.Game, 1);
            temporary.ResourceStore.Add(StrategyResourceType.Berries, 4);
            production.ResourceStore.Add(StrategyResourceType.Eggs, 2);
            household.ResourceStore.Add(StrategyResourceType.Fish, 3);
            loose.ResourceStore.Add(StrategyResourceType.Fish, 3);
            mixedExcluded.ResourceStore.Add(StrategyResourceType.Fish, 3);
            Assert.That(
                production.ResourceStore.TryReserve(
                    new object(),
                    StrategyResourceType.Eggs,
                    1,
                    StrategyResourceReservationChannel.ProductionInput,
                    out int reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(1));

            Assert.That(
                StrategyScoutProvisionService.GetAvailableRations(),
                Is.EqualTo(3.2f).Within(0.001f));
        }

        [Test]
        public void TakeUsesMinimumSufficientWholeUnitPlanDeterministically()
        {
            StrategyScoutProvisionTestStore store = CreateStore(
                "Settlement",
                StrategyResourceStoreScope.Settlement);
            store.ResourceStore.Add(StrategyResourceType.Game, 1);
            store.ResourceStore.Add(StrategyResourceType.Fish, 1);

            bool taken = StrategyScoutProvisionService.TryTakeRations(
                1f,
                new object(),
                out float supplied);

            Assert.That(taken, Is.True);
            Assert.That(supplied, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Fish), Is.Zero);
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Game), Is.EqualTo(1));
            Assert.That(store.ExternalTakeNotifications, Is.EqualTo(1));
        }

        [Test]
        public void ExactCombinationWinsOverAnOversupplyingUnit()
        {
            StrategyScoutProvisionTestStore store = CreateStore(
                "Settlement",
                StrategyResourceStoreScope.Settlement);
            store.ResourceStore.Add(StrategyResourceType.Berries, 4);
            store.ResourceStore.Add(StrategyResourceType.Fish, 1);

            bool taken = StrategyScoutProvisionService.TryTakeRations(
                1f,
                new object(),
                out float supplied);

            Assert.That(taken, Is.True);
            Assert.That(supplied, Is.EqualTo(1f).Within(0.001f));
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Berries), Is.Zero);
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Fish), Is.EqualTo(1));
            Assert.That(store.ExternalTakeNotifications, Is.EqualTo(1));
        }

        [Test]
        public void ExistingReservationCannotBeConsumedByExpedition()
        {
            StrategyScoutProvisionTestStore store = CreateStore(
                "Production",
                StrategyResourceStoreScope.Production);
            store.ResourceStore.Add(StrategyResourceType.Fish, 2);
            object householdOwner = new();
            Assert.That(
                store.ResourceStore.TryReserve(
                    householdOwner,
                    StrategyResourceType.Fish,
                    1,
                    StrategyResourceReservationChannel.Household,
                    out int reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(1));

            bool taken = StrategyScoutProvisionService.TryTakeRations(
                1f,
                new object(),
                out float supplied);

            Assert.That(taken, Is.True);
            Assert.That(supplied, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Fish), Is.EqualTo(1));
            Assert.That(store.ResourceStore.GetReserved(StrategyResourceType.Fish), Is.EqualTo(1));
            Assert.That(store.ResourceStore.GetAvailable(StrategyResourceType.Fish), Is.Zero);
        }

        [Test]
        public void InsufficientSupplyLeavesEveryStoreUntouched()
        {
            StrategyScoutProvisionTestStore settlement = CreateStore(
                "Settlement",
                StrategyResourceStoreScope.Settlement);
            StrategyScoutProvisionTestStore household = CreateStore(
                "Household",
                StrategyResourceStoreScope.Household);
            settlement.ResourceStore.Add(StrategyResourceType.Berries, 3);
            household.ResourceStore.Add(StrategyResourceType.Game, 10);

            bool taken = StrategyScoutProvisionService.TryTakeRations(
                1f,
                new object(),
                out float supplied);

            Assert.That(taken, Is.False);
            Assert.That(supplied, Is.Zero);
            Assert.That(settlement.ResourceStore.GetStored(StrategyResourceType.Berries), Is.EqualTo(3));
            Assert.That(household.ResourceStore.GetStored(StrategyResourceType.Game), Is.EqualTo(10));
            Assert.That(settlement.ExternalTakeNotifications, Is.Zero);
            Assert.That(household.ExternalTakeNotifications, Is.Zero);
        }

        [Test]
        public void PartialReservationRollsBackWithoutTakingStock()
        {
            StrategyScoutProvisionTestStore store = CreateStore(
                "Settlement",
                StrategyResourceStoreScope.Settlement);
            store.ResourceStore.Add(StrategyResourceType.Berries, 4);
            store.InjectReservationAfterFirstAvailabilityRead(
                StrategyResourceType.Berries,
                1);
            object expeditionOwner = new();

            bool taken = StrategyScoutProvisionService.TryTakeRations(
                1f,
                expeditionOwner,
                out float supplied);

            Assert.That(taken, Is.False);
            Assert.That(supplied, Is.Zero);
            Assert.That(store.ResourceStore.GetStored(StrategyResourceType.Berries), Is.EqualTo(4));
            Assert.That(store.ExternalTakeNotifications, Is.Zero);

            store.ClearInjectedReservation();
            object probeOwner = new();
            Assert.That(
                store.ResourceStore.TryReserve(
                    probeOwner,
                    StrategyResourceType.Berries,
                    4,
                    StrategyResourceReservationChannel.Expedition,
                    out int reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(4));
            store.ResourceStore.Release(
                probeOwner,
                StrategyResourceType.Berries,
                StrategyResourceReservationChannel.Expedition);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void InvalidRequestIsRejected(float request)
        {
            Assert.That(
                StrategyScoutProvisionService.TryTakeRations(
                    request,
                    new object(),
                    out float supplied),
                Is.False);
            Assert.That(supplied, Is.Zero);
        }

        [Test]
        public void NullReservationOwnerIsRejected()
        {
            Assert.That(
                StrategyScoutProvisionService.TryTakeRations(
                    1f,
                    null,
                    out float supplied),
                Is.False);
            Assert.That(supplied, Is.Zero);
        }

        private StrategyScoutProvisionTestStore CreateStore(
            string objectName,
            StrategyResourceStoreScope scope)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(root.transform, false);
            StrategyScoutProvisionTestStore owner =
                child.AddComponent<StrategyScoutProvisionTestStore>();
            owner.Bind(scope);
            return owner;
        }
    }

    public sealed class StrategyScoutProvisionTestStore : MonoBehaviour,
        IStrategyResourceStoreOwner,
        IStrategyResourceReservationProvider,
        IStrategyExternalResourceTakeObserver
    {
        private readonly StrategyResourceStore resourceStore = new();
        private StrategyResourceType injectedResource;
        private int injectedReservation;
        private int injectedResourceReads;

        public StrategyResourceStore ResourceStore => resourceStore;
        public int ExternalTakeNotifications { get; private set; }

        public void Bind(StrategyResourceStoreScope scope)
        {
            resourceStore.Bind(this, scope);
        }

        public void InjectReservationAfterFirstAvailabilityRead(
            StrategyResourceType resource,
            int amount)
        {
            injectedResource = resource;
            injectedReservation = amount;
            injectedResourceReads = 0;
        }

        public void ClearInjectedReservation()
        {
            injectedResource = StrategyResourceType.None;
            injectedReservation = 0;
            injectedResourceReads = 0;
        }

        public int GetReservedResourceAmount(StrategyResourceType resource)
        {
            if (resource != injectedResource || injectedReservation <= 0)
            {
                return 0;
            }

            injectedResourceReads++;
            return injectedResourceReads > 1 ? injectedReservation : 0;
        }

        public void OnExternalResourceTaken()
        {
            ExternalTakeNotifications++;
        }
    }
}
