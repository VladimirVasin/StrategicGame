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

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.buildings[0].preparedDishIds, Is.Not.Null);
            Assert.That(restored.buildings[0].preparedDishAmounts, Is.Not.Null);
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
