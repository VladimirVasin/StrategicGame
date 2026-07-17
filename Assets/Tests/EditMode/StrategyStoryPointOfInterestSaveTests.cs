using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyStoryPointOfInterestSaveTests
    {
        private static readonly StrategyStoryPointOfInterestCatalog StoryCatalog = new(
            new[]
            {
                new StrategyStoryPointOfInterestDefinition("story-first", 0, "First", "First body"),
                new StrategyStoryPointOfInterestDefinition("story-second", 1, "Second", "Second body")
            });

        [Test]
        public void Version11MigrationInitializesIndependentStoryState()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 11;
            legacy.storyPointsOfInterest = null;
            legacy.nextStoryPointOfInterestSequenceIndex = 99;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.storyPointsOfInterest, Is.Empty);
            Assert.That(restored.nextStoryPointOfInterestSequenceIndex, Is.Zero);
        }

        [Test]
        public void CurrentVersionRoundTripsResolvedStoryWithoutChangingResourcePoints()
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(new StrategyPointOfInterestSaveData
            {
                stableId = "legacy-neutral",
                cellX = 12,
                cellY = 12,
                resourceKind = (int)StrategyPointOfInterestResourceKind.None,
                investigated = true
            });
            save.storyPointsOfInterest.Add(CreateStoryPoint(
                "story-anchor-a",
                20,
                20,
                StrategyStoryPointOfInterestState.Resolved,
                "story-first",
                0,
                0));
            save.nextStoryPointOfInterestSequenceIndex = 1;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                StrategyCityItemCatalog.Production,
                StrategyResidentItemCatalog.Production,
                StoryCatalog,
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.pointsOfInterest, Has.Count.EqualTo(1));
            Assert.That(restored.storyPointsOfInterest, Has.Count.EqualTo(1));
            Assert.That(restored.storyPointsOfInterest[0].definitionId, Is.EqualTo("story-first"));
        }

        [Test]
        public void CatalogPreflightRejectsUnknownOrReorderedStory()
        {
            StrategySaveData save = CreateValidSave();
            save.storyPointsOfInterest.Add(CreateStoryPoint(
                "story-anchor-a",
                20,
                20,
                StrategyStoryPointOfInterestState.Resolved,
                "story-second",
                0,
                0));
            save.nextStoryPointOfInterestSequenceIndex = 1;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                StrategyCityItemCatalog.Production,
                StrategyResidentItemCatalog.Production,
                StoryCatalog,
                out _,
                out string reason,
                out _);

            Assert.That(loaded, Is.False);
            Assert.That(reason, Does.StartWith("unknown_or_reordered_story_point_"));
        }

        [Test]
        public void CommittedStoryRequiresAnActiveScoutMission()
        {
            StrategySaveData save = CreateValidSave();
            save.storyPointsOfInterest.Add(CreateStoryPoint(
                "story-anchor-a",
                20,
                20,
                StrategyStoryPointOfInterestState.Committed,
                "story-first",
                0,
                7));
            save.nextStoryPointOfInterestSequenceIndex = 1;

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("invalid_story_point_commitment_"));
        }

        private static StrategySaveData CreateValidSave()
        {
            return new StrategySaveData
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                elapsedSeconds = 40f,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
        }

        private static StrategyStoryPointOfInterestSaveData CreateStoryPoint(
            string id,
            int x,
            int y,
            StrategyStoryPointOfInterestState state,
            string definitionId,
            int sequenceIndex,
            int committedResidentId)
        {
            return new StrategyStoryPointOfInterestSaveData
            {
                stableId = id,
                cellX = x,
                cellY = y,
                state = (int)state,
                definitionId = definitionId,
                sequenceIndex = sequenceIndex,
                committedResidentId = committedResidentId
            };
        }
    }
}
