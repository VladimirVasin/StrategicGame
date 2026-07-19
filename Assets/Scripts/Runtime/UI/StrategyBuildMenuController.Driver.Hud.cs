using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    internal sealed partial class StrategyBuildMenuControllerDriver
    {
        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("StrategyBuildMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(ownerTransform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            CreateSpeedControlsHud(canvasObject.transform);
            CreateBuildButton(canvasObject.transform);
            CreateMenuLayer(canvasObject.transform);
            CreatePlacementFeedbackHud(canvasObject.transform);
        }

        private void CreateSpeedControlsHud(Transform parent)
        {
            RectTransform panel = CreateUiObject("SpeedControlsPanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(300f, -5f);
            panel.sizeDelta = new Vector2(150f, 60f);

            Image background = panel.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleRailModule(background);

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.30f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);

            for (int i = 0; i < speedButtons.Length; i++)
            {
                int index = i;
                RectTransform buttonRoot = CreateUiObject("SpeedX" + (i + 1), panel).GetComponent<RectTransform>();
                buttonRoot.anchorMin = new Vector2(0f, 0.5f);
                buttonRoot.anchorMax = new Vector2(0f, 0.5f);
                buttonRoot.pivot = new Vector2(0f, 0.5f);
                buttonRoot.anchoredPosition = new Vector2(7f + i * 49f, 0f);
                buttonRoot.sizeDelta = new Vector2(45f, 42f);

                Image image = buttonRoot.gameObject.AddComponent<Image>();
                StrategyHudStyle.StyleInset(image, StrategyHudStyle.Elevated, true);

                Button button = buttonRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SetSpeedFromHud(index + 1));
                ConfigureButtonColors(button);
                StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.SoundOnly);
                StrategyHudTooltip.Attach(buttonRoot.gameObject, "Set simulation speed to x" + (i + 1) + ". Keyboard shortcut: F" + (i + 1) + ".");

                Text label = CreateText("Label", buttonRoot, "x" + (i + 1), 13, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
                Stretch(label.rectTransform, 0f, 0f, 0f, 1f);

                speedButtons[i] = button;
                speedButtonImages[i] = image;
                speedButtonTexts[i] = label;
            }
        }

        private void CreatePlacementFeedbackHud(Transform parent)
        {
            placementFeedbackRoot = CreateUiObject("ActiveBuildToolPanel", parent).GetComponent<RectTransform>();
            placementFeedbackRoot.anchorMin = new Vector2(0.5f, 0f);
            placementFeedbackRoot.anchorMax = new Vector2(0.5f, 0f);
            placementFeedbackRoot.pivot = new Vector2(0.5f, 0f);
            placementFeedbackRoot.anchoredPosition = new Vector2(0f, 12f);
            placementFeedbackRoot.sizeDelta = new Vector2(520f, 48f);

            Image background = placementFeedbackRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(background, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.98f));
            StrategyHudStyle.AddShadow(placementFeedbackRoot.gameObject, 0.68f);

            RectTransform accent = CreateUiObject("PlacementStateAccent", placementFeedbackRoot).GetComponent<RectTransform>();
            accent.anchorMin = Vector2.zero;
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(4f, 0f);
            accent.anchoredPosition = Vector2.zero;
            placementFeedbackAccent = accent.gameObject.AddComponent<Image>();
            placementFeedbackAccent.raycastTarget = false;

            placementFeedbackTitle = CreateText(
                "ToolTitle",
                placementFeedbackRoot,
                string.Empty,
                13,
                TextAnchor.UpperLeft,
                StrategyHudStyle.TextPrimary);
            placementFeedbackTitle.fontStyle = FontStyle.Bold;
            placementFeedbackTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            placementFeedbackTitle.rectTransform.anchorMax = new Vector2(0.68f, 1f);
            placementFeedbackTitle.rectTransform.offsetMin = new Vector2(14f, -23f);
            placementFeedbackTitle.rectTransform.offsetMax = new Vector2(-6f, -4f);
            placementFeedbackTitle.resizeTextForBestFit = true;
            placementFeedbackTitle.resizeTextMinSize = 10;
            placementFeedbackTitle.resizeTextMaxSize = 13;

            placementFeedbackCost = CreateText(
                "ToolCost",
                placementFeedbackRoot,
                string.Empty,
                11,
                TextAnchor.UpperRight,
                StrategyHudStyle.Primary);
            placementFeedbackCost.fontStyle = FontStyle.Bold;
            placementFeedbackCost.rectTransform.anchorMin = new Vector2(0.68f, 1f);
            placementFeedbackCost.rectTransform.anchorMax = new Vector2(1f, 1f);
            placementFeedbackCost.rectTransform.offsetMin = new Vector2(0f, -23f);
            placementFeedbackCost.rectTransform.offsetMax = new Vector2(-12f, -4f);
            placementFeedbackCost.resizeTextForBestFit = true;
            placementFeedbackCost.resizeTextMinSize = 9;
            placementFeedbackCost.resizeTextMaxSize = 11;

            placementFeedbackStatus = CreateText(
                "PlacementStatus",
                placementFeedbackRoot,
                string.Empty,
                11,
                TextAnchor.LowerLeft,
                StrategyHudStyle.TextMuted);
            placementFeedbackStatus.rectTransform.anchorMin = Vector2.zero;
            placementFeedbackStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
            placementFeedbackStatus.rectTransform.offsetMin = new Vector2(14f, 4f);
            placementFeedbackStatus.rectTransform.offsetMax = new Vector2(-12f, 22f);
            placementFeedbackStatus.resizeTextForBestFit = true;
            placementFeedbackStatus.resizeTextMinSize = 9;
            placementFeedbackStatus.resizeTextMaxSize = 11;
            placementFeedbackRoot.gameObject.SetActive(false);
        }

        private void RefreshPlacementFeedback()
        {
            if (placementFeedbackRoot == null)
            {
                return;
            }

            StrategyBuildToolInfo info = default;
            bool visible = !isOpen
                && ActiveTool != StrategyBuildTool.None
                && TryGetActiveToolInfo(out info);
            placementFeedbackRoot.gameObject.SetActive(visible);
            if (buildButtonRoot != null)
            {
                buildButtonRoot.gameObject.SetActive(!visible);
            }

            if (!visible)
            {
                return;
            }

            placementFeedbackTitle.text = info.Title + " · " + info.Footprint.x + "×" + info.Footprint.y;
            placementFeedbackCost.text = info.Cost.ToDisplayText();
            placementFeedbackStatus.text = placementFeedbackMessage + " · LMB place · RMB/Esc cancel";
            placementFeedbackStatus.color = placementFeedbackValid
                ? StrategyHudStyle.Success
                : StrategyHudStyle.Danger;
            placementFeedbackAccent.color = placementFeedbackStatus.color;
        }

        private void RefreshBuildButtonTooltip()
        {
            if (buildButtonTooltip == null)
            {
                return;
            }

            buildButtonTooltip.enabled = !isOpen;
        }

        private void SetSpeedFromHud(int speed)
        {
            if (timeScale == null)
            {
                timeScale = UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
            }

            timeScale?.SetRequestedScale(speed);
            RefreshSpeedControls();
            StrategyHudSfxAudio.Play(StrategyHudSfxKind.Step);
            StrategyDebugLogger.Info(
                "BuildMenu",
                "SpeedButtonClicked",
                StrategyDebugLogger.F("speed", speed),
                StrategyDebugLogger.F("timeScaleFound", timeScale != null));
        }

        private void RefreshSpeedControls()
        {
            if (timeScale == null)
            {
                timeScale = UnityEngine.Object.FindAnyObjectByType<StrategyTimeScaleController>();
            }

            float currentScale = timeScale != null ? timeScale.CurrentScale : 1f;
            for (int i = 0; i < speedButtons.Length; i++)
            {
                bool active = Mathf.RoundToInt(currentScale) == i + 1;
                if (speedButtonImages[i] != null)
                {
                    speedButtonImages[i].color = active
                        ? new Color(0.25f, 0.36f, 0.36f, 0.98f)
                        : new Color(0.11f, 0.16f, 0.17f, 0.96f);
                }

                if (speedButtonTexts[i] != null)
                {
                    speedButtonTexts[i].color = active
                        ? new Color(0.95f, 0.88f, 0.62f)
                        : Color.white;
                }
            }
        }
    }
}
