using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPointOfInterestSaveValidationTests
    {
        [Test]
        public void PointOfInterestCannotOverlapCompletedBuilding()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(CreateBuilding(StrategyBuildTool.House, 8, 9));
            save.pointsOfInterest.Add(CreateNeutralPoint(9, 10));

            AssertInvalid(save, "point_of_interest_overlaps_world_0");
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
            save.pointsOfInterest.Add(CreateNeutralPoint(15, 18));

            AssertInvalid(save, "point_of_interest_overlaps_world_0");
        }

        [Test]
        public void NeutralPointOutsideWorldFootprintsRemainsValid()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(CreateBuilding(StrategyBuildTool.House, 8, 9));
            save.pointsOfInterest.Add(CreateNeutralPoint(10, 10));

            AssertValid(save);
        }

        [Test]
        public void Version5MigrationClearsLegacyPointsForOwnedLayoutRegeneration()
        {
            StrategySaveData save = CreateValidSave();
            save.version = 5;
            StrategyPointOfInterestSaveData point = CreateTypedPoint(
                21,
                22,
                StrategyPointOfInterestResourceKind.Iron,
                25,
                22,
                73);
            point.stableId = "poi-legacy";
            point.investigated = true;
            save.pointsOfInterest.Add(point);

            bool migrated = StrategySaveMigration.TryMigrate(save, out string reason);

            Assert.That(migrated, Is.True, reason);
            Assert.That(save.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(save.pointsOfInterest, Is.Empty);
        }

        [Test]
        public void EveryDefinedResourceKindWithConsistentOwnershipIsValid()
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateNeutralPoint(5, 5));
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                20,
                StrategyPointOfInterestResourceKind.Coal,
                14,
                20));
            save.pointsOfInterest.Add(CreateTypedPoint(
                30,
                20,
                StrategyPointOfInterestResourceKind.Iron,
                34,
                20));

            AssertValid(save);
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void UndefinedResourceKindIsRejected(int resourceKind)
        {
            StrategySaveData save = CreateValidSave();
            StrategyPointOfInterestSaveData point = CreateNeutralPoint(10, 10);
            point.resourceKind = resourceKind;
            save.pointsOfInterest.Add(point);

            AssertInvalid(save, "invalid_point_of_interest_resource_kind_0");
        }

        [Test]
        public void TypedPointWithoutOwnedMineralSiteIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            StrategyPointOfInterestSaveData point = CreateNeutralPoint(10, 10);
            point.resourceKind = (int)StrategyPointOfInterestResourceKind.Coal;
            save.pointsOfInterest.Add(point);

            AssertInvalid(save, "invalid_point_of_interest_mineral_site_0");
        }

        [Test]
        public void NeutralPointWithMineralStateIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            StrategyPointOfInterestSaveData point = CreateNeutralPoint(10, 10);
            point.hasMineralSite = true;
            point.mineralOriginX = 14;
            point.mineralOriginY = 10;
            point.remainingMineralAmount = 50;
            save.pointsOfInterest.Add(point);

            AssertInvalid(save, "neutral_point_has_mineral_site_0");
        }

        [TestCase(12)]
        [TestCase(16)]
        public void MineralSiteOutsidePointZoneIsRejected(int mineralOriginX)
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Coal,
                mineralOriginX,
                10));

            AssertInvalid(save, "point_of_interest_mineral_site_out_of_zone_0");
        }

        [Test]
        public void NegativeRemainingMineralAmountIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Iron,
                14,
                10,
                -1));

            AssertInvalid(save, "invalid_point_of_interest_mineral_site_0");
        }

        [TestCase(14)]
        [TestCase(16)]
        public void DuplicateOrTouchingMineralSitesAreRejected(int secondOriginX)
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Coal,
                14,
                10));
            save.pointsOfInterest.Add(CreateTypedPoint(
                20,
                10,
                StrategyPointOfInterestResourceKind.Iron,
                secondOriginX,
                10));

            AssertInvalid(save, "duplicate_point_of_interest_mineral_site_1");
        }

        [Test]
        public void MineralSiteCannotOverlapUnrelatedWorldFootprint()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(CreateBuilding(StrategyBuildTool.House, 14, 10));
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Coal,
                14,
                10));

            AssertInvalid(save, "point_of_interest_mineral_site_overlaps_world_0");
        }

        [TestCase(StrategyPointOfInterestResourceKind.Coal, StrategyBuildTool.CoalPit)]
        [TestCase(StrategyPointOfInterestResourceKind.Iron, StrategyBuildTool.Mine)]
        public void MineralSiteMayOverlapMatchingExtractionBuilding(
            StrategyPointOfInterestResourceKind kind,
            StrategyBuildTool tool)
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(CreateBuilding(tool, 14, 10));
            save.pointsOfInterest.Add(CreateTypedPoint(10, 10, kind, 14, 10));

            AssertValid(save);
        }

        [TestCase(StrategyPointOfInterestResourceKind.Coal, StrategyBuildTool.CoalPit)]
        [TestCase(StrategyPointOfInterestResourceKind.Iron, StrategyBuildTool.Mine)]
        public void MineralSiteMayOverlapMatchingExtractionConstructionSite(
            StrategyPointOfInterestResourceKind kind,
            StrategyBuildTool tool)
        {
            StrategySaveData save = CreateValidSave();
            save.constructionSites.Add(new StrategyConstructionSiteSaveData
            {
                tool = (int)tool,
                originX = 14,
                originY = 10,
                footprintX = 2,
                footprintY = 2
            });
            save.pointsOfInterest.Add(CreateTypedPoint(10, 10, kind, 14, 10));

            AssertValid(save);
        }

        [Test]
        public void MineralSiteInsideCampExclusionIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart.hasStarterCamp = true;
            save.foundingStart.starterCampX = 32;
            save.foundingStart.starterCampY = 32;
            save.pointsOfInterest.Add(CreateTypedPoint(
                36,
                32,
                StrategyPointOfInterestResourceKind.Iron,
                40,
                32));

            AssertInvalid(save, "point_of_interest_mineral_site_out_of_zone_0");
        }

        [Test]
        public void DepletedOwnedMineralSiteAllowsLaterBuildingAndTrailUse()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(CreateBuilding(StrategyBuildTool.House, 14, 10));
            save.trailCells.Add(10 * save.mapWidth + 14);
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Iron,
                14,
                10,
                0));

            AssertValid(save);
        }

        [Test]
        public void CurrentSaveJsonRoundTripPreservesOwnedMineralState()
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreateTypedPoint(
                10,
                10,
                StrategyPointOfInterestResourceKind.Coal,
                14,
                10,
                67));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.pointsOfInterest, Has.Count.EqualTo(1));
            StrategyPointOfInterestSaveData point = restored.pointsOfInterest[0];
            Assert.That(point.resourceKind, Is.EqualTo((int)StrategyPointOfInterestResourceKind.Coal));
            Assert.That(point.hasMineralSite, Is.True);
            Assert.That(point.mineralOriginX, Is.EqualTo(14));
            Assert.That(point.mineralOriginY, Is.EqualTo(10));
            Assert.That(point.remainingMineralAmount, Is.EqualTo(67));
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

        private static StrategyPointOfInterestSaveData CreateNeutralPoint(int x, int y)
        {
            return new StrategyPointOfInterestSaveData
            {
                stableId = $"poi-{x}-{y}",
                cellX = x,
                cellY = y,
                resourceKind = (int)StrategyPointOfInterestResourceKind.None
            };
        }

        private static StrategyPointOfInterestSaveData CreateTypedPoint(
            int cellX,
            int cellY,
            StrategyPointOfInterestResourceKind kind,
            int mineralOriginX,
            int mineralOriginY,
            int remainingAmount = 64)
        {
            return new StrategyPointOfInterestSaveData
            {
                stableId = $"poi-{cellX}-{cellY}",
                cellX = cellX,
                cellY = cellY,
                resourceKind = (int)kind,
                hasMineralSite = true,
                mineralOriginX = mineralOriginX,
                mineralOriginY = mineralOriginY,
                remainingMineralAmount = remainingAmount
            };
        }

        private static StrategyBuildingSaveData CreateBuilding(
            StrategyBuildTool tool,
            int originX,
            int originY)
        {
            return new StrategyBuildingSaveData
            {
                stableId = $"building-{tool}-{originX}-{originY}",
                tool = (int)tool,
                originX = originX,
                originY = originY,
                footprintX = 2,
                footprintY = 2
            };
        }

        private static void AssertValid(StrategySaveData save)
        {
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.True, reason);
        }

        private static void AssertInvalid(StrategySaveData save, string expectedReason)
        {
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Is.EqualTo(expectedReason));
        }
    }
}
