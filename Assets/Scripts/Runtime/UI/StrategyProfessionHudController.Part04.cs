using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyProfessionHudController
    {
        private static readonly StrategyAutoWorkforceCategory[] AutoPriorityOrder =
        {
            StrategyAutoWorkforceCategory.Construction,
            StrategyAutoWorkforceCategory.Food,
            StrategyAutoWorkforceCategory.Logistics,
            StrategyAutoWorkforceCategory.Wood,
            StrategyAutoWorkforceCategory.Stone,
            StrategyAutoWorkforceCategory.Planks,
            StrategyAutoWorkforceCategory.Iron,
            StrategyAutoWorkforceCategory.Coal
        };

        private readonly AutoPriorityRow[] autoPriorityRows = new AutoPriorityRow[AutoPriorityOrder.Length];
        private StrategyAutoWorkforceController autoWorkforce;
        private Text autoToggleText;
        private Text autoStatusText;
        private Button autoToggleButton;

        private void SetAutoWorkforce(StrategyAutoWorkforceController controller)
        {
            autoWorkforce = controller != null
                ? controller
                : autoWorkforce ?? UnityEngine.Object.FindAnyObjectByType<StrategyAutoWorkforceController>();
        }

        private void CreateAutoControls(Transform parent)
        {
            RectTransform root = CreateUiObject("AutoWorkforce", parent).GetComponent<RectTransform>();
            SetTopStretch(root, 24f, 96f, 24f, 82f);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.09f, 0.13f, 0.13f, 0.95f);

            RectTransform toggleRoot = CreateUiObject("Toggle", root).GetComponent<RectTransform>();
            SetTopLeft(toggleRoot, 10f, 8f, 156f, 30f);
            Image toggleImage = toggleRoot.gameObject.AddComponent<Image>();
            toggleImage.color = new Color(0.16f, 0.23f, 0.20f, 0.95f);
            autoToggleButton = toggleRoot.gameObject.AddComponent<Button>();
            autoToggleButton.targetGraphic = toggleImage;
            autoToggleButton.onClick.AddListener(ToggleAutoAssign);
            ConfigureButtonColors(autoToggleButton, toggleImage.color);
            autoToggleText = CreateText("Label", toggleRoot, "Auto Assign", 12, TextAnchor.MiddleCenter, Color.white);
            autoToggleText.fontStyle = FontStyle.Bold;
            SetOffsets(autoToggleText.rectTransform, 4f, 0f, 4f, 1f);

            autoStatusText = CreateText("Status", root, string.Empty, 11, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.80f));
            SetTopStretch(autoStatusText.rectTransform, 176f, 8f, 12f, 30f);

            float columnWidth = 137f;
            for (int i = 0; i < AutoPriorityOrder.Length; i++)
            {
                int column = i % 4;
                int row = i / 4;
                RectTransform item = CreateUiObject(AutoPriorityOrder[i] + "Priority", root).GetComponent<RectTransform>();
                item.anchorMin = new Vector2(0f, 1f);
                item.anchorMax = new Vector2(0f, 1f);
                item.pivot = new Vector2(0f, 1f);
                item.anchoredPosition = new Vector2(10f + column * columnWidth, -42f - row * 18f);
                item.sizeDelta = new Vector2(130f, 16f);
                autoPriorityRows[i] = CreatePriorityRow(item, AutoPriorityOrder[i]);
            }
        }

        private AutoPriorityRow CreatePriorityRow(RectTransform root, StrategyAutoWorkforceCategory category)
        {
            Text label = CreateText("Label", root, StrategyAutoWorkforceSettings.GetLabel(category), 10, TextAnchor.MiddleLeft, Color.white);
            SetOffsets(label.rectTransform, 0f, 0f, 60f, 0f);

            Button minus = CreateMiniPriorityButton("Minus", root, 48f, "-");
            Text value = CreateText("Value", root, "0", 10, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            value.fontStyle = FontStyle.Bold;
            SetRightMiddle(value.rectTransform, 24f, 0f, 20f, 16f);
            Button plus = CreateMiniPriorityButton("Plus", root, 0f, "+");

            StrategyAutoWorkforceCategory captured = category;
            minus.onClick.AddListener(() => ChangeAutoPriority(captured, -1));
            plus.onClick.AddListener(() => ChangeAutoPriority(captured, 1));

            return new AutoPriorityRow
            {
                Category = category,
                Label = label,
                Value = value,
                Minus = minus,
                Plus = plus
            };
        }

        private Button CreateMiniPriorityButton(string name, Transform parent, float right, string label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(1f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-right, 0f);
            root.sizeDelta = new Vector2(18f, 16f);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.17f, 0.17f, 0.96f);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button, image.color);
            Text text = CreateText("Label", root, label, 12, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetOffsets(text.rectTransform, 0f, 0f, 0f, 1f);
            return button;
        }

        private void ToggleAutoAssign()
        {
            SetAutoWorkforce(null);
            if (autoWorkforce == null)
            {
                return;
            }

            autoWorkforce.SetAutoAssignEnabled(!autoWorkforce.IsAutoAssignEnabled);
            actionStatusText.text = autoWorkforce.LastStatus;
            isDirty = true;
            RefreshAutoControls(CountFreeWorkers());
        }

        private void ChangeAutoPriority(StrategyAutoWorkforceCategory category, int delta)
        {
            SetAutoWorkforce(null);
            if (autoWorkforce == null)
            {
                return;
            }

            int value = autoWorkforce.AdjustPriority(category, delta);
            actionStatusText.text = StrategyAutoWorkforceSettings.GetLabel(category) + ": " + value;
            isDirty = true;
            RefreshAutoControls(CountFreeWorkers());
        }

        private void RegisterManualProfessionChange(StrategyProfessionType type, bool assign, bool success)
        {
            if (!success || assign)
            {
                return;
            }

            SetAutoWorkforce(null);
            autoWorkforce?.RegisterManualOverride(type);
        }

        private void RefreshAutoControls(int freeWorkers)
        {
            SetAutoWorkforce(null);
            bool available = autoWorkforce != null;
            if (autoToggleButton != null)
            {
                autoToggleButton.interactable = available;
            }

            if (autoToggleText != null)
            {
                autoToggleText.text = available && autoWorkforce.IsAutoAssignEnabled
                    ? "Auto Assign: On"
                    : "Auto Assign: Off";
            }

            if (autoStatusText != null)
            {
                autoStatusText.text = available
                    ? autoWorkforce.LastStatus + " / free adults " + freeWorkers
                    : "Auto workforce unavailable";
            }

            for (int i = 0; i < autoPriorityRows.Length; i++)
            {
                AutoPriorityRow row = autoPriorityRows[i];
                if (row == null)
                {
                    continue;
                }

                int priority = available ? autoWorkforce.Settings.GetPriority(row.Category) : 0;
                row.Value.text = priority.ToString();
                row.Minus.interactable = available && priority > StrategyAutoWorkforceSettings.MinPriority;
                row.Plus.interactable = available && priority < StrategyAutoWorkforceSettings.MaxPriority;
                row.Label.color = priority > 0 ? Color.white : new Color(0.55f, 0.60f, 0.58f);
            }
        }

        private sealed class AutoPriorityRow
        {
            public StrategyAutoWorkforceCategory Category;
            public Text Label;
            public Text Value;
            public Button Minus;
            public Button Plus;
        }
    }
}
