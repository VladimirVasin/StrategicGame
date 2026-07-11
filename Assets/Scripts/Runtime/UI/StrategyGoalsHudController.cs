using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyGoalsHudController : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.075f, 0.92f);
        private static readonly Color RowColor = new Color(0.095f, 0.125f, 0.12f, 0.94f);
        private static readonly Color CompleteColor = new Color(0.42f, 0.78f, 0.45f, 1f);
        private static readonly Color AccentColor = new Color(0.78f, 0.59f, 0.25f, 1f);
        private const float PanelWidth = 268f;
        private const float HeaderHeight = 30f;
        private const float RowHeight = 28f;
        private const float RowSpacing = 6f;
        private const float IntroDelaySeconds = 2.1f;
        private const float IntroAnimationSeconds = 0.55f;
        private const float IntroSlideOffset = 18f;

        private readonly List<GoalRowUi> rows = new();

        private GameObject canvasRoot;
        private RectTransform panelRoot;
        private CanvasGroup canvasGroup;
        private Image flashOverlay;
        private Text titleText;
        private Vector2 panelBasePosition;
        private bool hasShownIntro;
        private bool introPending;
        private float introDelayTimer;
        private float introAnimationTimer;
        private float pulseTimer;
        private float hideTimer;
        private bool configured;

        public void Configure()
        {
            configured = true;
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }
        }

        public void SetGoals(IReadOnlyList<StrategyGoalViewState> goals)
        {
            if (goals == null || goals.Count == 0)
            {
                ClearGoals();
                return;
            }

            if (!configured)
            {
                Configure();
            }

            EnsureUi();
            hideTimer = 0f;
            canvasRoot.SetActive(true);
            PrepareIntroIfNeeded();

            titleText.text = "Goals";
            panelRoot.sizeDelta = new Vector2(PanelWidth, CalculatePanelHeight(goals.Count));

            EnsureRowCount(goals.Count);
            flashOverlay.transform.SetAsLastSibling();
            for (int i = 0; i < rows.Count; i++)
            {
                bool visible = i < goals.Count;
                rows[i].Root.gameObject.SetActive(visible);
                if (visible)
                {
                    rows[i].SetIndex(i);
                    rows[i].Apply(goals[i]);
                }
            }
        }

        public void ClearGoals()
        {
            hideTimer = 0f;
            pulseTimer = 0f;
            introPending = false;
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }
        }

        public void PlayCompletionPulse(bool hideAfterComplete)
        {
            EnsureUi();
            pulseTimer = 1.1f;
            hideTimer = hideAfterComplete ? 3.2f : 0f;
            canvasRoot.SetActive(true);
        }

        private void Update()
        {
            if (canvasRoot == null || !canvasRoot.activeSelf)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            UpdateIntro(delta);
            UpdatePulse(delta);
            if (hideTimer > 0f)
            {
                hideTimer -= delta;
                if (hideTimer <= 0f)
                {
                    ClearGoals();
                }
            }
        }

        private void EnsureUi()
        {
            if (canvasRoot != null)
            {
                return;
            }

            canvasRoot = new GameObject("GoalsHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            panelRoot = CreateUiObject("GoalsPanel", canvasRoot.transform).GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0f, 1f);
            panelRoot.anchorMax = new Vector2(0f, 1f);
            panelRoot.pivot = new Vector2(0f, 1f);
            panelBasePosition = new Vector2(18f, -206f);
            panelRoot.anchoredPosition = panelBasePosition;
            panelRoot.sizeDelta = new Vector2(PanelWidth, CalculatePanelHeight(1));

            Image panelImage = panelRoot.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;
            panelImage.raycastTarget = false;

            Outline outline = panelRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);

            canvasGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            titleText = CreateText("Title", panelRoot, "Goals", 16, TextAnchor.MiddleLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.offsetMin = new Vector2(12f, -HeaderHeight);
            titleText.rectTransform.offsetMax = new Vector2(-12f, -6f);

            flashOverlay = CreateUiObject("CompletionFlash", panelRoot).AddComponent<Image>();
            RectTransform flashRect = flashOverlay.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            flashOverlay.color = new Color(CompleteColor.r, CompleteColor.g, CompleteColor.b, 0f);
            flashOverlay.raycastTarget = false;

            canvasRoot.SetActive(false);
        }

        private void EnsureRowCount(int count)
        {
            while (rows.Count < count)
            {
                rows.Add(CreateRow(rows.Count));
            }
        }

        private GoalRowUi CreateRow(int index)
        {
            RectTransform root = CreateUiObject("GoalRow" + index, panelRoot).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            SetRowOffsets(root, index);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = RowColor;
            image.raycastTarget = false;

            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            Image checkBox = CreateUiObject("CheckBox", root).AddComponent<Image>();
            checkBox.color = new Color(0.06f, 0.08f, 0.08f, 1f);
            checkBox.raycastTarget = false;
            LayoutElement checkBoxSize = checkBox.gameObject.AddComponent<LayoutElement>();
            checkBoxSize.minWidth = 14f;
            checkBoxSize.preferredWidth = 14f;
            checkBoxSize.minHeight = 14f;
            checkBoxSize.preferredHeight = 14f;

            Outline checkOutline = checkBox.gameObject.AddComponent<Outline>();
            checkOutline.effectColor = AccentColor;
            checkOutline.effectDistance = new Vector2(1f, -1f);

            Text checkMark = CreateText("CheckMark", checkBox.transform, string.Empty, 11, TextAnchor.MiddleCenter, CompleteColor);
            Stretch(checkMark.rectTransform);
            checkMark.fontStyle = FontStyle.Bold;

            Text label = CreateText("Label", root, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            return new GoalRowUi(root, checkBox, checkMark, label);
        }

        private void PrepareIntroIfNeeded()
        {
            if (hasShownIntro || introPending)
            {
                return;
            }

            introPending = true;
            introDelayTimer = IntroDelaySeconds;
            introAnimationTimer = 0f;
            canvasGroup.alpha = 0f;
            panelRoot.anchoredPosition = panelBasePosition + new Vector2(-IntroSlideOffset, 0f);
        }

        private void UpdateIntro(float delta)
        {
            if (!introPending)
            {
                return;
            }

            if (introDelayTimer > 0f)
            {
                introDelayTimer -= delta;
                canvasGroup.alpha = 0f;
                panelRoot.anchoredPosition = panelBasePosition + new Vector2(-IntroSlideOffset, 0f);
                return;
            }

            introAnimationTimer += delta;
            float t = Mathf.Clamp01(introAnimationTimer / IntroAnimationSeconds);
            float eased = t * t * (3f - 2f * t);
            canvasGroup.alpha = eased;
            panelRoot.anchoredPosition = Vector2.Lerp(
                panelBasePosition + new Vector2(-IntroSlideOffset, 0f),
                panelBasePosition,
                eased);

            if (t >= 1f)
            {
                introPending = false;
                hasShownIntro = true;
                canvasGroup.alpha = 1f;
                panelRoot.anchoredPosition = panelBasePosition;
            }
        }

        private void UpdatePulse(float delta)
        {
            if (introPending)
            {
                flashOverlay.color = new Color(CompleteColor.r, CompleteColor.g, CompleteColor.b, 0f);
                return;
            }

            if (pulseTimer <= 0f)
            {
                panelRoot.localScale = Vector3.one;
                panelRoot.anchoredPosition = panelBasePosition;
                flashOverlay.color = new Color(CompleteColor.r, CompleteColor.g, CompleteColor.b, 0f);
                return;
            }

            pulseTimer -= delta;
            float normalized = Mathf.Clamp01(pulseTimer / 1.1f);
            float wave = Mathf.Sin((1f - normalized) * Mathf.PI);
            panelRoot.localScale = Vector3.one * (1f + wave * 0.035f);
            panelRoot.anchoredPosition = panelBasePosition + new Vector2(wave * 2f, 0f);
            flashOverlay.color = new Color(CompleteColor.r, CompleteColor.g, CompleteColor.b, wave * 0.16f);
        }

        private static float CalculatePanelHeight(int rowCount)
        {
            int safeRows = Mathf.Max(1, rowCount);
            return HeaderHeight + 10f + safeRows * RowHeight + (safeRows - 1) * RowSpacing + 10f;
        }

        private static void SetRowOffsets(RectTransform row, int index)
        {
            float top = HeaderHeight + 10f + index * (RowHeight + RowSpacing);
            row.offsetMin = new Vector2(12f, -(top + RowHeight));
            row.offsetMax = new Vector2(-12f, -top);
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class GoalRowUi
        {
            public GoalRowUi(RectTransform root, Image checkBox, Text checkMark, Text label)
            {
                Root = root;
                CheckBox = checkBox;
                CheckMark = checkMark;
                Label = label;
            }

            public RectTransform Root { get; }
            private Image CheckBox { get; }
            private Text CheckMark { get; }
            private Text Label { get; }

            public void SetIndex(int index)
            {
                SetRowOffsets(Root, index);
            }

            public void Apply(StrategyGoalViewState state)
            {
                CheckBox.color = state.Completed ? CompleteColor : new Color(0.06f, 0.08f, 0.08f, 1f);
                CheckMark.text = state.Completed ? "X" : string.Empty;
                Label.text = state.Title;
                Label.color = state.Completed ? CompleteColor : Color.white;
                Label.fontStyle = state.Completed ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
