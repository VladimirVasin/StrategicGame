using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static class StrategyLocalizationAssetGenerator
    {
        private const string Root = "Assets/Localization";
        private const string SourceRoot = Root + "/Source";
        private const string GeneratedRoot = Root + "/Generated";
        private const string LocaleRoot = Root + "/Locales";
        private const string SettingsPath = Root + "/LocalizationSettings.asset";
        private const string RussianLocalePath = LocaleRoot + "/Russian.asset";
        private const string EnglishLocalePath = LocaleRoot + "/English.asset";
        private const string Header = "Key\tEnglish\tRussian\n";

        private static readonly string[] CommonFoundationRows =
        {
            "common.ready\tReady\tГотово",
            "settings.language.russian\tLanguage: Russian\tЯзык: Русский",
            "settings.language.english\tLanguage: English\tЯзык: Английский"
        };

        [MenuItem("ProjectUnknown/Localization/Generate String Tables")]
        public static void Generate()
        {
            EnsureFolders();
            EnsureFoundationSources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            LocalizationSettings settings = EnsureSettings();
            Locale russian = EnsureLocale(
                RussianLocalePath,
                StrategyLocalization.RussianLocaleCode,
                "Русский",
                0);
            Locale english = EnsureLocale(
                EnglishLocalePath,
                StrategyLocalization.EnglishLocaleCode,
                "English",
                1);
            LocalizationEditorSettings.AddLocale(russian);
            LocalizationEditorSettings.AddLocale(english);
            ConfigureSettings(settings, russian, english);

            var locales = new List<Locale> { russian, english };
            int totalEntries = 0;
            for (int i = 0; i < StrategyLocalizationTables.All.Length; i++)
            {
                string tableName = StrategyLocalizationTables.All[i];
                IReadOnlyList<StrategyLocalizationTsvCatalog.Row> rows =
                    ReadSourceFragments(tableName);
                StringTableCollection collection = EnsureCollection(tableName, locales);
                ImportRows(collection, russian, english, rows);
                totalEntries += rows.Count;
            }

            settings.GetStringDatabase().DefaultTable = StrategyLocalizationTables.Common;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Localization] Generated 9 RU/EN table collections with "
                + totalEntries + " entries.");
        }

        [MenuItem("ProjectUnknown/Localization/Validate TSV Catalogs")]
        public static void Validate()
        {
            EnsureFoundationSources();
            int totalEntries = 0;
            for (int i = 0; i < StrategyLocalizationTables.All.Length; i++)
            {
                string table = StrategyLocalizationTables.All[i];
                totalEntries += ReadSourceFragments(table).Count;
            }

            Debug.Log("[Localization] TSV validation passed: " + totalEntries + " entries.");
        }

        public static void GenerateAndExit()
        {
            try
            {
                Generate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static LocalizationSettings EnsureSettings()
        {
            LocalizationSettings settings =
                AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "ProjectUnknown Localization Settings";
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        private static Locale EnsureLocale(
            string path,
            string code,
            string displayName,
            ushort sortOrder)
        {
            Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path)
                ?? LocalizationEditorSettings.GetLocale(code);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                AssetDatabase.CreateAsset(locale, path);
            }

            locale.LocaleName = displayName;
            locale.SortOrder = sortOrder;
            EditorUtility.SetDirty(locale);
            return locale;
        }

        private static void ConfigureSettings(
            LocalizationSettings settings,
            Locale russian,
            Locale english)
        {
            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new CommandLineLocaleSelector { CommandLineArgument = "-language=" });
            selectors.Add(new PlayerPrefLocaleSelector
            {
                PlayerPreferenceKey = StrategyLocalization.LanguagePreferenceKey
            });
            selectors.Add(new SpecificLocaleSelector
            {
                LocaleId = new LocaleIdentifier(StrategyLocalization.RussianLocaleCode)
            });

            FallbackLocale fallback = russian.Metadata.GetMetadata<FallbackLocale>();
            if (fallback == null)
            {
                russian.Metadata.AddMetadata(new FallbackLocale(english));
            }
            else
            {
                fallback.Locale = english;
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            LocalizationSettings.ProjectLocale = english;
            LocalizationSettings.InitializeSynchronously = false;
            LocalizationSettings.PreloadBehavior = PreloadBehavior.PreloadSelectedLocaleAndFallbacks;
            settings.GetStringDatabase().UseFallback = true;
            EditorUtility.SetDirty(russian);
            EditorUtility.SetDirty(settings);
        }

        private static StringTableCollection EnsureCollection(
            string tableName,
            IList<Locale> locales)
        {
            StringTableCollection collection =
                LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                string directory = GeneratedRoot + "/" + tableName;
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    tableName,
                    directory,
                    locales);
            }

            for (int i = 0; i < locales.Count; i++)
            {
                if (!collection.ContainsTable(locales[i].Identifier))
                {
                    collection.AddNewTable(locales[i].Identifier);
                }
            }

            return collection;
        }

        private static void ImportRows(
            StringTableCollection collection,
            Locale russian,
            Locale english,
            IReadOnlyList<StrategyLocalizationTsvCatalog.Row> rows)
        {
            StringTable englishTable = collection.GetTable(english.Identifier) as StringTable;
            StringTable russianTable = collection.GetTable(russian.Identifier) as StringTable;
            if (englishTable == null || russianTable == null)
            {
                throw new InvalidOperationException(
                    "Collection is missing RU/EN tables: " + collection.TableCollectionName);
            }

            var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                sourceKeys.Add(rows[i].Key);
            }

            var staleKeys = new List<string>();
            for (int i = 0; i < collection.SharedData.Entries.Count; i++)
            {
                string key = collection.SharedData.Entries[i].Key;
                if (!sourceKeys.Contains(key))
                {
                    staleKeys.Add(key);
                }
            }

            for (int i = 0; i < staleKeys.Count; i++)
            {
                collection.RemoveEntry(staleKeys[i]);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                StrategyLocalizationTsvCatalog.Row row = rows[i];
                StringTableEntry englishEntry = englishTable.GetEntry(row.Key)
                    ?? englishTable.AddEntry(row.Key, row.English);
                StringTableEntry russianEntry = russianTable.GetEntry(row.Key)
                    ?? russianTable.AddEntry(row.Key, row.Russian);
                englishEntry.Value = row.English;
                russianEntry.Value = row.Russian;
                englishEntry.IsSmart = row.Smart;
                russianEntry.IsSmart = row.Smart;
            }

            collection.SetPreloadTableFlag(true);
            collection.RefreshAddressables();
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(russianTable);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(GeneratedRoot);
            Directory.CreateDirectory(LocaleRoot);
        }

        private static void EnsureFoundationSources()
        {
            EnsureFolders();
            EnsureFile(SourceRoot + "/Common.tsv", CommonFoundationRows);
            EnsureFile(SourceRoot + "/Menu.tsv", Array.Empty<string>());
            EnsureFile(SourceRoot + "/Legacy.tsv", Array.Empty<string>());
        }

        private static IReadOnlyList<StrategyLocalizationTsvCatalog.Row> ReadSourceFragments(
            string tableName)
        {
            var paths = new List<string>();
            string primary = SourceRoot + "/" + tableName + ".tsv";
            if (File.Exists(primary))
            {
                paths.Add(primary);
            }

            string[] fragments = Directory.GetFiles(
                SourceRoot,
                tableName + ".*.tsv",
                SearchOption.TopDirectoryOnly);
            Array.Sort(fragments, StringComparer.Ordinal);
            paths.AddRange(fragments);

            var combined = new List<StrategyLocalizationTsvCatalog.Row>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
            {
                IReadOnlyList<StrategyLocalizationTsvCatalog.Row> fragment =
                    StrategyLocalizationTsvCatalog.Read(tableName, paths[pathIndex]);
                for (int rowIndex = 0; rowIndex < fragment.Count; rowIndex++)
                {
                    StrategyLocalizationTsvCatalog.Row row = fragment[rowIndex];
                    if (!keys.Add(row.Key))
                    {
                        throw new InvalidDataException(
                            "Duplicate localization key across " + tableName
                            + " source fragments: " + row.Key);
                    }

                    combined.Add(row);
                }
            }

            return combined;
        }

        private static void EnsureFile(string path, IReadOnlyList<string> requiredRows)
        {
            var encoding = new UTF8Encoding(false);
            if (!File.Exists(path))
            {
                var builder = new StringBuilder(Header);
                for (int i = 0; i < requiredRows.Count; i++)
                {
                    builder.AppendLine(requiredRows[i]);
                }

                File.WriteAllText(path, builder.ToString(), encoding);
                return;
            }

            var existingKeys = new HashSet<string>(StringComparer.Ordinal);
            string[] lines = File.ReadAllLines(path, encoding);
            for (int i = 1; i < lines.Length; i++)
            {
                int tab = lines[i].IndexOf('\t');
                if (tab > 0)
                {
                    existingKeys.Add(lines[i].Substring(0, tab));
                }
            }

            var additions = new StringBuilder();
            for (int i = 0; i < requiredRows.Count; i++)
            {
                int tab = requiredRows[i].IndexOf('\t');
                string key = requiredRows[i].Substring(0, tab);
                if (!existingKeys.Contains(key))
                {
                    additions.AppendLine(requiredRows[i]);
                }
            }

            if (additions.Length > 0)
            {
                string separator = lines.Length > 0 && !File.ReadAllText(path).EndsWith("\n")
                    ? Environment.NewLine
                    : string.Empty;
                File.AppendAllText(path, separator + additions.ToString(), encoding);
            }
        }
    }
}
