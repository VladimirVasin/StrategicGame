using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal enum StrategyPauseMenuPage
    {
        Actions,
        Settings
    }

    [DisallowMultipleComponent]
    public sealed partial class StrategyPauseMenuController : MonoBehaviour
    {
        private const string PauseLockReason = "PauseMenu";

        private StrategyInputRouter inputRouter;
        private StrategyTimeScaleController timeScale;
        private StrategySaveSystem saveSystem;
        private StrategyConfirmationDialogController confirmationDialog;
        private StrategyInputContextHandle inputContext;
        private StrategyUiPanelTransition panelTransition;
        private GameObject actionsRoot;
        private GameObject settingsRoot;
        private Button resumeButton;
        private Button saveButton;
        private Button settingsButton;
        private Button mainMenuButton;
        private Button quitButton;
        private Button settingsBackButton;
        private Button languageButton;
        private Text languageButtonLabel;
        private Text statusText;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider uiScaleSlider;
        private Toggle fullscreenToggle;
        private Toggle reducedMotionToggle;
        private string statusLocalizationKey = string.Empty;
        private bool configured;
        private bool isOpen;
        private bool pauseLockHeld;
        private bool transitioning;
        private StrategyPauseMenuPage page;

        public bool IsConfigured => configured;
        public bool IsOpen => isOpen;
        internal StrategyPauseMenuPage CurrentPage => page;

        public void Configure(
            StrategyInputRouter router,
            StrategyTimeScaleController timeScaleController,
            StrategySaveSystem persistence,
            StrategyConfirmationDialogController confirmations)
        {
            if (configured)
            {
                return;
            }

            inputRouter = router;
            timeScale = timeScaleController;
            saveSystem = persistence;
            confirmationDialog = confirmations;
            configured = inputRouter != null
                && timeScale != null
                && saveSystem != null
                && confirmationDialog != null;
            if (!configured)
            {
                StrategyDebugLogger.Error(
                    "PauseMenu",
                    "ConfigurationRejected",
                    StrategyDebugLogger.F("hasInput", inputRouter != null),
                    StrategyDebugLogger.F("hasTime", timeScale != null),
                    StrategyDebugLogger.F("hasSave", saveSystem != null),
                    StrategyDebugLogger.F("hasConfirmation", confirmationDialog != null));
                return;
            }

            BuildView();
            BindActions();
            StrategyLocalization.LanguageChanged += RefreshLocalizedView;
            panelTransition.SetVisible(false, true);
            page = StrategyPauseMenuPage.Actions;
            StrategyDebugLogger.Info("PauseMenu", "Configured");
        }

        private void LateUpdate()
        {
            if (!configured || transitioning)
            {
                return;
            }

            if (!isOpen)
            {
                if (inputRouter.GlobalCancelPressed)
                {
                    Open();
                }

                return;
            }

            RefreshInputContext();
            if (inputRouter.TryConsumeCancel(this))
            {
                HandleCancel();
            }
        }

        internal bool Open()
        {
            if (!configured
                || isOpen
                || transitioning
                || !inputRouter.IsAvailable
                || inputRouter.ActiveContextCount > 0)
            {
                return false;
            }

            isOpen = true;
            page = StrategyPauseMenuPage.Actions;
            statusLocalizationKey = string.Empty;
            statusText.text = string.Empty;
            RefreshPage();
            RefreshInputContext();
            HoldPauseLock();
            panelTransition.SetVisible(true);
            Select(resumeButton);
            PlaySfx(StrategyHudSfxKind.Open);
            StrategyDebugLogger.Info(
                "PauseMenu",
                "Opened",
                StrategyDebugLogger.F("requestedScale", timeScale.CurrentScale));
            return true;
        }

        internal bool Close()
        {
            if (!isOpen || transitioning)
            {
                return false;
            }

            isOpen = false;
            page = StrategyPauseMenuPage.Actions;
            panelTransition.SetVisible(false);
            ReleaseInputContext();
            ReleasePauseLock();
            ClearSelection();
            PlaySfx(StrategyHudSfxKind.Close);
            StrategyDebugLogger.Info(
                "PauseMenu",
                "Closed",
                StrategyDebugLogger.F("restoredScale", timeScale != null ? timeScale.CurrentScale : 1f));
            return true;
        }

        internal void HandleCancel()
        {
            if (!isOpen || transitioning)
            {
                return;
            }

            if (page == StrategyPauseMenuPage.Settings)
            {
                ShowActions();
                return;
            }

            Close();
        }

        internal void ShowSettings()
        {
            if (!isOpen || transitioning || page == StrategyPauseMenuPage.Settings)
            {
                return;
            }

            page = StrategyPauseMenuPage.Settings;
            RefreshSettingsControls();
            RefreshPage();
            Select(masterSlider);
            PlaySfx(StrategyHudSfxKind.Open);
        }

        private void ShowActions()
        {
            if (!isOpen || transitioning)
            {
                return;
            }

            page = StrategyPauseMenuPage.Actions;
            RefreshPage();
            Select(settingsButton);
            PlaySfx(StrategyHudSfxKind.Close);
        }

        private void SaveGame()
        {
            if (transitioning)
            {
                return;
            }

            bool saved = saveSystem.SaveNow();
            SetStatus(
                saved ? "pause.saved" : "pause.save_failed",
                saved ? new Color(0.58f, 0.86f, 0.66f) : new Color(0.94f, 0.48f, 0.42f));
            PlaySfx(saved ? StrategyHudSfxKind.Confirm : StrategyHudSfxKind.Deny);
        }

        private void ConfirmReturnToMainMenu()
        {
            confirmationDialog.Show(
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.return.title"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.unsaved_body"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.return.accept"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.stay"),
                BeginReturnToMainMenu);
        }

        private void ConfirmQuit()
        {
            confirmationDialog.Show(
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.quit.title"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.unsaved_body"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.quit.accept"),
                StrategyLocalization.Get(StrategyLocalizationTables.Menu, "pause.confirm.stay"),
                QuitGame);
        }

        private void BeginReturnToMainMenu()
        {
            if (transitioning)
            {
                return;
            }

            transitioning = true;
            SetButtonsInteractable(false);
            SetStatus("pause.returning_to_main_menu", new Color(0.88f, 0.70f, 0.35f));
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(
                    StrategySceneCatalog.MainMenuSceneName,
                    LoadSceneMode.Single);
                if (operation != null)
                {
                    StrategyDebugLogger.Info("PauseMenu", "MainMenuRequested");
                    return;
                }
            }
            catch (Exception exception)
            {
                StrategyDebugLogger.Warn(
                    "PauseMenu",
                    "MainMenuRequestFailed",
                    StrategyDebugLogger.F("error", exception.Message));
            }

            transitioning = false;
            SetButtonsInteractable(true);
            SetStatus("pause.main_menu_open_failed", new Color(0.94f, 0.48f, 0.42f));
            Select(mainMenuButton);
            PlaySfx(StrategyHudSfxKind.Deny);
        }

        private void QuitGame()
        {
            StrategyDebugLogger.Info("PauseMenu", "QuitRequested");
#if UNITY_EDITOR
            SetStatus("pause.quit_standalone_only", new Color(0.88f, 0.70f, 0.35f));
            Select(quitButton);
#else
            transitioning = true;
            SetButtonsInteractable(false);
            Application.Quit();
#endif
        }

        private void BindActions()
        {
            resumeButton.onClick.AddListener(() => Close());
            saveButton.onClick.AddListener(SaveGame);
            settingsButton.onClick.AddListener(ShowSettings);
            mainMenuButton.onClick.AddListener(ConfirmReturnToMainMenu);
            quitButton.onClick.AddListener(ConfirmQuit);
            settingsBackButton.onClick.AddListener(ShowActions);
            masterSlider.onValueChanged.AddListener(StrategyGameSettings.SetMasterVolume);
            musicSlider.onValueChanged.AddListener(StrategyGameSettings.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(StrategyGameSettings.SetSfxVolume);
            uiScaleSlider.onValueChanged.AddListener(StrategyGameSettings.SetUiScale);
            fullscreenToggle.onValueChanged.AddListener(ChangeFullscreen);
            reducedMotionToggle.onValueChanged.AddListener(ChangeReducedMotion);
            languageButton.onClick.AddListener(ChangeLanguage);
        }

        private void RefreshInputContext()
        {
            if (!isOpen || inputRouter == null || !inputRouter.IsAvailable)
            {
                ReleaseInputContext();
                return;
            }

            if (inputContext == null || inputContext.IsDisposed)
            {
                inputContext = inputRouter.PushContext(
                    this,
                    StrategyInputChannel.All,
                    StrategyCancelMode.Close);
            }
        }

        private void HoldPauseLock()
        {
            if (pauseLockHeld || timeScale == null)
            {
                return;
            }

            timeScale.PushPauseLock(PauseLockReason);
            pauseLockHeld = true;
        }

        private void ReleasePauseLock()
        {
            if (!pauseLockHeld)
            {
                return;
            }

            if (timeScale != null && timeScale.IsPausedByLock)
            {
                timeScale.PopPauseLock(PauseLockReason);
            }

            pauseLockHeld = false;
        }

        private void ReleaseInputContext()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private void RefreshPage()
        {
            actionsRoot.SetActive(page == StrategyPauseMenuPage.Actions);
            settingsRoot.SetActive(page == StrategyPauseMenuPage.Settings);
        }

        private void RefreshSettingsControls()
        {
            masterSlider.SetValueWithoutNotify(StrategyGameSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(StrategyGameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(StrategyGameSettings.SfxVolume);
            uiScaleSlider.SetValueWithoutNotify(StrategyGameSettings.UiScale);
            fullscreenToggle.SetIsOnWithoutNotify(StrategyGameSettings.Fullscreen);
            reducedMotionToggle.SetIsOnWithoutNotify(StrategyGameSettings.ReducedMotion);
            RefreshLanguageLabel();
        }

        private static void ChangeFullscreen(bool value)
        {
            StrategyGameSettings.SetFullscreen(value);
            PlaySfx(StrategyHudSfxKind.Step);
        }

        private static void ChangeReducedMotion(bool value)
        {
            StrategyGameSettings.SetReducedMotion(value);
            PlaySfx(StrategyHudSfxKind.Step);
        }

        private void ChangeLanguage()
        {
            StrategyGameLanguage next = StrategyGameSettings.Language == StrategyGameLanguage.Russian
                ? StrategyGameLanguage.English
                : StrategyGameLanguage.Russian;
            StrategyGameSettings.SetLanguage(next);
            RefreshLocalizedView();
            PlaySfx(StrategyHudSfxKind.Step);
        }

        private void RefreshLocalizedView()
        {
            if (this == null)
            {
                StrategyLocalization.LanguageChanged -= RefreshLocalizedView;
                return;
            }

            StrategyLocalizedTextBinding[] bindings =
                GetComponentsInChildren<StrategyLocalizedTextBinding>(true);
            for (int index = 0; index < bindings.Length; index++)
            {
                bindings[index].Refresh();
            }

            RefreshLanguageLabel();
        }

        private void RefreshLanguageLabel()
        {
            if (languageButtonLabel == null)
            {
                return;
            }

            string key = StrategyGameSettings.Language == StrategyGameLanguage.English
                ? "settings.language.english"
                : "settings.language.russian";
            languageButtonLabel.text = StrategyLocalization.Get(StrategyLocalizationTables.Common, key);
            if (!string.IsNullOrEmpty(statusLocalizationKey))
            {
                statusText.text = StrategyLocalization.Get(StrategyLocalizationTables.Menu, statusLocalizationKey);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            resumeButton.interactable = interactable;
            saveButton.interactable = interactable;
            settingsButton.interactable = interactable;
            mainMenuButton.interactable = interactable;
            quitButton.interactable = interactable;
            settingsBackButton.interactable = interactable;
        }

        private void SetStatus(string key, Color color)
        {
            statusLocalizationKey = key;
            statusText.text = StrategyLocalization.Get(StrategyLocalizationTables.Menu, key);
            statusText.color = color;
        }

        private static void PlaySfx(StrategyHudSfxKind kind)
        {
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(kind);
            }
        }

        private static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnDisable()
        {
            isOpen = false;
            transitioning = false;
            page = StrategyPauseMenuPage.Actions;
            ReleaseInputContext();
            ReleasePauseLock();
            panelTransition?.SetVisible(false, true);
        }

        private void OnDestroy()
        {
            StrategyLocalization.LanguageChanged -= RefreshLocalizedView;
        }
    }
}
