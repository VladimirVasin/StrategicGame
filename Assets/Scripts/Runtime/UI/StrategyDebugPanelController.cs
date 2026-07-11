using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyDebugPanelController : MonoBehaviour
    {
        private const int CanvasSortingOrder = 33000;
        private const float PanelWidth = 660f;
        private const float PanelHeight = 680f;

        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.58f);
        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.07f, 0.98f);
        private static readonly Color SectionColor = new Color(0.095f, 0.13f, 0.13f, 1f);
        private static readonly Color ButtonColor = new Color(0.14f, 0.17f, 0.18f, 1f);
        private static readonly Color ActiveColor = new Color(0.56f, 0.39f, 0.15f, 1f);
        private static readonly Color TextColor = new Color(0.86f, 0.91f, 0.88f, 1f);
        private static readonly Color MutedTextColor = new Color(0.58f, 0.66f, 0.64f, 1f);

        private readonly List<WeatherButtonView> weatherButtons = new();
        private StrategyFogOfWarController fog;
        private StrategyWeatherController weather;
        private StrategyRefugeeArrivalController refugees;
        private CanvasGroup rootGroup;
        private Toggle fogToggle;
        private Toggle instantConstructionToggle;
        private Text currentWeatherText;
        private Text refugeeStatusText;
        private bool initialized;
        private bool isOpen;

        public void Configure(
            StrategyFogOfWarController fogController,
            StrategyWeatherController weatherController,
            StrategyRefugeeArrivalController refugeeController = null)
        {
            fog = fogController != null
                ? fogController
                : fog ?? Object.FindAnyObjectByType<StrategyFogOfWarController>();
            weather = weatherController != null
                ? weatherController
                : weather ?? Object.FindAnyObjectByType<StrategyWeatherController>();
            refugees = refugeeController != null
                ? refugeeController
                : refugees;

            if (!initialized)
            {
                initialized = true;
                EnsureEventSystem();
                BuildUi();
            }

            RefreshControls();
        }

        private void Awake()
        {
            Configure(null, null);
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure(null, null);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                SetOpen(!isOpen);
            }
            else if (isOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }

            if (isOpen && Time.frameCount % 10 == 0)
            {
                RefreshControls();
            }
        }

        private void SetOpen(bool open)
        {
            if (isOpen == open)
            {
                return;
            }

            isOpen = open;
            if (rootGroup == null)
            {
                return;
            }

            rootGroup.gameObject.SetActive(isOpen);
            rootGroup.alpha = isOpen ? 1f : 0f;
            rootGroup.interactable = isOpen;
            rootGroup.blocksRaycasts = isOpen;

            if (isOpen)
            {
                RefreshControls();
            }

            StrategyDebugLogger.Info("DebugPanel", isOpen ? "Opened" : "Closed");
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("StrategyDebugPanelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = CreateUiObject("DebugPanelRoot", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(root, 0f, 0f, 0f, 0f);
            Image overlay = root.gameObject.AddComponent<Image>();
            overlay.color = OverlayColor;
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform panel = CreatePanel("DebugPanel", root, PanelColor).GetComponent<RectTransform>();
            SetCentered(panel, PanelWidth, PanelHeight);

            Text title = CreateText("Title", panel, "Debug Panel", 28, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopLeft(title.rectTransform, 28f, 22f, 300f, 36f);

            Text hint = CreateText("Hint", panel, "F9 / Esc to close", 13, TextAnchor.MiddleRight, MutedTextColor);
            SetTopRight(hint.rectTransform, 28f, 28f, 160f, 22f);

            Button closeButton = CreateButton("CloseButton", panel, "Close", 14, ButtonColor);
            SetTopRight(closeButton.GetComponent<RectTransform>(), 28f, 60f, 92f, 34f);
            closeButton.onClick.AddListener(() => SetOpen(false));

            RectTransform fogSection = CreatePanel("FogSection", panel, SectionColor).GetComponent<RectTransform>();
            SetTopLeft(fogSection, 28f, 112f, PanelWidth - 56f, 84f);
            Text fogTitle = CreateText("FogTitle", fogSection, "Visibility", 16, TextAnchor.MiddleLeft, Color.white);
            fogTitle.fontStyle = FontStyle.Bold;
            SetTopLeft(fogTitle.rectTransform, 18f, 10f, 180f, 24f);
            fogToggle = CreateToggle("FogToggle", fogSection, "Disable Fog of War");
            SetTopLeft(fogToggle.GetComponent<RectTransform>(), 18f, 42f, 260f, 28f);
            fogToggle.onValueChanged.AddListener(SetFogDisabled);

            RectTransform buildSection = CreatePanel("BuildSection", panel, SectionColor).GetComponent<RectTransform>();
            SetTopLeft(buildSection, 28f, 216f, PanelWidth - 56f, 84f);
            Text buildTitle = CreateText("BuildTitle", buildSection, "Construction", 16, TextAnchor.MiddleLeft, Color.white);
            buildTitle.fontStyle = FontStyle.Bold;
            SetTopLeft(buildTitle.rectTransform, 18f, 10f, 180f, 24f);
            instantConstructionToggle = CreateToggle("InstantConstructionToggle", buildSection, "Instant Construction");
            SetTopLeft(instantConstructionToggle.GetComponent<RectTransform>(), 18f, 42f, 300f, 28f);
            instantConstructionToggle.onValueChanged.AddListener(SetInstantConstruction);

            RectTransform populationSection = CreatePanel("PopulationSection", panel, SectionColor).GetComponent<RectTransform>();
            SetTopLeft(populationSection, 28f, 320f, PanelWidth - 56f, 84f);
            Text populationTitle = CreateText("PopulationTitle", populationSection, "Population", 16, TextAnchor.MiddleLeft, Color.white);
            populationTitle.fontStyle = FontStyle.Bold;
            SetTopLeft(populationTitle.rectTransform, 18f, 10f, 180f, 24f);
            Button summonRefugeesButton = CreateButton("SummonRefugeesButton", populationSection, "Summon Refugees", 14, ButtonColor);
            SetTopLeft(summonRefugeesButton.GetComponent<RectTransform>(), 18f, 42f, 180f, 30f);
            summonRefugeesButton.onClick.AddListener(SummonRefugees);
            refugeeStatusText = CreateText("RefugeeStatus", populationSection, string.Empty, 12, TextAnchor.MiddleRight, MutedTextColor);
            SetTopRight(refugeeStatusText.rectTransform, 18f, 46f, 260f, 24f);

            RectTransform weatherSection = CreatePanel("WeatherSection", panel, SectionColor).GetComponent<RectTransform>();
            SetTopLeft(weatherSection, 28f, 424f, PanelWidth - 56f, 202f);
            Text weatherTitle = CreateText("WeatherTitle", weatherSection, "Weather", 16, TextAnchor.MiddleLeft, Color.white);
            weatherTitle.fontStyle = FontStyle.Bold;
            SetTopLeft(weatherTitle.rectTransform, 18f, 10f, 180f, 24f);
            currentWeatherText = CreateText("CurrentWeather", weatherSection, string.Empty, 13, TextAnchor.MiddleRight, MutedTextColor);
            SetTopRight(currentWeatherText.rectTransform, 18f, 12f, 220f, 22f);
            BuildWeatherButtons(weatherSection);

            isOpen = false;
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            rootGroup.gameObject.SetActive(false);
        }

        private void SetFogDisabled(bool disabled)
        {
            if (fog == null)
            {
                fog = Object.FindAnyObjectByType<StrategyFogOfWarController>();
            }

            fog?.SetPlayerFogEnabled(!disabled);
            RefreshControls();
        }

        private void SetInstantConstruction(bool enabled)
        {
            StrategyDebugOptions.SetInstantConstructionEnabled(enabled);
            if (enabled)
            {
                StrategyConstructionSite.DebugCompleteAllActiveSites();
            }

            RefreshControls();
        }

        private void SummonRefugees()
        {
            if (refugees == null)
            {
                refugees = Object.FindAnyObjectByType<StrategyRefugeeArrivalController>();
            }

            bool started = refugees != null && refugees.DebugStartArrival();
            if (refugeeStatusText != null)
            {
                refugeeStatusText.text = started ? "Arrival started" : "Arrival unavailable";
            }

            StrategyDebugLogger.Info(
                "DebugPanel",
                "RefugeesSummoned",
                StrategyDebugLogger.F("started", started),
                StrategyDebugLogger.F("hasController", refugees != null));
        }

        private void RefreshControls()
        {
            if (fogToggle != null)
            {
                fogToggle.SetIsOnWithoutNotify(fog != null && !fog.IsPlayerFogEnabled);
            }

            if (instantConstructionToggle != null)
            {
                instantConstructionToggle.SetIsOnWithoutNotify(StrategyDebugOptions.InstantConstructionEnabled);
            }

            StrategyWeatherKind current = weather != null ? weather.CurrentWeather : StrategyWeatherKind.Clear;
            if (currentWeatherText != null)
            {
                currentWeatherText.text = weather != null
                    ? "Current: " + GetWeatherLabel(current)
                    : "Current: unavailable";
            }

            if (refugeeStatusText != null
                && (string.IsNullOrEmpty(refugeeStatusText.text)
                    || refugeeStatusText.text == "Ready"
                    || refugeeStatusText.text == "Controller unavailable"))
            {
                refugeeStatusText.text = refugees != null ? "Ready" : "Controller unavailable";
            }

            for (int i = 0; i < weatherButtons.Count; i++)
            {
                WeatherButtonView view = weatherButtons[i];
                bool active = weather != null && view.Kind == current;
                view.Image.color = active ? ActiveColor : ButtonColor;
                view.Label.color = active ? Color.white : TextColor;
            }
        }

        private static Button CreateButton(string name, Transform parent, string label, int fontSize, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(color.r + 0.06f, color.g + 0.06f, color.b + 0.06f, color.a);
            colors.pressedColor = ActiveColor;
            button.colors = colors;

            Text text = CreateText("Label", root, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform, 6f, 0f, 6f, 0f);
            return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Toggle toggle = root.gameObject.AddComponent<Toggle>();

            RectTransform box = CreateUiObject("Box", root).GetComponent<RectTransform>();
            SetTopLeft(box, 0f, 3f, 22f, 22f);
            Image boxImage = box.gameObject.AddComponent<Image>();
            boxImage.color = new Color(0.18f, 0.22f, 0.21f, 1f);

            RectTransform check = CreateUiObject("Checkmark", box).GetComponent<RectTransform>();
            Stretch(check, 5f, 5f, 5f, 5f);
            Image checkImage = check.gameObject.AddComponent<Image>();
            checkImage.color = ActiveColor;

            Text labelText = CreateText("Label", root, label, 14, TextAnchor.MiddleLeft, TextColor);
            SetTopLeft(labelText.rectTransform, 32f, 0f, 220f, 28f);

            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject obj = CreateUiObject(name, parent);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return obj;
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

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.enabled = true;
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
            rect.sizeDelta = new Vector2(width, height);
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
