using NUnit.Framework;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyCombatPersistenceTests
    {
        [Test]
        public void Version12MigratesResidentsToFullCombatHealth()
        {
            StrategySaveData legacy = CreateValidSave();
            legacy.version = 12;
            legacy.residents[0].combatHealth = 0;
            legacy.residents[0].lastCombatRecoveryDayIndex = 7;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(legacy),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.True);
            Assert.That(restored.version, Is.EqualTo(StrategySaveData.CurrentVersion));
            Assert.That(restored.residents[0].combatHealth, Is.EqualTo(100));
            Assert.That(restored.residents[0].lastCombatRecoveryDayIndex, Is.EqualTo(-1));
        }

        [Test]
        public void CombatHealthRoundTripsWithoutMigration()
        {
            StrategySaveData source = CreateValidSave();
            source.residents[0].combatHealth = 43;
            source.residents[0].lastCombatRecoveryDayIndex = 9;

            bool loaded = StrategySaveSystem.TryDeserializeAndValidate(
                JsonUtility.ToJson(source),
                out StrategySaveData restored,
                out string reason,
                out bool migrated);

            Assert.That(loaded, Is.True, reason);
            Assert.That(migrated, Is.False);
            Assert.That(restored.residents[0].combatHealth, Is.EqualTo(43));
            Assert.That(restored.residents[0].lastCombatRecoveryDayIndex, Is.EqualTo(9));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(101)]
        public void InvalidCombatHealthIsRejected(int combatHealth)
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].combatHealth = combatHealth;

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("invalid_resident_"));
        }

        [Test]
        public void InvalidCombatRecoveryDayIsRejected()
        {
            StrategySaveData save = CreateValidSave();
            save.residents[0].lastCombatRecoveryDayIndex = -2;

            Assert.That(StrategySaveSystem.ValidateSaveData(save, out string reason), Is.False);
            Assert.That(reason, Does.StartWith("invalid_resident_"));
        }

        private static StrategySaveData CreateValidSave()
        {
            StrategySaveData save = new()
            {
                mapSeed = 74123,
                mapWidth = 12,
                mapHeight = 12
            };
            save.buildings.Add(new StrategyBuildingSaveData
            {
                stableId = "house-combat-save",
                tool = (int)StrategyBuildTool.House,
                originX = 2,
                originY = 3,
                footprintX = 2,
                footprintY = 2
            });
            save.residents.Add(new StrategyResidentSaveData
            {
                residentId = 1,
                homeStableId = "house-combat-save",
                gender = (int)StrategyResidentGender.Female,
                lifeStage = (int)StrategyResidentLifeStage.Adult,
                ageYears = 28f,
                worldX = 2.5f,
                worldY = 3.5f
            });
            return save;
        }
    }
}
