using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyMainMenuController : MonoBehaviour
    {
        private StrategyMapPreloadCoordinator preloader;
        private Camera menuCamera;
        private Button continueButton;
        private Button newButton;
        private Button settingsButton;
        private Button quitButton;
        private Button settingsBackButton;
        private Button languageButton;
        private GameObject actionsRoot;
        private GameObject settingsRoot;
        private GameObject loadingRoot;
        private Text continueDetailText;
        private Text preloadStatusText;
        private Text loadingStatusText;
        private Text loadingPercentText;
        private Text languageButtonLabel;
        private Image progressFill;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider uiScaleSlider;
        private Toggle fullscreenToggle;
        private Toggle reducedMotionToggle;
        private bool configured;
        private bool settingsOpen;
        private StrategyInputRouter inputRouter;
        private StrategyInputContextHandle inputContext;

        public bool IsConfigured => configured;

        public void SetInputRouter(StrategyInputRouter router)
        {
            inputContext?.Dispose();
            inputContext = null;
            inputRouter = router;
            RefreshInputContext();
        }

        public void Configure(StrategyMapPreloadCoordinator preloadCoordinator, Camera camera)
        {
            if (configured)
            {
                return;
            }

            preloader = preloadCoordinator;
            menuCamera = camera;
            configured = preloader != null;
            BuildView();
            BindActions();
            StrategyLocalization.LanguageChanged += RefreshLocalizedView;
            RefreshSettingsControls();
            RefreshView();
            StartCoroutine(StartMenuSfxAfterFirstFrame());
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            RefreshInputContext();
            if (settingsOpen && inputRouter != null && inputRouter.TryConsumeCancel(this))
            {
                CloseSettings();
            }

            RefreshView();
        }

        private void BindActions()
        {
            continueButton.onClick.AddListener(ContinueGame);
            newButton.onClick.AddListener(StartNewSettlement);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(QuitGame);
            settingsBackButton.onClick.AddListener(CloseSettings);
            masterSlider.onValueChanged.AddListener(ChangeMasterVolume);
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            sfxSlider.onValueChanged.AddListener(ChangeSfxVolume);
            uiScaleSlider.onValueChanged.AddListener(ChangeUiScale);
            fullscreenToggle.onValueChanged.AddListener(ChangeFullscreen);
            reducedMotionToggle.onValueChanged.AddListener(ChangeReducedMotion);
            languageButton.onClick.AddListener(ChangeLanguage);
        }

        private void RefreshView()
        {
            bool launching = preloader != null && preloader.IsLaunchRequested;
            continueButton.interactable = !launching && preloader != null && preloader.HasValidSave;
            newButton.interactable = !launching;
            settingsButton.interactable = !launching;
            quitButton.interactable = !launching;
            continueDetailText.text = preloader != null
                ? preloader.SaveSummary
                : StrategyLocalization.Get(StrategyLocalizationTables.Menu, "menu.save.none");

            if (!launching)
            {
                loadingRoot.SetActive(false);
                actionsRoot.SetActive(!settingsOpen);
                settingsRoot.SetActive(settingsOpen);
                if (preloader != null)
                {
                    preloadStatusText.text = StrategyLocalization.Get(
                        StrategyLocalizationTables.Menu,
                        "menu.preload.progress",
                        preloader.Stage,
                        Mathf.RoundToInt(preloader.Progress * 100f));
                }

                return;
            }

            actionsRoot.SetActive(false);
            settingsRoot.SetActive(false);
            loadingRoot.SetActive(true);
            float progress = preloader != null ? preloader.Progress : 0f;
            loadingStatusText.text = preloader != null
                ? preloader.Stage
                : StrategyLocalization.Get(StrategyLocalizationTables.Menu, "menu.preload.preparing_settlement");
            loadingPercentText.text = Mathf.RoundToInt(progress * 100f) + "%";
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void ContinueGame()
        {
            if (preloader == null || !preloader.RequestContinue())
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
                return;
            }

            settingsOpen = false;
            RefreshInputContext();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
        }

        private void StartNewSettlement()
        {
            if (preloader == null || !preloader.RequestNewSettlement())
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Deny);
                return;
            }

            settingsOpen = false;
            RefreshInputContext();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
        }

        private void OpenSettings()
        {
            settingsOpen = true;
            RefreshInputContext();
            RefreshSettingsControls();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Open);
        }

        private void CloseSettings()
        {
            settingsOpen = false;
            RefreshInputContext();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Close);
        }

        private void RefreshInputContext()
        {
            if (!settingsOpen || inputRouter == null || !inputRouter.IsAvailable)
            {
                inputContext?.Dispose();
                inputContext = null;
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

        private void OnDisable()
        {
            inputContext?.Dispose();
            inputContext = null;
        }

        private static void QuitGame()
        {
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Confirm);
            Application.Quit();
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

        private static void ChangeMasterVolume(float value)
        {
            StrategyGameSettings.SetMasterVolume(value);
        }

        private static void ChangeMusicVolume(float value)
        {
            StrategyGameSettings.SetMusicVolume(value);
        }

        private static void ChangeSfxVolume(float value)
        {
            StrategyGameSettings.SetSfxVolume(value);
        }

        private static void ChangeUiScale(float value)
        {
            StrategyGameSettings.SetUiScale(value);
        }

        private static void ChangeFullscreen(bool value)
        {
            StrategyGameSettings.SetFullscreen(value);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
        }

        private static void ChangeReducedMotion(bool value)
        {
            StrategyGameSettings.SetReducedMotion(value);
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
        }

        private void ChangeLanguage()
        {
            StrategyGameLanguage next = StrategyGameSettings.Language == StrategyGameLanguage.Russian
                ? StrategyGameLanguage.English
                : StrategyGameLanguage.Russian;
            StrategyGameSettings.SetLanguage(next);
            RefreshLocalizedView();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
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
        }

        private void OnDestroy()
        {
            StrategyLocalization.LanguageChanged -= RefreshLocalizedView;
        }

        private IEnumerator StartMenuSfxAfterFirstFrame()
        {
            yield return null;
            StrategyMusicController menuMusic = FindAnyObjectByType<StrategyMusicController>();
            if (menuMusic != null)
            {
                Destroy(menuMusic.gameObject);
            }

            StrategyAudioMixController mix = StrategyAudioMixController.Active;
            if (mix == null)
            {
                mix = new GameObject("Strategy Audio Mix").AddComponent<StrategyAudioMixController>();
            }

            mix.Configure(menuCamera);
        }
    }
}
