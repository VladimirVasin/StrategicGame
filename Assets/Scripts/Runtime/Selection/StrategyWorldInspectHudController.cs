using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWorldInspectHudController : MonoBehaviour
    {
        private const float Width = 326f;
        private const float Height = 210f;
        private const float AnimationSpeed = 10f;
        private const float BottomInset = 18f;
        private const int MaxChips = 3;
        private const int MaxRows = 4;

        private RectTransform panel;
        private CanvasGroup group;
        private Image accentImage;
        private Image iconImage;
        private Text titleText;
        private Text subtitleText;
        private Text bodyText;
        private readonly RectTransform[] chipRects = new RectTransform[MaxChips];
        private readonly Image[] chipBackgrounds = new Image[MaxChips];
        private readonly Image[] chipIcons = new Image[MaxChips];
        private readonly Text[] chipTexts = new Text[MaxChips];
        private readonly RectTransform[] rowRects = new RectTransform[MaxRows];
        private readonly Image[] rowBackgrounds = new Image[MaxRows];
        private readonly Image[] rowIcons = new Image[MaxRows];
        private readonly Text[] rowLabelTexts = new Text[MaxRows];
        private readonly Text[] rowValueTexts = new Text[MaxRows];
        private float visibility;
        private float targetVisibility;
        private float rightInset = 18f;

        public void Configure(float initialRightInset)
        {
            rightInset = Mathf.Max(0f, initialRightInset);
            EnsureUi();
            ApplyPosition();
        }

        public void SetRightInset(float inset)
        {
            rightInset = Mathf.Max(0f, inset);
        }

        public void Show(StrategyWorldInspectInfo info)
        {
            if (!info.IsValid)
            {
                Hide();
                return;
            }

            EnsureUi();
            titleText.text = info.Title;
            subtitleText.text = BuildSubtitle(info);
            iconImage.sprite = info.Icon;
            iconImage.color = info.Icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            accentImage.color = info.AccentColor;
            ApplyStructuredContent(info);
            targetVisibility = 1f;
        }

        public void Hide()
        {
            targetVisibility = 0f;
        }

        private void Update()
        {
            if (panel == null || group == null)
            {
                return;
            }

            visibility = Mathf.MoveTowards(visibility, targetVisibility, Time.unscaledDeltaTime * AnimationSpeed);
            float eased = visibility * visibility * (3f - 2f * visibility);
            group.alpha = eased;
            group.blocksRaycasts = false;
            group.interactable = false;
            ApplyPosition();
        }

        private void EnsureUi()
        {
            if (panel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("WorldInspectHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 27;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            panel = CreateUiObject("WorldInspectPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.sizeDelta = new Vector2(Width, Height);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.050f, 0.047f, 0.94f);
            background.raycastTarget = false;

            group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform accent = CreateUiObject("Accent", panel).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.sizeDelta = new Vector2(4f, 0f);
            accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.86f, 0.62f, 0.26f, 1f);
            accentImage.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", panel).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 18f, 18f, 54f, 54f);
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.color = new Color(1f, 1f, 1f, 0.08f);
            iconFrameImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 5f, 5f, 5f, 5f);
            iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            titleText = CreateText("Title", panel, 19, TextAnchor.UpperLeft, Color.white);
            SetTopLeft(titleText.rectTransform, 84f, 18f, 218f, 28f);
            subtitleText = CreateText("Subtitle", panel, 12, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            SetTopLeft(subtitleText.rectTransform, 84f, 48f, 218f, 20f);

            RectTransform divider = CreateUiObject("Divider", panel).GetComponent<RectTransform>();
            SetTopLeft(divider, 18f, 82f, Width - 36f, 2f);
            Image dividerImage = divider.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.22f);
            dividerImage.raycastTarget = false;

            bodyText = CreateText("Body", panel, 13, TextAnchor.UpperLeft, new Color(0.84f, 0.90f, 0.88f));
            SetTopLeft(bodyText.rectTransform, 18f, 96f, Width - 36f, 54f);
            CreateChips();
            CreateRows();
        }

        private void ApplyStructuredContent(StrategyWorldInspectInfo info)
        {
            bool structured = info.HasStructuredContent;
            bodyText.gameObject.SetActive(!structured);
            if (!structured)
            {
                bodyText.text = info.Body;
                SetChipsVisible(0);
                SetRowsVisible(0);
                return;
            }

            ApplyChips(info.Chips);
            ApplyRows(info.Rows);
        }

        private void ApplyChips(StrategyWorldInspectChip[] chips)
        {
            int visible = 0;
            if (chips != null)
            {
                for (int i = 0; i < chips.Length && visible < MaxChips; i++)
                {
                    StrategyWorldInspectChip chip = chips[i];
                    if (!chip.IsValid)
                    {
                        continue;
                    }

                    chipRects[visible].gameObject.SetActive(true);
                    chipBackgrounds[visible].color = chip.Color;
                    chipIcons[visible].sprite = chip.Icon;
                    chipIcons[visible].color = chip.Icon != null ? Color.white : Color.clear;
                    chipTexts[visible].text = chip.Label;
                    visible++;
                }
            }

            SetChipsVisible(visible);
        }

        private void ApplyRows(StrategyWorldInspectRow[] rows)
        {
            int visible = 0;
            if (rows != null)
            {
                for (int i = 0; i < rows.Length && visible < MaxRows; i++)
                {
                    StrategyWorldInspectRow row = rows[i];
                    if (!row.IsValid)
                    {
                        continue;
                    }

                    rowRects[visible].gameObject.SetActive(true);
                    rowBackgrounds[visible].color = row.Color;
                    rowIcons[visible].sprite = row.Icon;
                    rowIcons[visible].color = row.Icon != null ? Color.white : Color.clear;
                    rowLabelTexts[visible].text = row.Label;
                    rowValueTexts[visible].text = row.Value;
                    visible++;
                }
            }

            SetRowsVisible(visible);
        }

        private void SetChipsVisible(int visibleCount)
        {
            for (int i = 0; i < MaxChips; i++)
            {
                chipRects[i].gameObject.SetActive(i < visibleCount);
            }
        }

        private void SetRowsVisible(int visibleCount)
        {
            for (int i = 0; i < MaxRows; i++)
            {
                rowRects[i].gameObject.SetActive(i < visibleCount);
            }
        }

        private void CreateChips()
        {
            for (int i = 0; i < MaxChips; i++)
            {
                RectTransform chip = CreateUiObject("Chip_" + i, panel).GetComponent<RectTransform>();
                SetTopLeft(chip, 18f + i * 98f, 94f, 90f, 28f);
                chipRects[i] = chip;
                chipBackgrounds[i] = chip.gameObject.AddComponent<Image>();
                chipBackgrounds[i].color = new Color(0.10f, 0.15f, 0.14f, 0.92f);
                chipBackgrounds[i].raycastTarget = false;

                RectTransform icon = CreateUiObject("Icon", chip).GetComponent<RectTransform>();
                SetTopLeft(icon, 6f, 5f, 18f, 18f);
                chipIcons[i] = icon.gameObject.AddComponent<Image>();
                chipIcons[i].preserveAspect = true;
                chipIcons[i].raycastTarget = false;

                chipTexts[i] = CreateText("Text", chip, 10, TextAnchor.MiddleLeft, Color.white);
                chipTexts[i].fontStyle = FontStyle.Bold;
                chipTexts[i].resizeTextForBestFit = true;
                chipTexts[i].resizeTextMinSize = 8;
                chipTexts[i].resizeTextMaxSize = 10;
                SetOffsets(chipTexts[i].rectTransform, 28f, 0f, 5f, 0f);
                chip.gameObject.SetActive(false);
            }
        }

        private void CreateRows()
        {
            for (int i = 0; i < MaxRows; i++)
            {
                RectTransform row = CreateUiObject("Row_" + i, panel).GetComponent<RectTransform>();
                SetTopLeft(row, 18f, 130f + i * 18f, Width - 36f, 16f);
                rowRects[i] = row;
                rowBackgrounds[i] = row.gameObject.AddComponent<Image>();
                rowBackgrounds[i].color = new Color(0.08f, 0.11f, 0.10f, 0.82f);
                rowBackgrounds[i].raycastTarget = false;

                RectTransform icon = CreateUiObject("Icon", row).GetComponent<RectTransform>();
                SetTopLeft(icon, 5f, 2f, 12f, 12f);
                rowIcons[i] = icon.gameObject.AddComponent<Image>();
                rowIcons[i].preserveAspect = true;
                rowIcons[i].raycastTarget = false;

                rowLabelTexts[i] = CreateText("Label", row, 10, TextAnchor.MiddleLeft, new Color(0.86f, 0.70f, 0.42f));
                rowLabelTexts[i].fontStyle = FontStyle.Bold;
                SetOffsets(rowLabelTexts[i].rectTransform, 22f, 0f, 162f, 0f);

                rowValueTexts[i] = CreateText("Value", row, 10, TextAnchor.MiddleRight, new Color(0.84f, 0.90f, 0.88f));
                rowValueTexts[i].fontStyle = FontStyle.Bold;
                SetOffsets(rowValueTexts[i].rectTransform, 130f, 0f, 8f, 0f);
                row.gameObject.SetActive(false);
            }
        }

        private string BuildSubtitle(StrategyWorldInspectInfo info)
        {
            if (!info.HasCell)
            {
                return info.Subtitle;
            }

            string cellText = StrategySelectionLocalization.Text(
                "format.cell",
                info.Cell.x,
                info.Cell.y);
            return string.IsNullOrWhiteSpace(info.Subtitle)
                ? cellText
                : StrategySelectionLocalization.Text(
                    "format.subtitle_cell",
                    info.Subtitle,
                    cellText);
        }

        private void ApplyPosition()
        {
            if (panel == null)
            {
                return;
            }

            panel.anchoredPosition = new Vector2(-rightInset, BottomInset - (1f - visibility) * 18f);
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = StrategyUiThemeProvider.Font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetOffsets(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }
    }
}
