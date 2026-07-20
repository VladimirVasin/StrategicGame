using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTopStatusHudController : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;
        private const float DayProgressWidth = 218f;

        private StrategyPopulationController population;
        private StrategyPopulationRosterHudController rosterHud;
        private StrategyDayNightCycleController dayNight;
        private Text populationText;
        private Text timeText;
        private Text temperatureText;
        private Text phaseText;
        private StrategyHudTooltip populationTooltip;
        private Image phaseSwatch;
        private Image dayProgressImage;
        private RectTransform dayProgressFill;
        private bool initialized;
        private float refreshTimer;

        public void Configure(
            StrategyPopulationController populationController,
            StrategyPopulationRosterHudController rosterController,
            StrategyDayNightCycleController dayNightController)
        {
            dayNight = dayNightController != null ? dayNightController : dayNight;
            Configure(populationController, rosterController);
        }

        public void Configure(StrategyPopulationController populationController, StrategyPopulationRosterHudController rosterController)
        {
            rosterHud = rosterController != null ? rosterController : rosterHud;
            Configure(populationController);
        }

        public void Configure(StrategyPopulationController populationController)
        {
            population = populationController != null
                ? populationController
                : population ?? Object.FindAnyObjectByType<StrategyPopulationController>();
            dayNight ??= Object.FindAnyObjectByType<StrategyDayNightCycleController>();

            if (!initialized)
            {
                initialized = true;
                BuildUi();
            }

            RefreshPopulationText();
            RefreshTimeText();
        }

        private void Awake()
        {
            Configure(null);
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure(null);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshInterval;
            RefreshPopulationText();
            RefreshTimeText();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("TopStatusHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            RectTransform panel = CreateUiObject("PopulationPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(612f, -5f);
            panel.sizeDelta = new Vector2(196f, 60f);

            Image background = panel.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleRailModule(background, true);
            background.raycastTarget = true;

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(TogglePopulationRoster);
            StrategyUiButtonFeedback.Attach(button);
            populationTooltip = StrategyHudTooltip.Attach(
                panel.gameObject,
                StrategyLocalization.Get(
                    StrategyLocalizationTables.Hud,
                    "hud.population.tooltip"));

            populationText = CreateText("PopulationText", panel, string.Empty, 13, TextAnchor.MiddleCenter, StrategyHudStyle.TextPrimary);
            populationText.fontStyle = FontStyle.Bold;
            populationText.lineSpacing = 0.90f;
            Stretch(populationText.rectTransform, 8f, 0f, 8f, 0f);

            BuildTimePanel(canvasObject.transform);
        }

        private void BuildTimePanel(Transform parent)
        {
            RectTransform panel = CreateUiObject("CalendarTimePanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-16f, -3f);
            panel.sizeDelta = new Vector2(290f, 64f);

            Image background = panel.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleRailModule(background);
            background.raycastTarget = false;

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.40f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            RectTransform swatch = CreateUiObject("TimePhaseSwatch", panel).GetComponent<RectTransform>();
            swatch.anchorMin = new Vector2(0f, 1f);
            swatch.anchorMax = new Vector2(0f, 1f);
            swatch.pivot = new Vector2(0f, 1f);
            swatch.anchoredPosition = new Vector2(12f, -13f);
            swatch.sizeDelta = new Vector2(12f, 12f);
            phaseSwatch = swatch.gameObject.AddComponent<Image>();
            phaseSwatch.color = new Color(0.95f, 0.88f, 0.62f);
            phaseSwatch.raycastTarget = false;

            timeText = CreateText("CalendarTimeText", panel, string.Empty, 16, TextAnchor.MiddleLeft, new Color(0.95f, 0.91f, 0.74f));
            timeText.fontStyle = FontStyle.Bold;
            RectTransform timeRect = timeText.rectTransform;
            timeRect.anchorMin = new Vector2(0f, 1f);
            timeRect.anchorMax = new Vector2(1f, 1f);
            timeRect.pivot = new Vector2(0f, 1f);
            timeRect.anchoredPosition = new Vector2(32f, -7f);
            timeRect.sizeDelta = new Vector2(-152f, 22f);

            temperatureText = CreateText("CalendarTemperatureText", panel, string.Empty, 15, TextAnchor.MiddleRight, new Color(0.84f, 0.92f, 0.84f));
            temperatureText.fontStyle = FontStyle.Bold;
            RectTransform temperatureRect = temperatureText.rectTransform;
            temperatureRect.anchorMin = new Vector2(1f, 1f);
            temperatureRect.anchorMax = new Vector2(1f, 1f);
            temperatureRect.pivot = new Vector2(1f, 1f);
            temperatureRect.anchoredPosition = new Vector2(-18f, -7f);
            temperatureRect.sizeDelta = new Vector2(76f, 22f);

            phaseText = CreateText("CalendarPhaseText", panel, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.78f, 0.87f, 1f));
            RectTransform phaseRect = phaseText.rectTransform;
            phaseRect.anchorMin = new Vector2(0f, 1f);
            phaseRect.anchorMax = new Vector2(1f, 1f);
            phaseRect.pivot = new Vector2(0f, 1f);
            phaseRect.anchoredPosition = new Vector2(32f, -30f);
            phaseRect.sizeDelta = new Vector2(-44f, 18f);

            RectTransform track = CreateUiObject("DayProgressTrack", panel).GetComponent<RectTransform>();
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(0f, 0f);
            track.pivot = new Vector2(0f, 0f);
            track.anchoredPosition = new Vector2(32f, 1f);
            track.sizeDelta = new Vector2(DayProgressWidth, 3f);

            Image trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.16f, 0.20f, 0.22f, 0.95f);
            trackImage.raycastTarget = false;

            dayProgressFill = CreateUiObject("DayProgressFill", track).GetComponent<RectTransform>();
            dayProgressFill.anchorMin = new Vector2(0f, 0f);
            dayProgressFill.anchorMax = new Vector2(0f, 1f);
            dayProgressFill.pivot = new Vector2(0f, 0.5f);
            dayProgressFill.anchoredPosition = Vector2.zero;
            dayProgressFill.sizeDelta = new Vector2(0f, 0f);

            dayProgressImage = dayProgressFill.gameObject.AddComponent<Image>();
            dayProgressImage.color = new Color(0.95f, 0.88f, 0.62f);
            dayProgressImage.raycastTarget = false;
        }

        private void RefreshPopulationText()
        {
            if (populationText == null)
            {
                return;
            }

            if (population == null)
            {
                population = Object.FindAnyObjectByType<StrategyPopulationController>();
            }

            int adults = population != null ? population.AdultResidentCount : 0;
            int children = population != null ? population.ChildResidentCount : 0;
            int total = adults + children;
            populationText.text = StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.population.summary",
                total,
                adults,
                children);
            populationTooltip?.SetText(StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.population.tooltip"));
        }

        private void RefreshTimeText()
        {
            if (timeText == null || phaseText == null)
            {
                return;
            }

            dayNight ??= Object.FindAnyObjectByType<StrategyDayNightCycleController>();
            StrategyCalendarSnapshot snapshot = dayNight != null
                ? dayNight.CurrentSnapshot
                : StrategyDayNightCycleController.CurrentCalendarSnapshot;
            Color accent = StrategyDayNightCycleController.GetPhaseAccentColor(snapshot.Phase);
            Color seasonAccent = StrategySeasonCalendar.GetSeasonAccentColor(snapshot.Season);
            StrategyTemperatureSnapshot temperature =
                StrategyTemperatureModel.Evaluate(snapshot, StrategyWeatherController.Active);

            timeText.text = StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.calendar.day_time",
                snapshot.DisplayDay,
                snapshot.ClockText);
            if (temperatureText != null)
            {
                temperatureText.text = temperature.CelsiusText;
                temperatureText.color = StrategyTemperatureModel.GetTemperatureColor(temperature.Celsius);
            }

            string season = StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.season." + snapshot.Season.ToString().ToLowerInvariant());
            string phase = StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.phase." + snapshot.Phase.ToString().ToLowerInvariant());
            phaseText.text = StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                "hud.calendar.season_phase",
                season,
                snapshot.SeasonDay,
                StrategySeasonCalendar.DaysPerSeason,
                phase);
            phaseText.color = Color.Lerp(seasonAccent, accent, 0.35f);

            if (phaseSwatch != null)
            {
                phaseSwatch.color = accent;
            }

            if (dayProgressFill != null)
            {
                dayProgressFill.sizeDelta = new Vector2(
                    DayProgressWidth * Mathf.Clamp01(snapshot.DayPhase),
                    0f);
                if (dayProgressImage != null)
                {
                    dayProgressImage.color = accent;
                }
            }
        }

        private void TogglePopulationRoster()
        {
            if (rosterHud == null)
            {
                rosterHud = Object.FindAnyObjectByType<StrategyPopulationRosterHudController>();
            }

            if (rosterHud == null)
            {
                GameObject rosterObject = new GameObject("Strategy Population Roster HUD");
                rosterHud = rosterObject.AddComponent<StrategyPopulationRosterHudController>();
            }

            rosterHud.Configure(population);
            rosterHud.Toggle();
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = StrategyUiThemeProvider.Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
