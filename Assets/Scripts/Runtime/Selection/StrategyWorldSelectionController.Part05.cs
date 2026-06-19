using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {

        private void RefreshHouseFoodRows(StrategyPlacedBuilding building)
        {
            StrategyHouseholdFoodState food = building != null
                ? building.GetComponent<StrategyHouseholdFoodState>()
                : null;
            int homeFood = building != null && building.Resources != null
                ? building.Resources.GetTotalFoodAmount()
                : 0;
            float homeFoodRations = building != null && building.Resources != null
                ? building.Resources.GetTotalRationValue()
                : 0f;
            int granaryFood = StrategyGranary.GetTotalSettlementFood();
            float granaryFoodRations = StrategyGranary.GetTotalSettlementFoodRations();
            if (food == null)
            {
                ApplyFoodStatus(
                    "Food status",
                    "No household data",
                    "No meal",
                    FormatFoodStockLine(homeFood, homeFoodRations, granaryFood, granaryFoodRations),
                    0f,
                    new Color(0.30f, 0.34f, 0.34f, 0.94f),
                    new Color(0.52f, 0.58f, 0.56f, 0.95f));
                return;
            }

            float rationFill = food.LastRequiredRations <= 0f
                ? 1f
                : Mathf.Clamp01(food.LastSuppliedRations / food.LastRequiredRations);
            string rationText = food.LastRequiredRations <= 0f
                ? "No meal yet"
                : FormatRations(food.LastSuppliedRations)
                    + "/"
                    + FormatRations(food.LastRequiredRations);

            string statusText;
            string detailText;
            Color rowColor;
            Color fillColor;
            switch (food.Status)
            {
                case StrategyHouseholdFoodStatus.Settling:
                    statusText = "Settling in";
                    detailText = "First meal in " + FormatDuration(food.FoodGraceSecondsRemaining);
                    rowColor = new Color(0.16f, 0.28f, 0.31f, 0.94f);
                    fillColor = new Color(0.46f, 0.67f, 0.74f, 0.95f);
                    rationText = "No meal yet";
                    rationFill = 1f - Mathf.Clamp01(food.FoodGraceSecondsRemaining / Mathf.Max(1f, food.FoodGraceDurationSeconds));
                    break;
                case StrategyHouseholdFoodStatus.ShortRations:
                    statusText = "Short rations";
                    detailText = "Last meal missed " + FormatRations(food.LastMissingRations);
                    rowColor = new Color(0.32f, 0.26f, 0.16f, 0.94f);
                    fillColor = new Color(0.77f, 0.60f, 0.31f, 0.95f);
                    break;
                case StrategyHouseholdFoodStatus.Hungry:
                    statusText = "Hungry";
                    detailText = food.HungryResidentCount + " hungry, " + food.ShortageStreakDays + " short days";
                    rowColor = new Color(0.35f, 0.22f, 0.13f, 0.96f);
                    fillColor = new Color(0.86f, 0.50f, 0.24f, 0.95f);
                    break;
                case StrategyHouseholdFoodStatus.Starving:
                    statusText = "Starving";
                    detailText = food.StarvingResidentCount + " starving, mortality x" + food.MortalityMultiplier.ToString("0.0");
                    rowColor = new Color(0.36f, 0.16f, 0.13f, 0.96f);
                    fillColor = new Color(0.86f, 0.34f, 0.24f, 0.95f);
                    break;
                default:
                    statusText = "Fed";
                    detailText = food.IsFoodSupplyActivated
                        ? "Next meal in " + FormatDuration(food.NextFoodTickSeconds)
                        : "Food ready";
                    rowColor = new Color(0.15f, 0.30f, 0.22f, 0.94f);
                    fillColor = new Color(0.56f, 0.76f, 0.38f, 0.95f);
                    break;
            }

            ApplyFoodStatus(
                statusText,
                detailText,
                rationText,
                FormatFoodStockLine(homeFood, homeFoodRations, granaryFood, granaryFoodRations),
                rationFill,
                rowColor,
                fillColor);
        }

        private static string FormatFoodStockLine(
            int homeFood,
            float homeFoodRations,
            int granaryFood,
            float granaryFoodRations)
        {
            return "At home: "
                + FormatFoodAmount(homeFood, homeFoodRations)
                + "\nGranary reserve: "
                + FormatFoodAmount(granaryFood, granaryFoodRations);
        }

        private static string FormatFoodAmount(int food, float rations)
        {
            return food
                + " food, "
                + FormatRations(rations)
                + " rations";
        }

        private static string FormatRations(float rations)
        {
            return rations.ToString("0.#");
        }

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return minutes > 0
                ? minutes + "m " + remainder.ToString("00") + "s"
                : remainder + "s";
        }

        private void RefreshHouseCropRow(StrategyPlacedBuilding building)
        {
            StrategyBuildingUpgrade garden = null;
            bool hasCrop = building != null
                && building.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out garden)
                && garden.ProducedResource != StrategyResourceType.None;

            if (cropValueText != null)
            {
                cropValueText.text = hasCrop
                    ? "Garden crop: " + GetResourceTitle(garden.ProducedResource)
                    : "No garden crop";
                cropValueText.color = hasCrop
                    ? new Color(0.82f, 0.91f, 0.78f)
                    : new Color(0.60f, 0.67f, 0.64f);
            }

            if (cropIconImage != null)
            {
                cropIconImage.enabled = hasCrop;
                if (hasCrop)
                {
                    cropIconImage.sprite = StrategyResourceIconFactory.GetSprite(garden.ProducedResource);
                    cropIconImage.color = Color.white;
                }
            }
        }

        private void ApplyFoodStatus(
            string status,
            string detail,
            string ration,
            string granary,
            float rationFill,
            Color statusColor,
            Color fillColor)
        {
            if (foodStatusRowImage != null)
            {
                foodStatusRowImage.color = statusColor;
            }

            if (foodStatusText != null)
            {
                foodStatusText.text = status + "\n" + detail;
            }

            if (foodMealText != null)
            {
                foodMealText.text = ration;
            }

            if (foodGranaryText != null)
            {
                foodGranaryText.text = granary;
            }

            if (foodMealFillRect != null)
            {
                foodMealFillRect.anchorMax = new Vector2(Mathf.Clamp01(rationFill), 1f);
                foodMealFillRect.offsetMin = Vector2.zero;
                foodMealFillRect.offsetMax = Vector2.zero;
            }

            if (foodMealFillImage != null)
            {
                foodMealFillImage.color = fillColor;
            }
        }

        private void CreateResourceSlot(int index)
        {
            StrategyResourceType type = StrategyHouseResourceStore.DisplayOrder[index];
            int column = index % 2;
            int row = index / 2;
            float cellHeight = 38f;

            RectTransform slot = CreateUiObject("Resource_" + type, resourcesRoot).GetComponent<RectTransform>();
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.sizeDelta = new Vector2(ResourceCellWidth, cellHeight);
            slot.anchoredPosition = new Vector2(column * ResourceCellWidth, -166f - row * 40f);
            resourceSlots[index] = slot;

            Image background = slot.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.045f);
            background.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", slot).GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(24f, 24f);
            iconRect.anchoredPosition = new Vector2(4f, 0f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = StrategyResourceIconFactory.GetSprite(type);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            resourceIconImages[index] = icon;

            Text amountText = CreateText("Amount", slot, 10, TextAnchor.MiddleLeft, new Color(0.88f, 0.93f, 0.90f));
            amountText.fontStyle = FontStyle.Bold;
            amountText.lineSpacing = 0.95f;
            SetOffsets(amountText.rectTransform, 32f, 0f, 4f, 0f);
            resourceAmountTexts[index] = amountText;
        }

        private void EnsureResidentRowCount(int count)
        {
            while (residentRows.Count < count)
            {
                CreateResidentRow(residentRows.Count);
            }
        }

        private void CreateResidentRow(int index)
        {
            RectTransform row = CreateUiObject("Resident_" + index, residentsRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 6f, 34f + index * 38f, 6f, 34f);
            residentRows.Add(row);

            Image background = row.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.20f);
            background.raycastTarget = false;

            RectTransform portraitFrame = CreateUiObject("PortraitFrame", row).GetComponent<RectTransform>();
            SetTopLeft(portraitFrame, 4f, 4f, 32f, 30f);
            Image portraitBackground = portraitFrame.gameObject.AddComponent<Image>();
            portraitBackground.color = new Color(1f, 1f, 1f, 0.07f);
            portraitBackground.raycastTarget = false;

            RectTransform portraitRect = CreateUiObject("Portrait", portraitFrame).GetComponent<RectTransform>();
            SetOffsets(portraitRect, 2f, 2f, 2f, 2f);
            Image portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            residentPortraitImages.Add(portrait);

            Text nameText = CreateText("Name", row, 12, TextAnchor.UpperLeft, Color.white);
            nameText.fontStyle = FontStyle.Bold;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 9;
            nameText.resizeTextMaxSize = 12;
            SetTopStretch(nameText.rectTransform, 44f, 4f, 8f, 16f);
            residentNameTexts.Add(nameText);

            Text statusText = CreateText("Status", row, 11, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 9;
            statusText.resizeTextMaxSize = 11;
            SetTopStretch(statusText.rectTransform, 44f, 22f, 8f, 13f);
            residentStatusTexts.Add(statusText);

            row.gameObject.SetActive(false);
        }

        private void CreateWorkerRow(int index)
        {
            RectTransform row = CreateUiObject("Worker_" + index, workersRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 6f, 38f + index * 49f, 6f, 44f);
            workerRows[index] = row;

            Image background = row.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.20f);
            background.raycastTarget = false;

            RectTransform portraitFrame = CreateUiObject("PortraitFrame", row).GetComponent<RectTransform>();
            SetTopLeft(portraitFrame, 4f, 5f, 34f, 32f);
            Image portraitBackground = portraitFrame.gameObject.AddComponent<Image>();
            portraitBackground.color = new Color(1f, 1f, 1f, 0.07f);
            portraitBackground.raycastTarget = false;

            RectTransform portraitRect = CreateUiObject("Portrait", portraitFrame).GetComponent<RectTransform>();
            SetOffsets(portraitRect, 2f, 2f, 2f, 2f);
            Image portrait = portraitRect.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            workerPortraitImages[index] = portrait;

            Text nameText = CreateText("Name", row, 12, TextAnchor.UpperLeft, Color.white);
            nameText.fontStyle = FontStyle.Bold;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 9;
            nameText.resizeTextMaxSize = 12;
            SetTopStretch(nameText.rectTransform, 46f, 5f, 94f, 17f);
            workerNameTexts[index] = nameText;

            Text statusText = CreateText("Status", row, 11, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 9;
            statusText.resizeTextMaxSize = 11;
            SetTopStretch(statusText.rectTransform, 46f, 25f, 94f, 14f);
            workerStatusTexts[index] = statusText;

            RectTransform action = CreateUiObject("Action", row).GetComponent<RectTransform>();
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(82f, 28f);
            action.anchoredPosition = new Vector2(-6f, 0f);
            Image actionImage = action.gameObject.AddComponent<Image>();
            actionImage.color = new Color(0.18f, 0.30f, 0.27f, 0.96f);

            Button button = action.gameObject.AddComponent<Button>();
            button.targetGraphic = actionImage;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.30f, 0.27f, 0.96f);
            colors.highlightedColor = new Color(0.24f, 0.38f, 0.34f, 1f);
            colors.pressedColor = new Color(0.12f, 0.20f, 0.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.10f, 0.13f, 0.12f, 0.88f);
            button.colors = colors;
            int slotIndex = index;
            button.onClick.AddListener(() => ToggleWorkerSlot(slotIndex));
            workerButtons[index] = button;

            Text actionText = CreateText("ActionText", action, 10, TextAnchor.MiddleCenter, Color.white);
            actionText.fontStyle = FontStyle.Bold;
            actionText.resizeTextForBestFit = true;
            actionText.resizeTextMinSize = 8;
            actionText.resizeTextMaxSize = 10;
            SetOffsets(actionText.rectTransform, 4f, 0f, 4f, 0f);
            workerActionTexts[index] = actionText;
        }

        private Button CreateUpgradeButton(
            string name,
            Transform parent,
            float top,
            string label,
            out Text titleText,
            out Text stateText,
            out Text actionText)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -top - 50f);
            rect.offsetMax = new Vector2(0f, -top);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.13f, 0.12f, 0.98f);

            RectTransform accent = CreateUiObject("Accent", rect).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.offsetMin = Vector2.zero;
            accent.offsetMax = new Vector2(4f, 0f);
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.85f, 0.64f, 0.28f, 0.95f);
            accentImage.raycastTarget = false;

            RectTransform icon = CreateUiObject("Icon", rect).GetComponent<RectTransform>();
            SetTopLeft(icon, 12f, 11f, 28f, 28f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0.08f);
            iconImage.raycastTarget = false;

            Text iconText = CreateText("IconText", icon, 15, TextAnchor.MiddleCenter, new Color(0.95f, 0.78f, 0.40f));
            iconText.fontStyle = FontStyle.Bold;
            iconText.text = label == "Garden Beds" ? "G" : "K";
            SetOffsets(iconText.rectTransform, 0f, 0f, 0f, 0f);

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.13f, 0.12f, 0.98f);
            colors.highlightedColor = new Color(0.14f, 0.22f, 0.20f, 1f);
            colors.pressedColor = new Color(0.06f, 0.10f, 0.09f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.08f, 0.10f, 0.10f, 0.92f);
            button.colors = colors;

            titleText = CreateText("Title", rect, 13, TextAnchor.UpperLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = label;
            SetTopStretch(titleText.rectTransform, 50f, 8f, 98f, 18f);

            stateText = CreateText("State", rect, 11, TextAnchor.UpperLeft, new Color(0.76f, 0.83f, 0.80f));
            SetTopStretch(stateText.rectTransform, 50f, 27f, 98f, 14f);

            RectTransform action = CreateUiObject("Action", rect).GetComponent<RectTransform>();
            action.anchorMin = new Vector2(1f, 0.5f);
            action.anchorMax = new Vector2(1f, 0.5f);
            action.pivot = new Vector2(1f, 0.5f);
            action.sizeDelta = new Vector2(82f, 26f);
            action.anchoredPosition = new Vector2(-8f, 0f);
            Image actionImage = action.gameObject.AddComponent<Image>();
            actionImage.color = new Color(0.19f, 0.31f, 0.29f, 0.96f);
            actionImage.raycastTarget = false;

            actionText = CreateText("ActionText", action, 11, TextAnchor.MiddleCenter, Color.white);
            actionText.fontStyle = FontStyle.Bold;
            actionText.resizeTextForBestFit = true;
            actionText.resizeTextMinSize = 8;
            actionText.resizeTextMaxSize = 11;
            SetOffsets(actionText.rectTransform, 4f, 0f, 4f, 0f);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform CreateSectionBackground(string name, Transform parent, float top, float height)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            SetTopStretch(rect, 18f, top, 18f, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.045f);
            image.raycastTarget = false;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private static void SetTopStretch(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
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
