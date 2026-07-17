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
                StrategyFirstNightFaunaStage.Dormant);
            StrategySettlementFaunaTargets miceVisible = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.MiceVisible);
            StrategySettlementFaunaTargets completed = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.StoryCompleted);

            Assert.That(dormant.TargetMice, Is.Zero);
            Assert.That(dormant.TargetCats, Is.Zero);
            Assert.That(miceVisible.TargetMice, Is.EqualTo(3));
            Assert.That(miceVisible.TargetCats, Is.Zero);
            Assert.That(completed.TargetMice, Is.EqualTo(3));
            Assert.That(completed.TargetCats, Is.EqualTo(2));
        }

        [Test]
        public void CompletedPolicyCreatesMinimumPopulationWithoutOrganicUnlocks()
        {
            StrategySettlementFaunaTargets organic = new(1, 0, 1, 0, 0);

            StrategySettlementFaunaTargets completed = StrategySettlementFaunaPolicy.ApplyFirstNightStage(
                organic,
                StrategyFirstNightFaunaStage.StoryCompleted);

            Assert.That(completed.TargetMice, Is.EqualTo(3));
            Assert.That(completed.TargetCats, Is.EqualTo(1));
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
