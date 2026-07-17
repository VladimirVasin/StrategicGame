using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPointOfInterestDialogController
    {
        private GameObject yesButtonRoot;
        private GameObject noButtonRoot;
        private Text yesButtonLabel;
        private Text noButtonLabel;
        private Action yesCallback;
        private Action noCallback;

        public void ShowChoice(
            string title,
            string body,
            string yesLabel,
            string noLabel,
            Action onYes,
            Action onNo)
        {
            Configure();
            acknowledgementLocked = false;
            acknowledgedCallback = null;
            yesCallback = onYes;
            noCallback = onNo;
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Point of Interest" : title;
            bodyText.text = body ?? string.Empty;
            yesButtonLabel.text = string.IsNullOrWhiteSpace(yesLabel) ? "Да" : yesLabel;
            noButtonLabel.text = string.IsNullOrWhiteSpace(noLabel) ? "Нет" : noLabel;
            SetChoiceMode(true);
            panelTransition.SetVisible(true);
            RefreshInputContext(true);
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Notify);
            }

            StrategyDebugLogger.Info(
                "PointsOfInterest",
                "ChoiceDialogOpened",
                StrategyDebugLogger.F("title", titleText.text));
        }

        private void CreateChoiceButtons(RectTransform parent)
        {
            yesButtonRoot = CreateChoiceButton(
                "YesButton",
                parent,
                -106f,
                new Color(0.22f, 0.39f, 0.30f, 0.98f),
                HandleYes,
                out yesButtonLabel);
            noButtonRoot = CreateChoiceButton(
                "NoButton",
                parent,
                106f,
                new Color(0.34f, 0.22f, 0.20f, 0.98f),
                HandleNo,
                out noButtonLabel);
            SetChoiceMode(false);
        }

        private static GameObject CreateChoiceButton(
            string name,
            RectTransform parent,
            float x,
            Color color,
            UnityEngine.Events.UnityAction action,
            out Text label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(x, 26f);
            root.sizeDelta = new Vector2(188f, 46f);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
            button.colors = colors;
            StrategyUiButtonFeedback.Attach(button, StrategyUiButtonFeedbackProfile.Standard, null);

            label = CreateText("Label", root, string.Empty, 16, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform, 0f, 0f, 0f, 1f);
            return root.gameObject;
        }

        private void HandleYes()
        {
            CompleteChoice(true);
        }

        private void HandleNo()
        {
            CompleteChoice(false);
        }

        private void CompleteChoice(bool accepted)
        {
            if (acknowledgementLocked)
            {
                return;
            }

            acknowledgementLocked = true;
            Action callback = accepted ? yesCallback : noCallback;
            ClearChoiceCallbacks();
            panelTransition.SetVisible(false, true);
            RefreshInputContext(false);
            if (Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(
                    accepted ? StrategyHudSfxKind.Confirm : StrategyHudSfxKind.Cancel);
            }

            StrategyDebugLogger.Info(
                "PointsOfInterest",
                "ChoiceDialogResolved",
                StrategyDebugLogger.F("accepted", accepted));
            callback?.Invoke();
        }

        private void SetChoiceMode(bool choiceMode)
        {
            okButtonRoot?.SetActive(!choiceMode);
            yesButtonRoot?.SetActive(choiceMode);
            noButtonRoot?.SetActive(choiceMode);
        }

        private void ClearChoiceCallbacks()
        {
            yesCallback = null;
            noCallback = null;
        }
    }
}
