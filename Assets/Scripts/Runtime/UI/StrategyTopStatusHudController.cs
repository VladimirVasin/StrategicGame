using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyTopStatusHudController : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;

        private StrategyPopulationController population;
        private StrategyPopulationRosterHudController rosterHud;
        private Text populationText;
        private bool initialized;
        private float refreshTimer;

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

            if (!initialized)
            {
                initialized = true;
                BuildUi();
            }

            RefreshPopulationText();
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
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("TopStatusHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 26;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreateUiObject("PopulationPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(400f, -18f);
            panel.sizeDelta = new Vector2(250f, 42f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.13f, 0.92f);
            background.raycastTarget = true;

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(TogglePopulationRoster);

            populationText = CreateText("PopulationText", panel, string.Empty, 15, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            populationText.fontStyle = FontStyle.Bold;
            Stretch(populationText.rectTransform, 8f, 0f, 8f, 0f);
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
            populationText.text = "Population " + total
                + "   adults " + adults
                + " / children " + children;
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
