using NUnit.Framework;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPointOfInterestSaveValidationTests
    {
        [Test]
        public void PointOfInterestCannotOverlapCompletedBuilding()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "house-a",
                tool = (int)StrategyBuildTool.House,
                originX = 8,
                originY = 9,
                footprintX = 2,
                footprintY = 2
            });
            save.pointsOfInterest.Add(CreatePoint(9, 10));

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("point_of_interest_overlaps_world_"));
        }

        [Test]
        public void PointOfInterestCannotOverlapConstructionSite()
        {
            StrategySaveData save = CreateValidSave();
            save.constructionSites.Add(new StrategyConstructionSiteSaveData
            {
                tool = (int)StrategyBuildTool.ScoutLodge,
                originX = 14,
                originY = 15,
                footprintX = 2,
                footprintY = 4
            });
            save.pointsOfInterest.Add(CreatePoint(15, 18));

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("point_of_interest_overlaps_world_"));
        }

        [Test]
        public void PointOfInterestOutsideWorldFootprintsRemainsValid()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "house-a",
                tool = (int)StrategyBuildTool.House,
                originX = 8,
                originY = 9,
                footprintX = 2,
                footprintY = 2
            });
            save.pointsOfInterest.Add(CreatePoint(10, 10));

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);
        }

        private static StrategySaveData CreateValidSave()
        {
            return new StrategySaveData
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
        }

        private static StrategyPointOfInterestSaveData CreatePoint(int x, int y)
        {
            return new StrategyPointOfInterestSaveData
            {
                stableId = "poi-" + x + "-" + y,
                cellX = x,
                cellY = y
            };
        }
    }
}
