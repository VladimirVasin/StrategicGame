using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed class StrategyBuildingSelectionHudRenderer
    {
        private const float ChipWidth = 112f;
        private const float ChipHeight = 34f;
        private const float ChipGap = 8f;
        private const float SectionTitleHeight = 20f;
        private const float RowHeight = 48f;
        private const float RowGap = 4f;
        private const float SectionGap = 12f;
        private const float StatusHeight = 68f;

        private readonly RectTransform root;
        private readonly ChipView[] chips =
            new ChipView[StrategyBuildingHudSnapshot.MaxChips];
        private readonly SectionView[] sections =
            new SectionView[StrategyBuildingHudSnapshot.MaxSections];
        private readonly StatusView status;

        public StrategyBuildingSelectionHudRenderer(RectTransform parent)
        {
            root = CreateUiObject("BuildingHud", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.gameObject.SetActive(false);

            for (int i = 0; i < chips.Length; i++)
            {
                chips[i] = CreateChip(i);
            }

            for (int i = 0; i < sections.Length; i++)
            {
                sections[i] = CreateSection(i);
            }

            status = CreateStatus();
        }

        internal RectTransform Root => root;
        internal float ContentHeight { get; private set; }
        internal int VisibleSectionCount { get; private set; }
        internal StrategyBuildTool CurrentTool { get; private set; }
        internal bool IsVisible => root != null && root.gameObject.activeSelf;

        public float Show(StrategyBuildingHudSnapshot snapshot, float top)
        {
            if (snapshot == null)
            {
                Hide();
                return top;
            }

            CurrentTool = snapshot.Tool;
            float cursor = 0f;
            BindChips(snapshot, ref cursor);
            BindSections(snapshot, ref cursor);
            BindStatus(snapshot, ref cursor);
            ContentHeight = Mathf.Max(1f, cursor);
            SetTopStretch(root, 18f, top, 18f, ContentHeight);
            root.gameObject.SetActive(true);
            return top + ContentHeight;
        }

        public void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }

            ContentHeight = 0f;
            VisibleSectionCount = 0;
        }

        private void BindChips(StrategyBuildingHudSnapshot snapshot, ref float cursor)
        {
            int count = Mathf.Min(snapshot.ChipCount, chips.Length);
            for (int i = 0; i < chips.Length; i++)
            {
                bool visible = i < count;
                chips[i].Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                StrategyBuildingHudChip data = snapshot.GetChip(i);
                SetTopLeft(
                    chips[i].Root,
                    i * (ChipWidth + ChipGap),
                    cursor,
                    ChipWidth,
                    ChipHeight);
                chips[i].Label.text = data.Label;
                chips[i].Value.text = data.Value;
                chips[i].Icon.sprite = data.Icon;
                chips[i].Icon.color = data.Icon != null ? Color.white : Color.clear;
                chips[i].Background.color = GetSurfaceColor(data.Tone, 0.94f);
                chips[i].Accent.color = GetToneColor(data.Tone);
            }

            if (count > 0)
            {
                cursor += ChipHeight + SectionGap;
            }
        }

        private void BindSections(StrategyBuildingHudSnapshot snapshot, ref float cursor)
        {
            VisibleSectionCount = Mathf.Min(snapshot.SectionCount, sections.Length);
            for (int i = 0; i < sections.Length; i++)
            {
                bool visible = i < VisibleSectionCount;
                sections[i].Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                StrategyBuildingHudSection data = snapshot.GetSection(i);
                int rowCount = data != null
                    ? Mathf.Min(data.RowCount, sections[i].Rows.Length)
                    : 0;
                float sectionHeight = SectionTitleHeight
                    + rowCount * (RowHeight + RowGap)
                    - (rowCount > 0 ? RowGap : 0f);
                SetTopStretch(sections[i].Root, 0f, cursor, 0f, sectionHeight);
                sections[i].Title.text = data != null ? data.Title : string.Empty;

                for (int rowIndex = 0; rowIndex < sections[i].Rows.Length; rowIndex++)
                {
                    bool rowVisible = rowIndex < rowCount;
                    RowView row = sections[i].Rows[rowIndex];
                    row.Root.gameObject.SetActive(rowVisible);
                    if (!rowVisible)
                    {
                        continue;
                    }

                    StrategyBuildingHudRow rowData = data.GetRow(rowIndex);
                    SetTopStretch(
                        row.Root,
                        0f,
                        SectionTitleHeight + rowIndex * (RowHeight + RowGap),
                        0f,
                        RowHeight);
                    BindRow(row, rowData);
                }

                cursor += sectionHeight + SectionGap;
            }
        }

        private void BindStatus(StrategyBuildingHudSnapshot snapshot, ref float cursor)
        {
            status.Root.gameObject.SetActive(snapshot.HasStatus);
            if (!snapshot.HasStatus)
            {
                return;
            }

            SetTopStretch(status.Root, 0f, cursor, 0f, StatusHeight);
            status.Title.text = snapshot.StatusTitle;
            status.Body.text = snapshot.StatusBody;
            status.Background.color = GetSurfaceColor(snapshot.StatusTone, 0.96f);
            status.Accent.color = GetToneColor(snapshot.StatusTone);
            cursor += StatusHeight;
        }

        private static void BindRow(RowView view, StrategyBuildingHudRow data)
        {
            view.Label.text = data.Label;
            view.Value.text = data.Value;
            view.Detail.text = data.Detail;
            view.Icon.sprite = data.Icon;
            view.Icon.color = data.Icon != null ? Color.white : Color.clear;
            view.Background.color = GetSurfaceColor(data.Tone, 0.88f);
            view.Accent.color = GetToneColor(data.Tone);
            view.ProgressRoot.gameObject.SetActive(data.HasProgress);
            if (data.HasProgress)
            {
                float fill = Mathf.Clamp01(data.Progress);
                view.ProgressFill.anchorMax = new Vector2(fill, 1f);
                view.ProgressFill.offsetMax = Vector2.zero;
                view.ProgressFillImage.color = GetToneColor(data.Tone);
            }
        }

        private ChipView CreateChip(int index)
        {
            RectTransform rect = CreateUiObject("Chip_" + index, root).GetComponent<RectTransform>();
            Image background = rect.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, GetSurfaceColor(StrategyBuildingHudTone.Neutral, 0.94f));

            RectTransform accent = CreateUiObject("Accent", rect).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(3f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", rect).GetComponent<RectTransform>();
            SetTopLeft(iconRect, 8f, 7f, 20f, 20f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateText("Label", rect, 9, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 9;
            SetTopStretch(label.rectTransform, 34f, 4f, 5f, 12f);

            Text value = CreateText("Value", rect, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            value.fontStyle = FontStyle.Bold;
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 8;
            value.resizeTextMaxSize = 11;
            SetTopStretch(value.rectTransform, 34f, 16f, 5f, 15f);
            rect.gameObject.SetActive(false);
            return new ChipView(rect, background, accentImage, icon, label, value);
        }

        private SectionView CreateSection(int index)
        {
            RectTransform section = CreateUiObject("Section_" + index, root).GetComponent<RectTransform>();
            Text title = CreateText("Title", section, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 6f, 0f, 6f, SectionTitleHeight);

            RowView[] rows = new RowView[StrategyBuildingHudSection.MaxRows];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = CreateRow(i, section);
            }

            section.gameObject.SetActive(false);
            return new SectionView(section, title, rows);
        }

        private static RowView CreateRow(int index, Transform parent)
        {
            RectTransform row = CreateUiObject("Row_" + index, parent).GetComponent<RectTransform>();
            Image background = row.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, GetSurfaceColor(StrategyBuildingHudTone.Neutral, 0.88f));

            RectTransform accent = CreateUiObject("Accent", row).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(3f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", row).GetComponent<RectTransform>();
            SetTopLeft(iconRect, 10f, 10f, 28f, 28f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text label = CreateText("Label", row, 12, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 12;
            SetTopStretch(label.rectTransform, 48f, 7f, 105f, 17f);

            Text value = CreateText("Value", row, 12, TextAnchor.UpperRight, StrategyHudStyle.Primary);
            value.fontStyle = FontStyle.Bold;
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 9;
            value.resizeTextMaxSize = 12;
            SetTopStretch(value.rectTransform, 228f, 7f, 10f, 17f);

            Text detail = CreateText("Detail", row, 10, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            detail.resizeTextForBestFit = true;
            detail.resizeTextMinSize = 8;
            detail.resizeTextMaxSize = 10;
            SetTopStretch(detail.rectTransform, 48f, 25f, 10f, 14f);

            RectTransform progress = CreateUiObject("Progress", row).GetComponent<RectTransform>();
            progress.anchorMin = new Vector2(0f, 0f);
            progress.anchorMax = new Vector2(1f, 0f);
            progress.pivot = new Vector2(0.5f, 0f);
            progress.offsetMin = new Vector2(48f, 4f);
            progress.offsetMax = new Vector2(-10f, 8f);
            Image progressBackground = progress.gameObject.AddComponent<Image>();
            progressBackground.color = new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.86f);
            progressBackground.raycastTarget = false;

            RectTransform fill = CreateUiObject("Fill", progress).GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.raycastTarget = false;
            progress.gameObject.SetActive(false);
            row.gameObject.SetActive(false);
            return new RowView(row, background, accentImage, icon, label, value, detail, progress, fill, fillImage);
        }

        private StatusView CreateStatus()
        {
            RectTransform rect = CreateUiObject("Status", root).GetComponent<RectTransform>();
            Image background = rect.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, GetSurfaceColor(StrategyBuildingHudTone.Neutral, 0.96f));

            RectTransform accent = CreateUiObject("Accent", rect).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(4f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.raycastTarget = false;

            Text title = CreateText("Title", rect, 12, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 14f, 10f, 14f, 18f);

            Text body = CreateText("Body", rect, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 9;
            body.resizeTextMaxSize = 11;
            body.lineSpacing = 1.05f;
            SetTopStretch(body.rectTransform, 14f, 31f, 14f, 28f);
            rect.gameObject.SetActive(false);
            return new StatusView(rect, background, accentImage, title, body);
        }

        private static Color GetToneColor(StrategyBuildingHudTone tone) => tone switch
        {
            StrategyBuildingHudTone.Positive => StrategyHudStyle.Success,
            StrategyBuildingHudTone.Warning => StrategyHudStyle.Warning,
            StrategyBuildingHudTone.Critical => StrategyHudStyle.Danger,
            StrategyBuildingHudTone.Info => StrategyHudStyle.Secondary,
            _ => StrategyHudStyle.Primary
        };

        private static Color GetSurfaceColor(StrategyBuildingHudTone tone, float alpha)
        {
            Color mixed = Color.Lerp(StrategyHudStyle.Surface, GetToneColor(tone), tone == StrategyBuildingHudTone.Neutral ? 0.04f : 0.16f);
            mixed.a = alpha;
            return mixed;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            Text text = CreateUiObject(name, parent).AddComponent<Text>();
            StrategyHudStyle.StyleText(text, StrategyHudTextRole.Body, color);
            text.fontSize = size;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
        }

        private static void SetTopStretch(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private sealed class ChipView
        {
            public ChipView(RectTransform root, Image background, Image accent, Image icon, Text label, Text value)
            {
                Root = root;
                Background = background;
                Accent = accent;
                Icon = icon;
                Label = label;
                Value = value;
            }

            public RectTransform Root { get; }
            public Image Background { get; }
            public Image Accent { get; }
            public Image Icon { get; }
            public Text Label { get; }
            public Text Value { get; }
        }

        private sealed class SectionView
        {
            public SectionView(RectTransform root, Text title, RowView[] rows)
            {
                Root = root;
                Title = title;
                Rows = rows;
            }

            public RectTransform Root { get; }
            public Text Title { get; }
            public RowView[] Rows { get; }
        }

        private sealed class RowView
        {
            public RowView(RectTransform root, Image background, Image accent, Image icon, Text label, Text value, Text detail, RectTransform progressRoot, RectTransform progressFill, Image progressFillImage)
            {
                Root = root;
                Background = background;
                Accent = accent;
                Icon = icon;
                Label = label;
                Value = value;
                Detail = detail;
                ProgressRoot = progressRoot;
                ProgressFill = progressFill;
                ProgressFillImage = progressFillImage;
            }

            public RectTransform Root { get; }
            public Image Background { get; }
            public Image Accent { get; }
            public Image Icon { get; }
            public Text Label { get; }
            public Text Value { get; }
            public Text Detail { get; }
            public RectTransform ProgressRoot { get; }
            public RectTransform ProgressFill { get; }
            public Image ProgressFillImage { get; }
        }

        private sealed class StatusView
        {
            public StatusView(RectTransform root, Image background, Image accent, Text title, Text body)
            {
                Root = root;
                Background = background;
                Accent = accent;
                Title = title;
                Body = body;
            }

            public RectTransform Root { get; }
            public Image Background { get; }
            public Image Accent { get; }
            public Text Title { get; }
            public Text Body { get; }
        }
    }
}
