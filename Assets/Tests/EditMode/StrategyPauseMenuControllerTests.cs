using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy.EditorTests
{
    public sealed class StrategyPauseMenuControllerTests
    {
        private GameObject root;
        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private StrategyPauseMenuController pauseMenu;
        private EventSystem existingEventSystem;
        private StrategyHudSfxAudio existingHudSfx;
        private float originalTimeScale;
        private float originalFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            existingEventSystem = Object.FindAnyObjectByType<EventSystem>();
            existingHudSfx = Object.FindAnyObjectByType<StrategyHudSfxAudio>();

            root = new GameObject("Pause Menu Test Root");
            inputRouter = root.AddComponent<StrategyInputRouter>();
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(inputAsset, Is.Not.Null);
            Assert.That(inputRouter.Configure(inputAsset), Is.True, inputRouter.ConfigurationError);

            timeScale = root.AddComponent<StrategyTimeScaleController>();
            timeScale.SetInputRouter(inputRouter);
            timeScale.Configure();
            timeScale.SetRequestedScale(3f);

            StrategySaveSystem saveSystem = root.AddComponent<StrategySaveSystem>();
            StrategyConfirmationDialogController confirmationDialog =
                root.AddComponent<StrategyConfirmationDialogController>();
            confirmationDialog.SetInputRouter(inputRouter);
            confirmationDialog.Configure();

            GameObject pauseMenuObject = new("Test Strategy Pause Menu");
            pauseMenuObject.transform.SetParent(root.transform, false);
            pauseMenu = pauseMenuObject.AddComponent<StrategyPauseMenuController>();
            pauseMenu.Configure(inputRouter, timeScale, saveSystem, confirmationDialog);
            Assert.That(pauseMenu.IsConfigured, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            if (existingEventSystem == null)
            {
                EventSystem createdEventSystem = Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }

            if (existingHudSfx == null)
            {
                StrategyHudSfxAudio createdHudSfx = Object.FindAnyObjectByType<StrategyHudSfxAudio>();
                if (createdHudSfx != null)
                {
                    Object.DestroyImmediate(createdHudSfx.gameObject);
                }
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        [Test]
        public void OpenAndCloseOwnOneContextAndRestoreRequestedSpeed()
        {
            Assert.That(pauseMenu.IsOpen, Is.False);
            Assert.That(pauseMenu.Open(), Is.True);
            Assert.That(pauseMenu.Open(), Is.False);
            Assert.That(pauseMenu.IsOpen, Is.True);
            Assert.That(inputRouter.ActiveContextCount, Is.EqualTo(1));
            Assert.That(inputRouter.BlockedChannels, Is.EqualTo(StrategyInputChannel.All));
            Assert.That(inputRouter.TopCancelMode, Is.EqualTo(StrategyCancelMode.Close));
            Assert.That(timeScale.IsPausedByLock, Is.True);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));
            Assert.That(Time.timeScale, Is.Zero);

            Assert.That(pauseMenu.Close(), Is.True);
            Assert.That(pauseMenu.Close(), Is.False);
            Assert.That(pauseMenu.IsOpen, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(timeScale.CurrentScale, Is.EqualTo(3f));
            Assert.That(Time.timeScale, Is.EqualTo(3f));
        }

        [Test]
        public void CancelReturnsFromSettingsBeforeClosingMenu()
        {
            Assert.That(pauseMenu.Open(), Is.True);
            pauseMenu.ShowSettings();
            Assert.That(pauseMenu.CurrentPage, Is.EqualTo(StrategyPauseMenuPage.Settings));

            pauseMenu.HandleCancel();
            Assert.That(pauseMenu.IsOpen, Is.True);
            Assert.That(pauseMenu.CurrentPage, Is.EqualTo(StrategyPauseMenuPage.Actions));
            Assert.That(timeScale.IsPausedByLock, Is.True);

            pauseMenu.HandleCancel();
            Assert.That(pauseMenu.IsOpen, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
        }

        [Test]
        public void LifecycleCleanupReleasesInputAndPauseOwnership()
        {
            Assert.That(pauseMenu.Open(), Is.True);

            MethodInfo onDisable = typeof(StrategyPauseMenuController).GetMethod(
                "OnDisable",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDisable, Is.Not.Null);
            onDisable.Invoke(pauseMenu, null);

            Assert.That(pauseMenu.IsOpen, Is.False);
            Assert.That(inputRouter.ActiveContextCount, Is.Zero);
            Assert.That(timeScale.IsPausedByLock, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(3f));
        }

        [Test]
        public void SettingsLanguageButtonSwitchesTheOpenMenuBetweenRussianAndEnglish()
        {
            Locale previousLocale = LocalizationSettings.SelectedLocale;
            bool hadPreference = PlayerPrefs.HasKey(StrategyLocalization.LanguagePreferenceKey);
            string previousPreference = PlayerPrefs.GetString(
                StrategyLocalization.LanguagePreferenceKey,
                StrategyLocalization.RussianLocaleCode);
            Locale russian = LocalizationSettings.AvailableLocales.GetLocale("ru");
            Locale english = LocalizationSettings.AvailableLocales.GetLocale("en");
            Assert.That(russian, Is.Not.Null);
            Assert.That(english, Is.Not.Null);

            try
            {
                Assert.That(
                    StrategyLocalization.SetLanguage(StrategyGameLanguage.Russian),
                    Is.True);
                Assert.That(pauseMenu.Open(), Is.True);
                pauseMenu.ShowSettings();

                Text settingsTitle = FindText("SettingsTitle");
                Button languageButton = FindButton("Language");
                Text languageLabel = languageButton.GetComponentInChildren<Text>(true);
                Assert.That(settingsTitle.text, Is.EqualTo("НАСТРОЙКИ"));
                Assert.That(languageLabel.text, Is.EqualTo("Язык: Русский"));

                languageButton.onClick.Invoke();

                Assert.That(StrategyGameSettings.Language, Is.EqualTo(StrategyGameLanguage.English));
                Assert.That(settingsTitle.text, Is.EqualTo("SETTINGS"));
                Assert.That(languageLabel.text, Is.EqualTo("Language: English"));
            }
            finally
            {
                StrategyGameLanguage restoreLanguage = previousLocale != null
                    ? StrategyLocalization.FromLocaleCode(previousLocale.Identifier.Code)
                    : StrategyLocalization.FromLocaleCode(previousPreference);
                StrategyLocalization.SetLanguage(restoreLanguage);
                LocalizationSettings.SelectedLocale = previousLocale;
                if (hadPreference)
                {
                    PlayerPrefs.SetString(
                        StrategyLocalization.LanguagePreferenceKey,
                        previousPreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(StrategyLocalization.LanguagePreferenceKey);
                }

                PlayerPrefs.Save();
            }
        }

        private Text FindText(string objectName)
        {
            Text[] texts = pauseMenu.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                if (texts[index].name == objectName)
                {
                    return texts[index];
                }
            }

            Assert.Fail("Missing pause-menu Text: " + objectName);
            return null;
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = pauseMenu.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++)
            {
                if (buttons[index].name == objectName)
                {
                    return buttons[index];
                }
            }

            Assert.Fail("Missing pause-menu Button: " + objectName);
            return null;
        }

    }
}
