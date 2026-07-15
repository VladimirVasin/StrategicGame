using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyProcessingStorageTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Processing Storage Test Root");
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
        public void SawmillKeepsFullLogsAndPlanksInIndependentPools()
        {
            StrategySawmill sawmill = CreateSawmill("Sawmill");
            sawmill.AddPlanks(StrategyProductionStorage.ProcessingOutputCapacity);
            DeliverInput(
                sawmill,
                StrategyResourceType.Logs,
                StrategyProductionStorage.ProcessingInputCapacity);

            AssertFullPools(
                sawmill.ResourceStore,
                sawmill.InputStorageUsed,
                sawmill.OutputStorageUsed);
            Assert.That(sawmill.LogsStored, Is.EqualTo(6));
            Assert.That(sawmill.PlanksStored, Is.EqualTo(6));
            Assert.That(sawmill.CanStartWorkCycle(), Is.False);
            Assert.That(sawmill.GetHudStatusText(), Does.Contain("Input Storage: 6/6"));
            Assert.That(sawmill.GetHudStatusText(), Does.Contain("Output Storage: 6/6"));
        }

        [Test]
        public void KilnKeepsFullInputsAndPotteryInIndependentPools()
        {
            StrategyKiln kiln = CreateKiln("Kiln");
            kiln.AddPottery(StrategyProductionStorage.ProcessingOutputCapacity);
            DeliverInput(kiln, StrategyResourceType.Clay, 4);
            DeliverInput(kiln, StrategyResourceType.Coal, 2);

            AssertFullPools(kiln.ResourceStore, kiln.InputStorageUsed, kiln.OutputStorageUsed);
            Assert.That(kiln.ClayStored, Is.EqualTo(4));
            Assert.That(kiln.CoalStored, Is.EqualTo(2));
            Assert.That(kiln.PotteryStored, Is.EqualTo(6));
            Assert.That(kiln.CanStartWorkCycle(), Is.False);
            Assert.That(kiln.GetHudStatusText(), Does.Contain("Input Storage: 6/6"));
            Assert.That(kiln.GetHudStatusText(), Does.Contain("Output Storage: 6/6"));
        }

        [Test]
        public void ForgeKeepsFullInputsAndToolsInIndependentPools()
        {
            StrategyForge forge = CreateForge("Forge");
            forge.AddTools(StrategyProductionStorage.ProcessingOutputCapacity);
            DeliverInput(forge, StrategyResourceType.Iron, 2);
            DeliverInput(forge, StrategyResourceType.Coal, 2);
            DeliverInput(forge, StrategyResourceType.Logs, 2);

            AssertFullPools(forge.ResourceStore, forge.InputStorageUsed, forge.OutputStorageUsed);
            Assert.That(forge.IronStored, Is.EqualTo(2));
            Assert.That(forge.CoalStored, Is.EqualTo(2));
            Assert.That(forge.LogsStored, Is.EqualTo(2));
            Assert.That(forge.ToolsStored, Is.EqualTo(6));
            Assert.That(forge.CanStartWorkCycle(), Is.False);
            Assert.That(forge.GetHudStatusText(), Does.Contain("Input Storage: 6/6"));
            Assert.That(forge.GetHudStatusText(), Does.Contain("Output Storage: 6/6"));
        }

        [Test]
        public void PendingOutputBlocksAnotherCycleWithoutBlockingInputDelivery()
        {
            StrategySawmill sawmill = CreateSawmill("Pending Output Sawmill");
            sawmill.AddPlanks(4);
            DeliverInput(sawmill, StrategyResourceType.Logs, 6);

            Assert.That(sawmill.TryConsumeLogForWork(out int expectedPlanks), Is.True);
            Assert.That(expectedPlanks, Is.EqualTo(2));
            Assert.That(sawmill.OutputStorageUsed, Is.EqualTo(4));
            Assert.That(sawmill.ReservedOutputStorageUsed, Is.EqualTo(6));
            Assert.That(sawmill.CanStartWorkCycle(), Is.False);

            DeliverInput(sawmill, StrategyResourceType.Logs, 1);
            Assert.That(sawmill.InputStorageUsed, Is.EqualTo(6));
            Assert.That(sawmill.ReservedOutputStorageUsed, Is.EqualTo(6));

            sawmill.AddPlanks(expectedPlanks);
            Assert.That(sawmill.PendingPlanksForDemolition, Is.Zero);
            Assert.That(sawmill.OutputStorageUsed, Is.EqualTo(6));
        }

        [Test]
        public void SaveRoundTripAndDemolitionManifestPreserveBothPools()
        {
            StrategySawmill source = CreateSawmill("Saved Sawmill");
            source.AddPlanks(6);
            DeliverInput(source, StrategyResourceType.Logs, 6);

            StrategySaveData save = CreateValidSave();
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "sawmill-storage-test",
                tool = (int)StrategyBuildTool.Sawmill,
                originX = 8,
                originY = 8,
                footprintX = 3,
                footprintY = 2,
                resourceAmounts = source.ResourceStore.CaptureAmounts()
            });

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restoredSave,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            StrategySawmill restored = CreateSawmill("Restored Sawmill");
            restored.ResourceStore.RestoreAmounts(restoredSave.buildings[0].resourceAmounts);
            AssertFullPools(
                restored.ResourceStore,
                restored.InputStorageUsed,
                restored.OutputStorageUsed);

            StrategyBuildingResourceManifest manifest =
                StrategyBuildingResourceManifest.Capture(new[] { restored.ResourceStore });
            Assert.That(manifest.GetAmount(StrategyResourceType.Logs), Is.EqualTo(6));
            Assert.That(manifest.GetAmount(StrategyResourceType.Planks), Is.EqualTo(6));
            Assert.That(manifest.TotalAmount, Is.EqualTo(12));

            manifest.ClearCapturedStores();
            Assert.That(restored.ResourceStore.TotalStored, Is.Zero);
        }

        private StrategySawmill CreateSawmill(string name)
        {
            StrategySawmill sawmill = CreateComponent<StrategySawmill>(name);
            sawmill.Configure(null, null, null);
            return sawmill;
        }

        private StrategyKiln CreateKiln(string name)
        {
            StrategyKiln kiln = CreateComponent<StrategyKiln>(name);
            kiln.Configure(null, null, null);
            return kiln;
        }

        private StrategyForge CreateForge(string name)
        {
            StrategyForge forge = CreateComponent<StrategyForge>(name);
            forge.Configure(null, null, null);
            return forge;
        }

        private T CreateComponent<T>(string name) where T : MonoBehaviour
        {
            GameObject child = new(name);
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }

        private static void DeliverInput(
            IStrategyProductionLogisticsNode node,
            StrategyResourceType resource,
            int amount)
        {
            object owner = new();
            Assert.That(
                node.TryReserveInputDelivery(resource, owner, amount, out int reserved),
                Is.True);
            Assert.That(reserved, Is.EqualTo(amount));
            Assert.That(
                node.TryAcceptInputDelivery(resource, owner, reserved, out int accepted),
                Is.True);
            Assert.That(accepted, Is.EqualTo(amount));
        }

        private static void AssertFullPools(
            StrategyResourceStore store,
            int inputStorageUsed,
            int outputStorageUsed)
        {
            Assert.That(inputStorageUsed, Is.EqualTo(StrategyProductionStorage.ProcessingInputCapacity));
            Assert.That(outputStorageUsed, Is.EqualTo(StrategyProductionStorage.ProcessingOutputCapacity));
            Assert.That(store.TotalStored, Is.EqualTo(StrategyProductionStorage.ProcessingTotalCapacity));
            Assert.That(store.Capacity, Is.EqualTo(StrategyProductionStorage.ProcessingTotalCapacity));
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
