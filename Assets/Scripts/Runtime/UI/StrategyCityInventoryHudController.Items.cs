using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityInventoryHudController
    {
        private static readonly Color ItemRowColor =
            new(0.085f, 0.115f, 0.11f, 0.98f);
        private static readonly Color SelectedItemRowColor =
            new(0.29f, 0.22f, 0.105f, 0.98f);

        private void RefreshInventoryView()
        {
            string selectedItemId = GetSelectedItemId();
            ownedItems.Clear();
            inventory?.CopyOwnedItems(ownedItems);

            int distinctItemCount = ownedItems.Count;
            badgeText.text = distinctItemCount.ToString();
            badgeRoot.SetActive(distinctItemCount > 0);

            bool hasItems = distinctItemCount > 0;
            itemViewportRoot.SetActive(hasItems);
            detailPanelRoot.SetActive(hasItems);
            emptyStateRoot.SetActive(!hasItems);
            if (!hasItems)
            {
                selectedItemIndex = -1;
                HideAllItemRows();
                return;
            }

            EnsureItemRowCount(distinctItemCount);
            for (int index = 0; index < itemRows.Count; index++)
            {
                bool visible = index < distinctItemCount;
                ItemRowView row = itemRows[index];
                row.Root.gameObject.SetActive(visible);
                if (visible)
                {
                    PopulateItemRow(row, index);
                }
            }

            int retainedIndex = FindItemIndex(selectedItemId);
            SelectItem(retainedIndex >= 0 ? retainedIndex : 0, false, false);
        }

        private string GetSelectedItemId()
        {
            return selectedItemIndex >= 0 && selectedItemIndex < ownedItems.Count
                ? ownedItems[selectedItemIndex].ItemId
                : null;
        }

        private int FindItemIndex(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return -1;
            }

            for (int index = 0; index < ownedItems.Count; index++)
            {
                if (ownedItems[index].ItemId == itemId)
                {
                    return index;
                }
            }

            return -1;
        }

        private void HideAllItemRows()
        {
            for (int index = 0; index < itemRows.Count; index++)
            {
                itemRows[index].Root.gameObject.SetActive(false);
            }
        }

        private void EnsureItemRowCount(int requiredCount)
        {
            while (itemRows.Count < requiredCount)
            {
                itemRows.Add(CreateItemRow(itemRows.Count));
            }
        }

        private ItemRowView CreateItemRow(int index)
        {
            RectTransform root = CreateUiObject("ItemRow_" + index, itemContent)
                .GetComponent<RectTransform>();
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 88f;
            layout.minHeight = 88f;
            Image background = root.gameObject.AddComponent<Image>();
            background.color = ItemRowColor;

            ItemRowView row = new()
            {
                Root = root,
                Background = background,
                Index = index
            };

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectItem(row.Index, true, false));
            ConfigureButtonColors(button, ItemRowColor);
            StrategyUiButtonFeedback.Attach(
                button,
                StrategyUiButtonFeedbackProfile.SoundOnly);
            row.Button = button;

            RectTransform accentRoot = CreateUiObject("SelectionAccent", root)
                .GetComponent<RectTransform>();
            accentRoot.anchorMin = new Vector2(0f, 0f);
            accentRoot.anchorMax = new Vector2(0f, 1f);
            accentRoot.pivot = new Vector2(0f, 0.5f);
            accentRoot.anchoredPosition = Vector2.zero;
            accentRoot.sizeDelta = new Vector2(4f, 0f);
            row.SelectionAccent = accentRoot.gameObject.AddComponent<Image>();
            row.SelectionAccent.color = Color.clear;
            row.SelectionAccent.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", root)
                .GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 14f, 15f, 58f, 58f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.065f);
            frame.raycastTarget = false;
            RectTransform iconRoot = CreateUiObject("Icon", iconFrame)
                .GetComponent<RectTransform>();
            Stretch(iconRoot, 6f, 6f, 6f, 6f);
            row.Icon = iconRoot.gameObject.AddComponent<Image>();
            row.Icon.preserveAspect = true;
            row.Icon.raycastTarget = false;

            row.Name = CreateText(
                "Name",
                root,
                string.Empty,
                15,
                TextAnchor.UpperLeft,
                Color.white);
            row.Name.fontStyle = FontStyle.Bold;
            row.Name.resizeTextForBestFit = true;
            row.Name.resizeTextMinSize = 12;
            row.Name.resizeTextMaxSize = 15;
            SetTopStretch(row.Name.rectTransform, 84f, 12f, 96f, 24f);

            row.Summary = CreateText(
                "Summary",
                root,
                string.Empty,
                12,
                TextAnchor.UpperLeft,
                new Color(0.72f, 0.80f, 0.75f));
            row.Summary.lineSpacing = 1.05f;
            SetTopStretch(row.Summary.rectTransform, 84f, 40f, 96f, 36f);

            row.Quantity = CreateText(
                "Quantity",
                root,
                string.Empty,
                11,
                TextAnchor.MiddleRight,
                MutedGold);
            row.Quantity.fontStyle = FontStyle.Bold;
            RectTransform quantityRect = row.Quantity.rectTransform;
            quantityRect.anchorMin = new Vector2(1f, 0.5f);
            quantityRect.anchorMax = new Vector2(1f, 0.5f);
            quantityRect.pivot = new Vector2(1f, 0.5f);
            quantityRect.anchoredPosition = new Vector2(-14f, 0f);
            quantityRect.sizeDelta = new Vector2(72f, 24f);
            return row;
        }

        private void PopulateItemRow(ItemRowView row, int index)
        {
            StrategyCityInventoryEntry entry = ownedItems[index];
            StrategyCityItemDefinition definition = ResolveDefinition(entry.ItemId);
            row.Index = index;
            row.Name.text = definition != null
                ? definition.Title
                : H("hud.inventory.unknown_item");
            row.Summary.text = GetItemRowSummary(definition);
            row.Quantity.text = definition != null && definition.MaxStack == 1
                ? H("hud.inventory.unique")
                : "x" + entry.Quantity;
            row.Icon.sprite = ResolveItemIcon(definition);
            bool selected = index == selectedItemIndex;
            Color rowColor = selected ? SelectedItemRowColor : ItemRowColor;
            row.Background.color = rowColor;
            row.SelectionAccent.color = selected ? Gold : Color.clear;
            ConfigureButtonColors(row.Button, rowColor);
        }

        private void SelectItem(int index, bool playSfx, bool moveUiSelection)
        {
            if (index < 0 || index >= ownedItems.Count)
            {
                return;
            }

            bool changed = selectedItemIndex != index;
            selectedItemIndex = index;
            for (int rowIndex = 0; rowIndex < itemRows.Count; rowIndex++)
            {
                ItemRowView row = itemRows[rowIndex];
                if (!row.Root.gameObject.activeSelf)
                {
                    continue;
                }

                bool selected = rowIndex == index;
                Color rowColor = selected ? SelectedItemRowColor : ItemRowColor;
                row.Background.color = rowColor;
                row.SelectionAccent.color = selected ? Gold : Color.clear;
                ConfigureButtonColors(row.Button, rowColor);
            }

            RefreshSelectedItemDetails();
            if (moveUiSelection && EventSystem.current != null)
            {
                ItemRowView selectedRow = itemRows[index];
                selectedRow.Button.GetComponent<StrategyUiButtonFeedback>()
                    ?.SuppressNextFocusCue();
                EventSystem.current.SetSelectedGameObject(selectedRow.Button.gameObject);
            }

            if (playSfx && changed && Application.isPlaying)
            {
                StrategyHudSfxAudio.Play(StrategyHudSfxKind.Select);
            }
        }

        private void RefreshSelectedItemDetails()
        {
            StrategyCityInventoryEntry entry = ownedItems[selectedItemIndex];
            StrategyCityItemDefinition definition = ResolveDefinition(entry.ItemId);
            detailIcon.sprite = ResolveItemIcon(definition);
            detailName.text = definition != null
                ? definition.Title
                : H("hud.inventory.unknown_item");
            detailQuantity.text = definition != null && definition.MaxStack == 1
                ? H("hud.inventory.unique_city_item")
                : definition != null
                    ? H("hud.inventory.stored_capacity", entry.Quantity, definition.MaxStack)
                    : H("hud.inventory.stored", entry.Quantity);
            detailDescription.text = definition != null
                && !string.IsNullOrWhiteSpace(definition.Description)
                    ? definition.Description
                    : H("hud.inventory.story_missing");
            detailEffect.text = definition != null
                && !string.IsNullOrWhiteSpace(definition.EffectText)
                    ? definition.EffectText
                    : H("hud.inventory.effect_missing");
        }

        private static string GetItemRowSummary(StrategyCityItemDefinition definition)
        {
            if (definition == null)
            {
                return H("hud.inventory.record_missing");
            }

            if (!string.IsNullOrWhiteSpace(definition.EffectText))
            {
                return definition.EffectText;
            }

            return !string.IsNullOrWhiteSpace(definition.Description)
                ? definition.Description
                : H("hud.inventory.effect_none");
        }

        private static string H(string key, params object[] arguments)
        {
            return StrategyLocalization.Get(
                StrategyLocalizationTables.Hud,
                key,
                arguments);
        }

        private StrategyCityItemDefinition ResolveDefinition(string itemId)
        {
            return inventory != null
                && inventory.Catalog.TryGet(itemId, out StrategyCityItemDefinition definition)
                    ? definition
                    : null;
        }

        private static Sprite ResolveItemIcon(StrategyCityItemDefinition definition)
        {
            if (definition != null && !string.IsNullOrWhiteSpace(definition.IconResourcePath))
            {
                Sprite authored = Resources.Load<Sprite>(definition.IconResourcePath);
                if (authored != null)
                {
                    return authored;
                }
            }

            return GetChestSprite();
        }
    }
}
