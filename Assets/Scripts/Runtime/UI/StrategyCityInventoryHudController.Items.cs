using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyCityInventoryHudController
    {
        private static readonly Color ItemCardColor =
            new(0.085f, 0.115f, 0.11f, 0.98f);
        private static readonly Color SelectedItemCardColor =
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
                HideAllItemCards();
                return;
            }

            EnsureItemCardCount(distinctItemCount);
            for (int index = 0; index < itemCards.Count; index++)
            {
                bool visible = index < distinctItemCount;
                ItemCardView card = itemCards[index];
                card.Root.gameObject.SetActive(visible);
                if (visible)
                {
                    PopulateItemCard(card, index);
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

        private void HideAllItemCards()
        {
            for (int index = 0; index < itemCards.Count; index++)
            {
                itemCards[index].Root.gameObject.SetActive(false);
            }
        }

        private void EnsureItemCardCount(int requiredCount)
        {
            while (itemCards.Count < requiredCount)
            {
                itemCards.Add(CreateItemCard(itemCards.Count));
            }
        }

        private ItemCardView CreateItemCard(int index)
        {
            RectTransform root = CreateUiObject("ItemCard_" + index, itemContent)
                .GetComponent<RectTransform>();
            Image background = root.gameObject.AddComponent<Image>();
            background.color = ItemCardColor;

            ItemCardView card = new()
            {
                Root = root,
                Background = background,
                Index = index
            };

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectItem(card.Index, true, false));
            ConfigureButtonColors(button, ItemCardColor);
            StrategyUiButtonFeedback.Attach(
                button,
                StrategyUiButtonFeedbackProfile.SoundOnly);
            card.Button = button;

            RectTransform iconFrame = CreateUiObject("IconFrame", root)
                .GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 8f, 11f, 54f, 54f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.065f);
            frame.raycastTarget = false;
            RectTransform iconRoot = CreateUiObject("Icon", iconFrame)
                .GetComponent<RectTransform>();
            Stretch(iconRoot, 6f, 6f, 6f, 6f);
            card.Icon = iconRoot.gameObject.AddComponent<Image>();
            card.Icon.preserveAspect = true;
            card.Icon.raycastTarget = false;

            card.Name = CreateText(
                "Name",
                root,
                string.Empty,
                13,
                TextAnchor.UpperLeft,
                Color.white);
            card.Name.fontStyle = FontStyle.Bold;
            card.Name.resizeTextForBestFit = true;
            card.Name.resizeTextMinSize = 10;
            card.Name.resizeTextMaxSize = 13;
            SetTopLeft(card.Name.rectTransform, 69f, 12f, 80f, 36f);

            card.Quantity = CreateText(
                "Quantity",
                root,
                string.Empty,
                11,
                TextAnchor.LowerLeft,
                MutedGold);
            card.Quantity.fontStyle = FontStyle.Bold;
            SetTopLeft(card.Quantity.rectTransform, 69f, 49f, 80f, 18f);
            return card;
        }

        private void PopulateItemCard(ItemCardView card, int index)
        {
            StrategyCityInventoryEntry entry = ownedItems[index];
            StrategyCityItemDefinition definition = ResolveDefinition(entry.ItemId);
            card.Index = index;
            card.Name.text = definition != null ? definition.Title : "Unknown Item";
            card.Quantity.text = "x" + entry.Quantity;
            card.Icon.sprite = ResolveItemIcon(definition);
            Color cardColor = index == selectedItemIndex
                ? SelectedItemCardColor
                : ItemCardColor;
            card.Background.color = cardColor;
            ConfigureButtonColors(card.Button, cardColor);
        }

        private void SelectItem(int index, bool playSfx, bool moveUiSelection)
        {
            if (index < 0 || index >= ownedItems.Count)
            {
                return;
            }

            bool changed = selectedItemIndex != index;
            selectedItemIndex = index;
            for (int cardIndex = 0; cardIndex < itemCards.Count; cardIndex++)
            {
                ItemCardView card = itemCards[cardIndex];
                if (!card.Root.gameObject.activeSelf)
                {
                    continue;
                }

                Color cardColor = cardIndex == index
                    ? SelectedItemCardColor
                    : ItemCardColor;
                card.Background.color = cardColor;
                ConfigureButtonColors(card.Button, cardColor);
            }

            RefreshSelectedItemDetails();
            if (moveUiSelection && EventSystem.current != null)
            {
                ItemCardView selectedCard = itemCards[index];
                selectedCard.Button.GetComponent<StrategyUiButtonFeedback>()
                    ?.SuppressNextFocusCue();
                EventSystem.current.SetSelectedGameObject(selectedCard.Button.gameObject);
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
            detailName.text = definition != null ? definition.Title : "Unknown Item";
            detailQuantity.text = definition != null
                ? entry.Quantity + " / " + definition.MaxStack + " STORED"
                : entry.Quantity + " STORED";
            detailDescription.text = definition != null
                && !string.IsNullOrWhiteSpace(definition.Description)
                    ? definition.Description
                    : "This item's story has not been recorded yet.";
            detailEffect.text = definition != null
                && !string.IsNullOrWhiteSpace(definition.EffectText)
                    ? definition.EffectText
                    : "No active effect is recorded.";
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
