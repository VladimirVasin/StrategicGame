using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyFirstNightFaunaTests
    {
        [Test]
        public void FirstNightPolicyBlocksFaunaUntilDuskAndCatsUntilStoryCompletion()
        {
            StrategySettlementFaunaTargets organic = new(6, 3, 2, 2, 1);

            StrategySettlementFaunaTargets dormant = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.Dormant,
                ownsCats: true);
            StrategySettlementFaunaTargets miceVisible = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.MiceVisible,
                ownsCats: true);
            StrategySettlementFaunaTargets completedWithoutCats = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.StoryCompleted,
                ownsCats: false);
            StrategySettlementFaunaTargets completedWithCats = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.StoryCompleted,
                ownsCats: true);

            Assert.That(dormant.TargetMice, Is.Zero);
            Assert.That(dormant.TargetCats, Is.Zero);
            Assert.That(miceVisible.TargetMice, Is.EqualTo(3));
            Assert.That(miceVisible.TargetCats, Is.Zero);
            Assert.That(completedWithoutCats.TargetMice, Is.EqualTo(3));
            Assert.That(completedWithoutCats.TargetCats, Is.Zero);
            Assert.That(completedWithCats.TargetMice, Is.EqualTo(3));
            Assert.That(completedWithCats.TargetCats, Is.EqualTo(2));
        }

        [Test]
        public void CompletedPolicyCreatesMinimumPopulationWithoutOrganicUnlocks()
        {
            StrategySettlementFaunaTargets organic = new(1, 0, 1, 0, 0);

            StrategySettlementFaunaTargets completed = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.StoryCompleted,
                ownsCats: true);

            Assert.That(completed.TargetMice, Is.EqualTo(3));
            Assert.That(completed.TargetCats, Is.EqualTo(1));
        }

        [Test]
        public void SettlementFaunaTracksTheConfiguredCityInventoryOnly()
        {
            GameObject root = new("Settlement Fauna Inventory Test");
            GameObject firstInventoryObject = new("First City Inventory Test");
            GameObject secondInventoryObject = new("Second City Inventory Test");
            try
            {
                StrategyCityInventory firstInventory =
                    firstInventoryObject.AddComponent<StrategyCityInventory>();
                StrategyCityInventory secondInventory =
                    secondInventoryObject.AddComponent<StrategyCityInventory>();
                firstInventory.Configure(StrategyCityItemCatalog.Production);
                secondInventory.Configure(StrategyCityItemCatalog.Production);
                StrategySettlementFaunaController fauna =
                    root.AddComponent<StrategySettlementFaunaController>();
                fauna.SetFirstNightStage(StrategyFirstNightFaunaStage.StoryCompleted);
                fauna.Configure(null, null, null, null, firstInventory);

                Assert.That(fauna.OwnsCats, Is.False);
                Assert.That(fauna.Targets.TargetCats, Is.Zero);

                Assert.That(firstInventory.TryAdd(StrategyCityItemIds.Cats, 1), Is.True);
                Assert.That(fauna.OwnsCats, Is.True);
                Assert.That(fauna.Targets.TargetCats, Is.EqualTo(1));

                fauna.Configure(null, null, null, null, secondInventory);
                Assert.That(fauna.OwnsCats, Is.False);
                Assert.That(fauna.Targets.TargetCats, Is.Zero);

                Assert.That(firstInventory.TryRemove(StrategyCityItemIds.Cats, 1), Is.True);
                Assert.That(fauna.OwnsCats, Is.False);
                Assert.That(fauna.Targets.TargetCats, Is.Zero);

                Assert.That(secondInventory.TryAdd(StrategyCityItemIds.Cats, 1), Is.True);
                Assert.That(fauna.OwnsCats, Is.True);
                Assert.That(fauna.Targets.TargetCats, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(firstInventoryObject);
                Object.DestroyImmediate(secondInventoryObject);
            }
        }

        [Test]
        public void StoryCompletionGrantsCatsOnceAndCompletesTheNarrativeStage()
        {
            GameObject root = new("First Night Reward Test");
            GameObject inventoryObject = new("First Night Reward Inventory");
            try
            {
                StrategyCityInventory inventory =
                    inventoryObject.AddComponent<StrategyCityInventory>();
                inventory.Configure(StrategyCityItemCatalog.Production);
                StrategyFirstNightFaunaEventController firstNightEvent =
                    root.AddComponent<StrategyFirstNightFaunaEventController>();
                firstNightEvent.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    inventory,
                    null);
                MethodInfo completeStory = typeof(StrategyFirstNightFaunaEventController)
                    .GetMethod(
                        "HandleStoryCompleted",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(completeStory, Is.Not.Null);

                completeStory.Invoke(firstNightEvent, null);

                Assert.That(
                    inventory.GetQuantity(StrategyCityItemIds.Cats),
                    Is.EqualTo(1));
                Assert.That(
                    firstNightEvent.Stage,
                    Is.EqualTo(StrategyFirstNightFaunaStage.StoryCompleted));
                long inventoryVersion = inventory.Version;

                completeStory.Invoke(firstNightEvent, null);

                Assert.That(
                    inventory.GetQuantity(StrategyCityItemIds.Cats),
                    Is.EqualTo(1));
                Assert.That(inventory.Version, Is.EqualTo(inventoryVersion));
            }
            finally
            {
                StrategyEventLogHudController.ResetSessionState();
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void RestoringACompletedStageSilentlyRepairsMissingCatsEntitlement()
        {
            GameObject root = new("First Night Reward Restore Test");
            GameObject inventoryObject = new("First Night Reward Restore Inventory");
            try
            {
                StrategyCityInventory inventory =
                    inventoryObject.AddComponent<StrategyCityInventory>();
                inventory.Configure(StrategyCityItemCatalog.Production);
                StrategyFirstNightFaunaEventController firstNightEvent =
                    root.AddComponent<StrategyFirstNightFaunaEventController>();
                firstNightEvent.Configure(
                    null,
                    null,
                    null,
                    null,
                    null,
                    inventory,
                    null);

                firstNightEvent.RestoreStage(StrategyFirstNightFaunaStage.StoryCompleted);

                Assert.That(
                    inventory.GetQuantity(StrategyCityItemIds.Cats),
                    Is.EqualTo(1));
                Assert.That(
                    firstNightEvent.Stage,
                    Is.EqualTo(StrategyFirstNightFaunaStage.StoryCompleted));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [Test]
        public void MiceVisibleStageFillsTheFirstNightMinimumInOnePopulationRefresh()
        {
            Assert.That(
                StrategySettlementFaunaPolicy.GetMouseSpawnBudget(
                    StrategyFirstNightFaunaStage.MiceVisible,
                    0,
                    StrategySettlementFaunaPolicy.FirstNightMouseMinimum),
                Is.EqualTo(3));
            Assert.That(
                StrategySettlementFaunaPolicy.GetMouseSpawnBudget(
                    StrategyFirstNightFaunaStage.StoryCompleted,
                    0,
                    StrategySettlementFaunaPolicy.FirstNightMouseMinimum),
                Is.EqualTo(1));
        }

        [Test]
        public void InterruptedRatCinematicRunningFlagIsReleasedForRetry()
        {
            Assert.That(
                StrategyFirstNightFaunaEventController.ShouldRetainRatCinematicRunning(
                    true,
                    true),
                Is.True);
            Assert.That(
                StrategyFirstNightFaunaEventController.ShouldRetainRatCinematicRunning(
                    true,
                    false),
                Is.False);
        }

        [TestCase(0, StrategyTimeOfDayPhase.Afternoon, false, false)]
        [TestCase(0, StrategyTimeOfDayPhase.Dusk, true, false)]
        [TestCase(0, StrategyTimeOfDayPhase.Night, true, true)]
        [TestCase(1, StrategyTimeOfDayPhase.Dawn, true, true)]
        public void FirstNightThresholdsCatchPhaseAndDayTransitions(
            int dayIndex,
            StrategyTimeOfDayPhase phase,
            bool expectedDusk,
            bool expectedNight)
        {
            StrategyCalendarSnapshot snapshot = new(
                dayIndex,
                0f,
                0,
                0,
                phase,
                0f,
                StrategySeason.Spring,
                1,
                1,
                0f);

            Assert.That(
                StrategyFirstNightFaunaEventController.HasReachedFirstDusk(snapshot),
                Is.EqualTo(expectedDusk));
            Assert.That(
                StrategyFirstNightFaunaEventController.HasReachedFirstNight(snapshot),
                Is.EqualTo(expectedNight));
        }

        [Test]
        public void StoryCatalogContainsThreeImportableWideFrames()
        {
            Assert.That(StrategyFirstNightFaunaStoryCatalog.Frames, Has.Length.EqualTo(3));
            for (int i = 0; i < StrategyFirstNightFaunaStoryCatalog.Frames.Length; i++)
            {
                StrategyFoundingStoryPanel panel = StrategyFirstNightFaunaStoryCatalog.Frames[i];
                Sprite sprite = Resources.Load<Sprite>(panel.ResourcePath);
                Assert.That(sprite, Is.Not.Null, panel.ResourcePath);
                Assert.That(sprite.rect.width, Is.EqualTo(1672f), panel.ResourcePath);
                Assert.That(sprite.rect.height, Is.EqualTo(941f), panel.ResourcePath);
            }
        }
    }
}
