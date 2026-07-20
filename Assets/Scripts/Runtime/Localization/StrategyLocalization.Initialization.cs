using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ProjectUnknown.Strategy
{
    public static partial class StrategyLocalization
    {
        private static bool EnsureLookupReady()
        {
            if (!LocalizationSettings.HasSettings)
            {
                return false;
            }

            try
            {
                var operation = LocalizationSettings.InitializationOperation;
                if (!operation.IsDone)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    return false;
#else
                    operation.WaitForCompletion();
#endif
                }

                if (!operation.IsDone)
                {
                    return false;
                }

                if (!initializationCompleted)
                {
                    CompleteInitialization();
                }

                return LocalizationSettings.SelectedLocale != null
                    || ApplyPendingLanguage();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Strategy localization initialization failed: "
                    + exception.GetBaseException().Message);
                return false;
            }
        }

        private static void EnsureInitialized()
        {
            if (!subscribed)
            {
                Initialize();
            }
        }

        private static void EnsureSubscribed()
        {
            if (subscribed)
            {
                return;
            }

            LocalizationSettings.SelectedLocaleChanged += SynchronizeSelectedLocale;
            subscribed = true;
        }

        private static void SynchronizeSelectedLocale(Locale locale)
        {
            StrategyGameLanguage next = locale == null
                ? StrategyGameLanguage.Russian
                : FromLocaleCode(locale.Identifier.Code);
            string localeCode = locale != null ? locale.Identifier.Code : RussianLocaleCode;
            bool shouldPublish = !hasPublishedLanguage
                || currentLanguage != next
                || !string.Equals(
                    publishedLocaleCode,
                    localeCode,
                    StringComparison.OrdinalIgnoreCase);
            currentLanguage = next;
            if (!shouldPublish)
            {
                return;
            }

            hasPublishedLanguage = true;
            publishedLocaleCode = localeCode;
            LiteralCache.Clear();
            MissingLiterals.Clear();
            notificationVersion++;
            LanguageChanged?.Invoke();
        }

        private static void CompleteInitialization()
        {
            completionHooked = false;
            initializationCompleted = true;
            MissingEntries.Clear();
            LiteralCache.Clear();
            MissingLiterals.Clear();

            int versionBeforeApply = notificationVersion;
            ApplyPendingLanguage();
            if (notificationVersion == versionBeforeApply)
            {
                SynchronizeSelectedLocale(LocalizationSettings.SelectedLocale);
            }
        }

        private static bool ApplyPendingLanguage()
        {
            StrategyGameLanguage requested = pendingLanguage
                ?? StrategyGameLanguage.Russian;
            string code = ToLocaleCode(requested);
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null && requested != StrategyGameLanguage.Russian)
            {
                requested = StrategyGameLanguage.Russian;
                pendingLanguage = requested;
                code = RussianLocaleCode;
                locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            }

            if (locale == null)
            {
                Debug.LogWarning("Strategy localization locale is missing: " + code);
                return false;
            }

            int versionBeforeSelection = notificationVersion;
            LocalizationSettings.SelectedLocale = locale;
            if (notificationVersion == versionBeforeSelection)
            {
                SynchronizeSelectedLocale(locale);
            }

            return true;
        }

        private static void ResolveStartupPreference()
        {
            if (pendingLanguage.HasValue)
            {
                return;
            }

            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                const string prefix = "-language=";
                if (!arguments[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string overrideCode = arguments[i].Substring(prefix.Length);
                if (TryParseSupportedLanguage(overrideCode, out StrategyGameLanguage overrideLanguage))
                {
                    pendingLanguage = overrideLanguage;
                    currentLanguage = overrideLanguage;
                    return;
                }
            }

            string savedCode = PlayerPrefs.GetString(LanguagePreferenceKey, RussianLocaleCode);
            if (!TryParseSupportedLanguage(savedCode, out StrategyGameLanguage savedLanguage))
            {
                savedLanguage = StrategyGameLanguage.Russian;
                PlayerPrefs.SetString(LanguagePreferenceKey, RussianLocaleCode);
                PlayerPrefs.Save();
            }
            else if (!PlayerPrefs.HasKey(LanguagePreferenceKey))
            {
                PlayerPrefs.SetString(LanguagePreferenceKey, RussianLocaleCode);
                PlayerPrefs.Save();
            }

            pendingLanguage = savedLanguage;
            currentLanguage = savedLanguage;
        }

        private static bool TryParseSupportedLanguage(
            string code,
            out StrategyGameLanguage language)
        {
            if (string.Equals(code, EnglishLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                language = StrategyGameLanguage.English;
                return true;
            }

            if (string.Equals(code, RussianLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                language = StrategyGameLanguage.Russian;
                return true;
            }

            language = StrategyGameLanguage.Russian;
            return false;
        }
    }
}
