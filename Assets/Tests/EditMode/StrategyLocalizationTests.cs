using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectUnknown.Strategy.EditorTools;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyLocalizationTests
    {
        [Test]
        public void SupportedLanguageCodes_AreStable()
        {
            Assert.That(
                StrategyLocalization.ToLocaleCode(StrategyGameLanguage.Russian),
                Is.EqualTo("ru"));
            Assert.That(
                StrategyLocalization.ToLocaleCode(StrategyGameLanguage.English),
                Is.EqualTo("en"));
            Assert.That(
                StrategyLocalization.FromLocaleCode("en-US"),
                Is.EqualTo(StrategyGameLanguage.English));
            Assert.That(
                StrategyLocalization.FromLocaleCode("de"),
                Is.EqualTo(StrategyGameLanguage.Russian));
        }

        [Test]
        public void LegacyKey_IsDeterministicUtf16Hash()
        {
            Assert.That(
                StrategyLocalization.GetLegacyKey("Residents"),
                Is.EqualTo("literal.ae61b8ba6c3792b8"));
            Assert.That(
                StrategyLocalization.GetLegacyKey("Residents"),
                Is.Not.EqualTo(StrategyLocalization.GetLegacyKey("residents")));
        }

        [Test]
        public void TableNames_AreUniqueAndComplete()
        {
            Assert.That(StrategyLocalizationTables.All, Has.Length.EqualTo(9));
            Assert.That(
                new HashSet<string>(StrategyLocalizationTables.All).Count,
                Is.EqualTo(StrategyLocalizationTables.All.Length));
        }

        [Test]
        public void TsvCatalogs_AreValidAndBilingual()
        {
            Assert.DoesNotThrow(StrategyLocalizationAssetGenerator.Validate);
        }

        [Test]
        public void TsvCatalog_RejectsBrokenOrRepeatedPlaceholderSchemas()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(
                    path,
                    "Key\tEnglish\tRussian\n"
                    + "test.repeated\t{0} {0}\t{0}\n");
                Assert.Throws<InvalidDataException>(() =>
                    StrategyLocalizationTsvCatalog.Read("Hud", path));

                File.WriteAllText(
                    path,
                    "Key\tEnglish\tRussian\n"
                    + "test.broken\tValue {0\tЗначение {0\n");
                Assert.Throws<InvalidDataException>(() =>
                    StrategyLocalizationTsvCatalog.Read("Hud", path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void GeneratedCollections_ContainMatchingRussianAndEnglishEntries()
        {
            IReadOnlyList<Locale> locales = LocalizationEditorSettings.GetLocales();
            Locale russian = FindLocale(locales, StrategyLocalization.RussianLocaleCode);
            Locale english = FindLocale(locales, StrategyLocalization.EnglishLocaleCode);
            Assert.That(russian, Is.Not.Null);
            Assert.That(english, Is.Not.Null);

            for (int index = 0; index < StrategyLocalizationTables.All.Length; index++)
            {
                string tableName = StrategyLocalizationTables.All[index];
                StringTableCollection collection =
                    LocalizationEditorSettings.GetStringTableCollection(tableName);
                Assert.That(collection, Is.Not.Null, tableName);

                StringTable russianTable = collection.GetTable(russian.Identifier)
                    as StringTable;
                StringTable englishTable = collection.GetTable(english.Identifier)
                    as StringTable;
                Assert.That(russianTable, Is.Not.Null, tableName + " Russian");
                Assert.That(englishTable, Is.Not.Null, tableName + " English");
                Assert.That(russianTable.Count, Is.EqualTo(englishTable.Count), tableName);
                Assert.That(russianTable.Count, Is.GreaterThan(0), tableName);

                foreach (StringTableEntry englishEntry in englishTable.Values)
                {
                    StringTableEntry russianEntry = russianTable.GetEntry(
                        englishEntry.KeyId);
                    Assert.That(russianEntry, Is.Not.Null,
                        tableName + "/" + englishEntry.Key);
                    Assert.That(englishEntry.Value, Is.Not.Empty,
                        tableName + "/" + englishEntry.Key + " English");
                    Assert.That(russianEntry.Value, Is.Not.Empty,
                        tableName + "/" + englishEntry.Key + " Russian");
                }
            }
        }

        [Test]
        public void StartupSelectors_DefaultToRussianAfterOverrides()
        {
            LocalizationSettings settings =
                LocalizationEditorSettings.ActiveLocalizationSettings;
            Assert.That(settings, Is.Not.Null);

            List<IStartupLocaleSelector> selectors =
                settings.GetStartupLocaleSelectors();
            Assert.That(selectors, Has.Count.EqualTo(3));
            Assert.That(selectors[0], Is.TypeOf<CommandLineLocaleSelector>());
            Assert.That(selectors[1], Is.TypeOf<PlayerPrefLocaleSelector>());
            Assert.That(selectors[2], Is.TypeOf<SpecificLocaleSelector>());
            Assert.That(
                ((SpecificLocaleSelector)selectors[2]).LocaleId.Code,
                Is.EqualTo(StrategyLocalization.RussianLocaleCode));
        }

        [Test]
        public void RuntimeLookup_SwitchesTheSameSemanticKeyBetweenRussianAndEnglish()
        {
            IReadOnlyList<Locale> locales = LocalizationEditorSettings.GetLocales();
            Locale russian = FindLocale(locales, StrategyLocalization.RussianLocaleCode);
            Locale english = FindLocale(locales, StrategyLocalization.EnglishLocaleCode);
            Locale previous = LocalizationSettings.SelectedLocale;
            Assert.That(russian, Is.Not.Null);
            Assert.That(english, Is.Not.Null);

            try
            {
                LocalizationSettings.SelectedLocale = russian;
                Assert.That(
                    StrategyLocalization.Get(StrategyLocalizationTables.Menu, "settings.title"),
                    Is.EqualTo("НАСТРОЙКИ"));

                LocalizationSettings.SelectedLocale = english;
                Assert.That(
                    StrategyLocalization.Get(StrategyLocalizationTables.Menu, "settings.title"),
                    Is.EqualTo("SETTINGS"));
            }
            finally
            {
                LocalizationSettings.SelectedLocale = previous;
            }
        }

        [Test]
        public void RuntimeSource_DoesNotHardcodeCyrillicPlayerCopy()
        {
            string[] paths = Directory.GetFiles(
                "Assets/Scripts/Runtime",
                "*.cs",
                SearchOption.AllDirectories);
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                string source = File.ReadAllText(paths[pathIndex]);
                int cyrillicIndex = -1;
                for (int charIndex = 0; charIndex < source.Length; charIndex++)
                {
                    char value = source[charIndex];
                    if (value >= '\u0400' && value <= '\u04ff')
                    {
                        cyrillicIndex = charIndex;
                        break;
                    }
                }

                Assert.That(
                    cyrillicIndex,
                    Is.EqualTo(-1),
                    paths[pathIndex] + " contains Cyrillic at character " + cyrillicIndex
                    + "; player copy belongs in the bilingual catalogs.");
            }
        }

        private static Locale FindLocale(
            IReadOnlyList<Locale> locales,
            string code)
        {
            for (int index = 0; index < locales.Count; index++)
            {
                if (locales[index] != null
                    && locales[index].Identifier.Code == code)
                {
                    return locales[index];
                }
            }

            return null;
        }
    }
}
