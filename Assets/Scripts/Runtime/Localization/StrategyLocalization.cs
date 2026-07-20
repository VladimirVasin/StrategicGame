using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyLocalization
    {
        public const string RussianLocaleCode = "ru";
        public const string EnglishLocaleCode = "en";
        public const string LanguagePreferenceKey = "settings.language";

        private static readonly HashSet<string> MissingEntries = new HashSet<string>();
        private static readonly Dictionary<string, string> LiteralCache =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> MissingLiterals =
            new HashSet<string>(StringComparer.Ordinal);

        private static bool subscribed;
        private static bool completionHooked;
        private static bool initializationCompleted;
        private static bool hasPublishedLanguage;
        private static string publishedLocaleCode;
        private static StrategyGameLanguage? pendingLanguage;
        private static StrategyGameLanguage currentLanguage = StrategyGameLanguage.Russian;
        private static int notificationVersion;

        public static event Action LanguageChanged;

        public static StrategyGameLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
        }

        public static bool IsReady
        {
            get
            {
                return initializationCompleted;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            subscribed = false;
            completionHooked = false;
            initializationCompleted = false;
            hasPublishedLanguage = false;
            publishedLocaleCode = null;
            pendingLanguage = null;
            currentLanguage = StrategyGameLanguage.Russian;
            notificationVersion = 0;
            MissingEntries.Clear();
            LiteralCache.Clear();
            MissingLiterals.Clear();
            LanguageChanged = null;
            StrategyLegacyTextAutoLocalizer.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            Initialize();
        }

        public static void Initialize()
        {
            ResolveStartupPreference();
            if (!LocalizationSettings.HasSettings)
            {
                Debug.LogWarning("Strategy localization settings are unavailable.");
                return;
            }

            EnsureSubscribed();
            StrategyLegacyTextAutoLocalizer.Install();
            if (initializationCompleted)
            {
                return;
            }

            var operation = LocalizationSettings.InitializationOperation;
            if (operation.IsDone)
            {
                CompleteInitialization();
            }
            else if (!completionHooked)
            {
                completionHooked = true;
                operation.Completed += _ => CompleteInitialization();
            }
        }

        public static bool Initialize(StrategyGameLanguage language)
        {
            Initialize();
            return SetLanguage(language);
        }

        public static bool SetLanguage(StrategyGameLanguage language)
        {
            Initialize();
            if (!LocalizationSettings.HasSettings)
            {
                return false;
            }

            pendingLanguage = language;
            PlayerPrefs.SetString(LanguagePreferenceKey, ToLocaleCode(language));
            PlayerPrefs.Save();
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                return true;
            }

            initializationCompleted = true;
            return ApplyPendingLanguage();
        }

        public static string Get(string key)
        {
            return Get(InferTable(key), key, Array.Empty<object>());
        }

        public static string Get(string table, string key)
        {
            return Get(table, key, Array.Empty<object>());
        }

        public static string Get(string table, string key, params object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentException("Localization table is required.", nameof(table));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Localization key is required.", nameof(key));
            }

            EnsureInitialized();
            if (TryGet(table, key, arguments, out string localized))
            {
                return localized;
            }

            ReportMissingOnce(table, key);
            return key;
        }

        public static string TranslateLiteral(string english)
        {
            return TryTranslateLiteral(english, out string localized) ? localized : english;
        }

        public static bool TryTranslateLiteral(string english, out string localized)
        {
            localized = english;
            if (string.IsNullOrEmpty(english))
            {
                return false;
            }

            EnsureInitialized();
            if (!EnsureLookupReady())
            {
                return false;
            }

            if (LiteralCache.TryGetValue(english, out string cached))
            {
                localized = cached;
                return true;
            }

            if (MissingLiterals.Contains(english)
                || !LegacySourceExists(english)
                || !TryGet(StrategyLocalizationTables.Legacy, GetLegacyKey(english),
                    Array.Empty<object>(), out localized))
            {
                MissingLiterals.Add(english);
                localized = english;
                return false;
            }

            LiteralCache[english] = localized;
            return true;
        }

        public static string GetLegacyKey(string english)
        {
            if (english == null)
            {
                throw new ArgumentNullException(nameof(english));
            }

            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < english.Length; i++)
            {
                char value = english[i];
                hash ^= (byte)value;
                hash *= prime;
                hash ^= (byte)(value >> 8);
                hash *= prime;
            }

            return "literal." + hash.ToString("x16");
        }

        public static string ToLocaleCode(StrategyGameLanguage language)
        {
            return language == StrategyGameLanguage.English
                ? EnglishLocaleCode
                : RussianLocaleCode;
        }

        public static StrategyGameLanguage FromLocaleCode(string code)
        {
            return !string.IsNullOrEmpty(code)
                && code.StartsWith(EnglishLocaleCode, StringComparison.OrdinalIgnoreCase)
                ? StrategyGameLanguage.English
                : StrategyGameLanguage.Russian;
        }

        private static bool TryGet(
            string table,
            string key,
            object[] arguments,
            out string localized)
        {
            localized = null;
            if (!LocalizationSettings.HasSettings)
            {
                return false;
            }

            try
            {
                if (!EnsureLookupReady())
                {
                    return false;
                }

                var result = LocalizationSettings.StringDatabase.GetTableEntry(
                    table,
                    key,
                    null,
                    FallbackBehavior.UseProjectSettings);
                if (result.Entry == null)
                {
                    return false;
                }

                localized = result.Entry.GetLocalizedString(arguments ?? Array.Empty<object>());
                return localized != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Strategy localization lookup failed for "
                    + table + "/" + key + ": " + exception.GetBaseException().Message);
                return false;
            }
        }

        private static bool LegacySourceExists(string english)
        {
            Locale englishLocale = LocalizationSettings.AvailableLocales.GetLocale(
                EnglishLocaleCode);
            if (englishLocale == null)
            {
                return false;
            }

            try
            {
                var result = LocalizationSettings.StringDatabase.GetTableEntry(
                    StrategyLocalizationTables.Legacy,
                    GetLegacyKey(english),
                    englishLocale,
                    FallbackBehavior.DontUseFallback);
                return result.Entry != null
                    && string.Equals(result.Entry.Value, english, StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string InferTable(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return StrategyLocalizationTables.Menu;
            }

            if (StartsWith(key, "common.")) return StrategyLocalizationTables.Common;
            if (StartsWith(key, "founding.")) return StrategyLocalizationTables.Founding;
            if (StartsWith(key, "hud.") || StartsWith(key, "goal."))
                return StrategyLocalizationTables.Hud;
            if (StartsWith(key, "building.") || StartsWith(key, "build."))
                return StrategyLocalizationTables.Buildings;
            if (StartsWith(key, "resource.") || StartsWith(key, "recipe."))
                return StrategyLocalizationTables.Resources;
            if (StartsWith(key, "resident.") || StartsWith(key, "profession."))
                return StrategyLocalizationTables.Residents;
            if (StartsWith(key, "story.")) return StrategyLocalizationTables.Stories;
            if (StartsWith(key, "literal.")) return StrategyLocalizationTables.Legacy;
            return StrategyLocalizationTables.Menu;
        }

        private static bool StartsWith(string value, string prefix)
        {
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void ReportMissingOnce(string table, string key)
        {
            string identity = table + "/" + key;
            if (MissingEntries.Add(identity))
            {
                Debug.LogWarning("Missing localization entry: " + identity);
            }
        }
    }
}
