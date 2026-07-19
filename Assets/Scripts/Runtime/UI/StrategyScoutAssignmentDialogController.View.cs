using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyScoutAssignmentDialogController
    {
        private void BuildUi()
        {
            GameObject canvasObject = new GameObject(
                "ScoutAssignmentDialogCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 290;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            RectTransform root = CreateUiObject("Root", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(root, 0f, 0f, 0f, 0f);
            Image shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0.008f, 0.012f, 0.012f, 0.54f);
            rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            board = CreateUiObject("ExpeditionBoardPanel", root).GetComponent<RectTransform>();
            board.anchorMin = new Vector2(1f, 0.5f);
            board.anchorMax = new Vector2(1f, 0.5f);
            board.pivot = new Vector2(1f, 0.5f);
            board.anchoredPosition = new Vector2(-54f, 0f);
            board.sizeDelta = new Vector2(720f, 800f);

            Image background = board.gameObject.AddComponent<Image>();
            background.sprite = StrategyUiThemeProvider.GetPanelSprite();
            background.type = Image.Type.Sliced;
            background.color = new Color(0.055f, 0.078f, 0.075f, 0.995f);
            Outline outline = board.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.59f, 0.25f, 0.82f);
            outline.effectDistance = new Vector2(2f, -2f);

            BuildBoardDecoration();
            BuildHeader();
            BuildStoryCard();
            BuildCandidateList();
            BuildExpeditionDurationControls();
            BuildFooter();

            panelTransition = root.gameObject.AddComponent<StrategyUiPanelTransition>();
            panelTransition.Configure(rootGroup, board, new Vector2(42f, 0f), 0.975f, 0.24f, 0.16f);
            panelTransition.SetVisible(false, true);
        }

        private void BuildBoardDecoration()
        {
            RectTransform rail = CreateUiObject("RouteRail", board).GetComponent<RectTransform>();
            SetTopLeft(rail, 18f, 22f, 3f, 756f);
            Image railImage = rail.gameObject.AddComponent<Image>();
            railImage.color = new Color(0.77f, 0.55f, 0.24f, 0.62f);
            railImage.raycastTarget = false;

            for (int i = 0; i < 7; i++)
            {
                RectTransform marker = CreateUiObject("RouteMarker_" + i, board).GetComponent<RectTransform>();
                SetTopLeft(marker, 13f, 32f + i * 119f, 13f, 13f);
                Image markerImage = marker.gameObject.AddComponent<Image>();
                markerImage.color = i == 0
                    ? new Color(1f, 0.78f, 0.36f, 1f)
                    : new Color(0.60f, 0.47f, 0.27f, 0.86f);
                markerImage.raycastTarget = false;
            }

            RectTransform topAccent = CreateUiObject("TopAccent", board).GetComponent<RectTransform>();
            SetTopStretch(topAccent, 34f, 0f, 0f, 5f);
            Image accentImage = topAccent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.91f, 0.65f, 0.27f, 1f);
            accentImage.raycastTarget = false;
        }

        private void BuildHeader()
        {
            RectTransform compass = CreateUiObject("CompassBadge", board).GetComponent<RectTransform>();
            SetTopLeft(compass, 46f, 25f, 72f, 72f);
            Image compassBackground = compass.gameObject.AddComponent<Image>();
            compassBackground.sprite = StrategyUiThemeProvider.GetButtonSprite();
            compassBackground.type = Image.Type.Sliced;
            compassBackground.color = new Color(0.18f, 0.16f, 0.11f, 1f);
            Text compassText = CreateText("Compass", compass, "N\n+\nS", 13, TextAnchor.MiddleCenter, new Color(0.96f, 0.74f, 0.34f, 1f));
            compassText.fontStyle = FontStyle.Bold;
            Stretch(compassText.rectTransform, 4f, 5f, 4f, 5f);

            Text eyebrow = CreateText(
                "Eyebrow",
                board,
                "SCOUT LODGE  /  EXPEDITION BOARD",
                12,
                TextAnchor.MiddleLeft,
                new Color(0.86f, 0.66f, 0.31f, 1f));
            eyebrow.fontStyle = FontStyle.Bold;
            SetTopLeft(eyebrow.rectTransform, 136f, 23f, 544f, 22f);

            titleText = CreateText("Title", board, "THE FIRST EXPEDITION", 28, TextAnchor.MiddleLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 21;
            titleText.resizeTextMaxSize = 28;
            SetTopLeft(titleText.rectTransform, 136f, 43f, 544f, 38f);

            subtitleText = CreateText(
                "Subtitle",
                board,
                "Beyond the Firelight",
                16,
                TextAnchor.MiddleLeft,
                new Color(0.80f, 0.87f, 0.81f, 1f));
            subtitleText.fontStyle = FontStyle.Italic;
            SetTopLeft(subtitleText.rectTransform, 136f, 80f, 544f, 25f);

            CreateDivider(118f);
        }

        private void BuildStoryCard()
        {
            RectTransform card = CreateUiObject("BriefingCard", board).GetComponent<RectTransform>();
            SetTopStretch(card, 44f, 137f, 28f, 178f);
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = StrategyUiThemeProvider.GetPanelSprite();
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.095f, 0.12f, 0.105f, 0.96f);

            Text label = CreateText(
                "Label",
                card,
                "WHY THE TRAIL MATTERS",
                11,
                TextAnchor.UpperLeft,
                new Color(0.94f, 0.70f, 0.31f, 1f));
            label.fontStyle = FontStyle.Bold;
            SetTopStretch(label.rectTransform, 18f, 13f, 18f, 18f);

            storyText = CreateText(
                "Story",
                card,
                "The roofs are standing and the camps are working, but beyond the last familiar path the valley is still only rumor. Choose one adult to carry our first map into the unknown.",
                13,
                TextAnchor.UpperLeft,
                new Color(0.88f, 0.92f, 0.86f, 1f));
            storyText.resizeTextForBestFit = true;
            storyText.resizeTextMinSize = 11;
            storyText.resizeTextMaxSize = 13;
            SetTopStretch(storyText.rectTransform, 18f, 37f, 18f, 53f);

            Text mechanics = CreateText(
                "Mechanics",
                card,
                "Scouts range by day and night, reveal unexplored land, investigate landmarks, and report distant Iron and Coal deposits - and whatever else waits beyond the fog.",
                12,
                TextAnchor.UpperLeft,
                new Color(0.75f, 0.84f, 0.78f, 1f));
            mechanics.resizeTextForBestFit = true;
            mechanics.resizeTextMinSize = 10;
            mechanics.resizeTextMaxSize = 12;
            SetTopStretch(mechanics.rectTransform, 18f, 96f, 18f, 43f);

            Text capacity = CreateText(
                "Capacity",
                card,
                "ONE LODGE  /  ONE SCOUT  /  REASSIGN ANY TIME",
                10,
                TextAnchor.MiddleLeft,
                new Color(0.89f, 0.69f, 0.36f, 1f));
            capacity.fontStyle = FontStyle.Bold;
            SetTopStretch(capacity.rectTransform, 18f, 147f, 18f, 18f);
        }

        private void BuildCandidateList()
        {
            candidateHeadingText = CreateText(
                "CandidateHeading",
                board,
                "CHOOSE A RESIDENT",
                12,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 0.72f, 0.37f, 1f));
            candidateHeadingText.fontStyle = FontStyle.Bold;
            SetTopStretch(candidateHeadingText.rectTransform, 46f, 329f, 28f, 24f);

            RectTransform viewport = CreateUiObject("CandidateViewport", board).GetComponent<RectTransform>();
            SetTopStretch(viewport, 44f, 359f, 43f, 210f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.025f, 0.038f, 0.036f, 0.92f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            contentRoot = CreateUiObject("CandidateContent", viewport).GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(-12f, 1f);

            RectTransform scrollbarRoot = CreateUiObject("Scrollbar", board).GetComponent<RectTransform>();
            SetTopRight(scrollbarRoot, 30f, 359f, 9f, 210f);
            Image track = scrollbarRoot.gameObject.AddComponent<Image>();
            track.color = new Color(0f, 0f, 0f, 0.36f);
            RectTransform handle = CreateUiObject("Handle", scrollbarRoot).GetComponent<RectTransform>();
            Stretch(handle, 1f, 1f, 1f, 1f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(0.82f, 0.61f, 0.30f, 0.90f);
            Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRoot;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 28f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            emptyText = CreateText(
                "Empty",
                board,
                "No adult residents are in the settlement yet.",
                14,
                TextAnchor.MiddleCenter,
                new Color(0.70f, 0.76f, 0.72f, 1f));
            SetTopStretch(emptyText.rectTransform, 70f, 420f, 55f, 60f);
        }

        private void BuildFooter()
        {
            actionStatusText = CreateText(
                "ActionStatus",
                board,
                string.Empty,
                12,
                TextAnchor.MiddleLeft,
                new Color(0.84f, 0.75f, 0.52f, 1f));
            actionStatusText.resizeTextForBestFit = true;
            actionStatusText.resizeTextMinSize = 10;
            actionStatusText.resizeTextMaxSize = 12;
            SetTopStretch(actionStatusText.rectTransform, 46f, 675f, 28f, 29f);

            confirmButton = CreateActionButton(
                "ConfirmButton",
                board,
                new Color(0.38f, 0.27f, 0.12f, 1f),
                ConfirmSelection,
                out confirmLabel);
            SetBottomStretch(confirmButton.GetComponent<RectTransform>(), 46f, 30f, 218f, 50f);
            StrategyUiButtonFeedback.Attach(confirmButton, StrategyUiButtonFeedbackProfile.Cinematic, null);

            deferButton = CreateActionButton(
                "DeferButton",
                board,
                new Color(0.12f, 0.16f, 0.15f, 1f),
                DeferSelection,
                out deferLabel);
            SetBottomRight(deferButton.GetComponent<RectTransform>(), 28f, 30f, 174f, 50f);
            StrategyUiButtonFeedback.Attach(deferButton, StrategyUiButtonFeedbackProfile.Standard, null);
        }

        private StrategyScoutAssignmentRowView CreateCandidateRow(int index)
        {
            RectTransform root = CreateUiObject("Candidate_" + index, contentRoot).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(0f, StrategyScoutAssignmentRowView.RowHeight - 4f);
            root.anchoredPosition = new Vector2(0f, -index * StrategyScoutAssignmentRowView.RowHeight);

            Image background = root.gameObject.AddComponent<Image>();
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.Compact, null);

            RectTransform selectionMark = CreateUiObject("SelectionMark", root).GetComponent<RectTransform>();
            SetTopLeft(selectionMark, 0f, 0f, 5f, StrategyScoutAssignmentRowView.RowHeight - 4f);
            Image selectionImage = selectionMark.gameObject.AddComponent<Image>();
            selectionImage.color = new Color(0.96f, 0.70f, 0.28f, 1f);
            selectionImage.raycastTarget = false;

            RectTransform portraitFrame = CreateUiObject("PortraitFrame", root).GetComponent<RectTransform>();
            SetTopLeft(portraitFrame, 12f, 8f, 54f, 54f);
            Image frameImage = portraitFrame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0f, 0f, 0f, 0.32f);
            frameImage.raycastTarget = false;
            RectTransform portraitRoot = CreateUiObject("Portrait", portraitFrame).GetComponent<RectTransform>();
            Stretch(portraitRoot, 3f, 3f, 3f, 3f);
            Image portrait = portraitRoot.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            Text name = CreateText("Name", root, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 11;
            name.resizeTextMaxSize = 14;
            SetTopLeft(name.rectTransform, 78f, 9f, 245f, 24f);

            Text detail = CreateText(
                "Detail",
                root,
                string.Empty,
                11,
                TextAnchor.UpperLeft,
                new Color(0.70f, 0.78f, 0.74f, 1f));
            SetTopLeft(detail.rectTransform, 78f, 35f, 245f, 20f);

            Text status = CreateText(
                "Status",
                root,
                string.Empty,
                11,
                TextAnchor.MiddleRight,
                new Color(0.83f, 0.68f, 0.38f, 1f));
            status.resizeTextForBestFit = true;
            status.resizeTextMinSize = 9;
            status.resizeTextMaxSize = 11;
            SetTopRight(status.rectTransform, 14f, 12f, 255f, 42f);

            return new StrategyScoutAssignmentRowView(
                root,
                background,
                button,
                portrait,
                name,
                detail,
                status,
                selectionMark.gameObject);
        }

        private static Button CreateActionButton(
            string name,
            Transform parent,
            Color color,
            UnityEngine.Events.UnityAction action,
            out Text label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = StrategyUiThemeProvider.GetButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.42f);
            button.colors = colors;
            label = CreateText("Label", root, string.Empty, 14, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 14;
            Stretch(label.rectTransform, 10f, 4f, 10f, 4f);
            return button;
        }

        private void CreateDivider(float top)
        {
            RectTransform line = CreateUiObject("Divider", board).GetComponent<RectTransform>();
            SetTopStretch(line, 44f, top, 28f, 2f);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.70f, 0.34f, 0.34f);
            image.raycastTarget = false;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            TextAnchor anchor,
            Color color)
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

        private static void SetTopStretch(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBottomStretch(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
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

        private static void SetBottomRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
