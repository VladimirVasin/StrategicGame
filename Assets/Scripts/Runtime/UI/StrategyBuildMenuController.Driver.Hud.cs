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

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateTreasuryHud(canvasObject.transform);
            CreateSpeedControlsHud(canvasObject.transform);
            CreateBuildButton(canvasObject.transform);
            CreateMenuLayer(canvasObject.transform);
        }

        private void CreateTreasuryHud(Transform parent)
        {
            RectTransform panel = CreateUiObject("TreasuryPanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(18f, -18f);
            panel.sizeDelta = new Vector2(178f, 42f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.13f, 0.92f);

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.38f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);

            treasuryText = CreateText("TreasuryText", panel, "Logs 0  Stone 0", 16, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            treasuryText.fontStyle = FontStyle.Bold;
            Stretch(treasuryText.rectTransform, 8f, 0f, 8f, 0f);
        }

        private void CreateSpeedControlsHud(Transform parent)
        {
            RectTransform panel = CreateUiObject("SpeedControlsPanel", parent).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(18f, -66f);
            panel.sizeDelta = new Vector2(178f, 34f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.09f, 0.10f, 0.88f);

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
                buttonRoot.anchoredPosition = new Vector2(6f + i * 56f, 0f);
                buttonRoot.sizeDelta = new Vector2(52f, 24f);

                Image image = buttonRoot.gameObject.AddComponent<Image>();
                image.color = new Color(0.11f, 0.16f, 0.17f, 0.96f);

                Button button = buttonRoot.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => SetSpeedFromHud(index + 1));
                ConfigureButtonColors(button);

                Text label = CreateText("Label", buttonRoot, "x" + (i + 1), 13, TextAnchor.MiddleCenter, Color.white);
                label.fontStyle = FontStyle.Bold;
                Stretch(label.rectTransform, 0f, 0f, 0f, 1f);

                speedButtons[i] = button;
                speedButtonImages[i] = image;
                speedButtonTexts[i] = label;
            }
        }
    }
}
