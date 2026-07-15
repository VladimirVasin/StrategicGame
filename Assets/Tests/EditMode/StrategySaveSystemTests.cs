using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategySaveSystemTests
    {
        [Test]
        public void Version1MigratesToCurrentVersion()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 1;
            legacy.buildings[0].preparedDishIds = null;
            legacy.buildings[0].preparedDishAmounts = null;
            legacy.pointsOfInterest = null;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy), out StrategySaveData restored, out string reason, out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.buildings[0].preparedDishIds, Is.Not.Null);
            Assert.That(restored.buildings[0].preparedDishAmounts, Is.Not.Null);
            Assert.That(restored.foundingStart, Is.Not.Null);
            Assert.That(restored.foundingStart.hasStarterCamp, Is.False);
            Assert.That(restored.foundingStart.answers, Is.Empty);
            Assert.That(restored.pointsOfInterest, Is.Empty);
        }

        [Test]
        public void Version2MigratesToNeutralFoundingStart()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 2;
            legacy.foundingStart = CreateFoundingStart();
            legacy.pointsOfInterest = null;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy), out StrategySaveData restored, out string reason, out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.foundingStart.hasStarterCamp, Is.False);
            Assert.That(restored.foundingStart.hasStarterCartOrigin, Is.False);
            Assert.That(restored.foundingStart.profileVersion, Is.Zero);
            Assert.That(restored.foundingStart.profileId, Is.Empty);
            Assert.That(restored.foundingStart.answers, Is.Empty);
            Assert.That(restored.pointsOfInterest, Is.Empty);
        }

        [Test]
        public void Version3MigratesWithEmptyPointsOfInterest()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 3;
            legacy.pointsOfInterest = null;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy), out StrategySaveData restored, out string reason, out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.pointsOfInterest, Is.Empty);
        }

        [Test]
        public void FoundingStartDataRoundTripsWithStableAnswerPairs()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart = CreateFoundingStart();
            save.pointsOfInterest.Add(CreatePointOfInterest("poi-roundtrip", 12, 13));

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(save),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.foundingStart.hasStarterCamp, Is.True);
            Assert.That(restored.foundingStart.starterCampX, Is.EqualTo(22));
            Assert.That(restored.foundingStart.starterCampY, Is.EqualTo(31));
            Assert.That(restored.foundingStart.hasStarterCartOrigin, Is.True);
            Assert.That(restored.foundingStart.starterCartOriginX, Is.EqualTo(24));
            Assert.That(restored.foundingStart.starterCartOriginY, Is.EqualTo(30));
            Assert.That(restored.foundingStart.profileVersion, Is.EqualTo(1));
            Assert.That(restored.foundingStart.profileId, Is.EqualTo("founding-v1"));
            Assert.That(restored.foundingStart.answers, Has.Count.EqualTo(2));
            Assert.That(restored.foundingStart.answers[0].questionId, Is.EqualTo("water"));
            Assert.That(restored.foundingStart.answers[0].answerId, Is.EqualTo("near-river"));
            Assert.That(restored.pointsOfInterest, Has.Count.EqualTo(1));
            Assert.That(restored.pointsOfInterest[0].stableId, Is.EqualTo("poi-roundtrip"));
            Assert.That(restored.pointsOfInterest[0].investigated, Is.True);
        }

        [Test]
        public void FoundingStartCoordinatesMustFitTheSavedMap()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart = CreateFoundingStart();
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string validReason), Is.True, validReason);

            save.foundingStart.starterCampX = save.mapWidth;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string campReason), Is.False);
            Assert.That(campReason, Is.EqualTo("invalid_starter_camp_cell"));

            save.foundingStart.starterCampX = 22;
            save.foundingStart.starterCartOriginX = save.mapWidth - 2;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string cartReason), Is.False);
            Assert.That(cartReason, Is.EqualTo("invalid_starter_cart_origin"));

            save.foundingStart.starterCartOriginX = 24;
            save.foundingStart.starterCartOriginY = save.mapHeight - 2;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reservedRowReason), Is.False);
            Assert.That(reservedRowReason, Is.EqualTo("invalid_starter_cart_origin"));
        }

        [Test]
        public void FoundingAnswersRequireUniqueQuestionsAndStableIds()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart = CreateFoundingStart();
            save.foundingStart.answers.Add(new StrategyFoundingAnswerSaveData
            {
                questionId = "water",
                answerId = "inland"
            });

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string duplicateReason), Is.False);
            Assert.That(duplicateReason, Is.EqualTo("invalid_founding_answer_2"));

            save.foundingStart.answers[2].questionId = "unsafe question id";
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string invalidIdReason), Is.False);
            Assert.That(invalidIdReason, Is.EqualTo("invalid_founding_answer_2"));
        }

        [Test]
        public void PendingFoundingStartDataIsReturnedAsADefensiveCopy()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart = CreateFoundingStart();
            StrategySaveSystem.PreparePendingLoad(save);
            try
            {
                Assert.That(StrategySaveSystem.TryGetPendingFoundingStartData(out StrategyFoundingStartSaveData first), Is.True);
                first.starterCampX = 1;
                first.answers[0].answerId = "inland";

                Assert.That(StrategySaveSystem.TryGetPendingFoundingStartData(out StrategyFoundingStartSaveData second), Is.True);
                Assert.That(second.starterCampX, Is.EqualTo(22));
                Assert.That(second.answers[0].answerId, Is.EqualTo("near-river"));
            }
            finally
            {
                StrategySaveSystem.ClearPendingLoad();
            }
        }

        [Test]
        public void NeutralFoundingDataStillMarksPendingContinueBootstrap()
        {
            StrategySaveData save = CreateValidSave();
            save.foundingStart = new StrategyFoundingStartSaveData();
            StrategySaveSystem.PreparePendingLoad(save);
            try
            {
                Assert.That(
                    StrategySaveSystem.TryGetPendingFoundingStartData(out StrategyFoundingStartSaveData data),
                    Is.True);
                Assert.That(data.hasStarterCamp, Is.False);
                Assert.That(data.profileVersion, Is.Zero);
            }
            finally
            {
                StrategySaveSystem.ClearPendingLoad();
            }
        }

        [Test]
        public void FutureVersionIsRejected()
        {
            StrategySaveData future = CreateValidSave();
            future.version = StrategySaveData.CurrentVersion + 1;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(future),
                out _,
                out string reason,
                out _);

            Assert.That(loaded, Is.False);
            Assert.That(reason, Is.EqualTo("unsupported_version_" + future.version));
        }

        [Test]
        public void DuplicateBuildingIdentifiersAreRejected()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = save.buildings[0].stableId,
                tool = (int)StrategyBuildTool.Granary,
                originX = 8,
                originY = 8,
                footprintX = 2,
                footprintY = 2
            });

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("invalid_or_duplicate_building_id_"));
        }

        [TestCase("poi-a", 11, 11, "invalid_or_duplicate_point_of_interest_id_")]
        [TestCase("poi-b", 10, 10, "invalid_or_duplicate_point_of_interest_cell_")]
        [TestCase("poi-b", 64, 11, "invalid_or_duplicate_point_of_interest_cell_")]
        public void InvalidPointOfInterestIdentifiersAndCellsAreRejected(
            string secondId, int secondX, int secondY, string reasonPrefix)
        {
            StrategySaveData save = CreateValidSave();
            save.pointsOfInterest.Add(CreatePointOfInterest("poi-a", 10, 10));
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string validReason), Is.True, validReason);
            save.pointsOfInterest.Add(CreatePointOfInterest(secondId, secondX, secondY));
            AssertInvalidReasonStartsWith(save, reasonPrefix);
        }

        [Test]
        public void CorruptPrimaryFallsBackToValidatedBackup()
        {
            string directory = CreateTemporaryDirectory();
            try
            {
                string primary = Path.Combine(directory, "strategy-save.json");
                string backup = primary + ".bak";
                File.WriteAllText(primary, "{ not valid json");
                File.WriteAllText(backup, JsonUtility.ToJson(CreateValidSave()));

                bool loaded = StrategySaveSystem.TryReadSaveFromPaths(
                    primary,
                    backup,
                    out StrategySaveData restored,
                    out string reason,
                    out bool usedBackup);

                Assert.That(loaded, Is.True, reason);
                Assert.That(usedBackup, Is.True);
                Assert.That(restored.mapSeed, Is.EqualTo(12345));
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void OversizedPrimaryIsRejectedBeforeReadAndFallsBackToBackup()
        {
            string directory = CreateTemporaryDirectory();
            try
            {
                string primary = Path.Combine(directory, "strategy-save.json");
                string backup = primary + ".bak";
                using (FileStream stream = new(primary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.SetLength(StrategySaveSystem.MaxSaveFileBytes + 1L);
                }

                StrategySaveData expected = CreateValidSave();
                File.WriteAllText(backup, JsonUtility.ToJson(expected));

                bool loaded = StrategySaveSystem.TryReadSaveFromPaths(
                    primary,
                    backup,
                    out StrategySaveData restored,
                    out string reason,
                    out bool usedBackup);

                Assert.That(loaded, Is.True, reason);
                Assert.That(usedBackup, Is.True);
                Assert.That(restored.mapSeed, Is.EqualTo(expected.mapSeed));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void AtomicWriteRotatesPreviousPrimaryToBackup()
        {
            string directory = CreateTemporaryDirectory();
            try
            {
                string primary = Path.Combine(directory, "strategy-save.json");
                string backup = primary + ".bak";
                StrategySaveSystem.WriteSaveAtomically("first", primary, backup);
                StrategySaveSystem.WriteSaveAtomically("second", primary, backup);

                Assert.That(File.ReadAllText(primary), Is.EqualTo("second"));
                Assert.That(File.ReadAllText(backup), Is.EqualTo("first"));
                Assert.That(File.Exists(primary + ".tmp"), Is.False);
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void PreparedDishCollectionLimitIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            for (int i = 0; i <= StrategySaveSystem.MaxSavePreparedDishesPerBuilding; i++)
            {
                save.buildings[0].preparedDishIds.Add("dish-" + i);
                save.buildings[0].preparedDishAmounts.Add(1);
            }

            AssertInvalidReasonStartsWith(save, "prepared_dish_limit_exceeded_");
        }

        [Test]
        public void PreparedDishPairMismatchIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            save.buildings[0].preparedDishIds.Add("stew");

            AssertInvalidReasonStartsWith(save, "invalid_household_food_state_");
        }

        [Test]
        public void PreparedDishEntriesRequireUniqueBoundedIdsAndPositiveAmounts()
        {
            StrategySaveData save = CreateValidSave();
            StrategyBuildingSaveData building = save.buildings[0];
            building.preparedDishIds.Add("retired-recipe");
            building.preparedDishAmounts.Add(1);
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string validReason), Is.True, validReason);

            building.preparedDishIds.Add("retired-recipe");
            building.preparedDishAmounts.Add(2);
            AssertInvalidReasonStartsWith(save, "invalid_prepared_dish_state_");

            building.preparedDishIds.Clear();
            building.preparedDishAmounts.Clear();
            building.preparedDishIds.Add(new string('x', StrategySaveSystem.MaxSavePreparedDishIdLength + 1));
            building.preparedDishAmounts.Add(1);
            AssertInvalidReasonStartsWith(save, "invalid_prepared_dish_state_");

            building.preparedDishIds[0] = "stew";
            building.preparedDishAmounts[0] = 0;
            AssertInvalidReasonStartsWith(save, "invalid_prepared_dish_state_");
        }

        [Test]
        public void ResidentChildCollectionLimitIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            for (int i = 0; i <= StrategySaveSystem.MaxSaveChildLinksPerResident; i++)
            {
                save.residents[0].childIds.Add(i + 2);
            }

            AssertInvalidReasonStartsWith(save, "resident_child_limit_exceeded_");
        }

        [Test]
        public void ResidentChildLinksRequirePositiveUniqueNonSelfIds()
        {
            StrategySaveData save = CreateValidSave();
            StrategyResidentSaveData resident = save.residents[0];
            resident.childIds.Add(999);
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string validReason), Is.True, validReason);

            resident.childIds.Add(999);
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");

            resident.childIds.Clear();
            resident.childIds.Add(resident.residentId);
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");

            resident.childIds[0] = 0;
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");
        }

        [Test]
        public void ResidentParentLinksRequireNonNegativeDistinctNonSelfIds()
        {
            StrategySaveData save = CreateValidSave();
            StrategyResidentSaveData resident = save.residents[0];
            resident.fatherId = 2;
            resident.motherId = 3;
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string validReason), Is.True, validReason);

            resident.fatherId = -1;
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");

            resident.fatherId = resident.residentId;
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");

            resident.fatherId = 2;
            resident.motherId = 2;
            AssertInvalidReasonStartsWith(save, "invalid_resident_family_links_");
        }

        private static StrategySaveData CreateValidSave()
        {
            StrategySaveData save = new()
            {
                mapSeed = 12345,
                mapWidth = 64,
                mapHeight = 64,
                elapsedSeconds = 10f,
                weatherKind = (int)StrategyWeatherKind.Clear
            };
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "house-a",
                tool = (int)StrategyBuildTool.House,
                originX = 2,
                originY = 3,
                footprintX = 2,
                footprintY = 2
            });
            save.residents.Add(new StrategyResidentSaveData
            {
                residentId = 1,
                homeStableId = "house-a",
                gender = (int)StrategyResidentGender.Female,
                lifeStage = (int)StrategyResidentLifeStage.Adult,
                ageYears = 28f,
                worldX = 2.5f,
                worldY = 3.5f
            });
            return save;
        }

        private static StrategyFoundingStartSaveData CreateFoundingStart()
        {
            StrategyFoundingStartSaveData data = new()
            {
                hasStarterCamp = true,
                starterCampX = 22,
                starterCampY = 31,
                hasStarterCartOrigin = true,
                starterCartOriginX = 24,
                starterCartOriginY = 30,
                profileVersion = 1,
                profileId = "founding-v1"
            };
            data.answers.Add(new StrategyFoundingAnswerSaveData
            {
                questionId = "water",
                answerId = "near-river"
            });
            data.answers.Add(new StrategyFoundingAnswerSaveData
            {
                questionId = "landscape",
                answerId = "forest-edge"
            });
            return data;
        }

        private static StrategyPointOfInterestSaveData CreatePointOfInterest(string id, int x, int y)
        {
            return new StrategyPointOfInterestSaveData { stableId = id, cellX = x, cellY = y, investigated = true };
        }

        private static string CreateTemporaryDirectory()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "ProjectUnknown-SaveTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void AssertInvalidReasonStartsWith(StrategySaveData save, string prefix)
        {
            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith(prefix));
        }

        private static void DeleteTemporaryDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
