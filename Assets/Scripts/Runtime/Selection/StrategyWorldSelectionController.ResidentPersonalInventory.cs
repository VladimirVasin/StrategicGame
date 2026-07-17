using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const float ResidentPersonalInventoryTop = 412f;
        private const float ResidentPersonalInventoryHeight = 180f;
        private const float ResidentPersonalItemSlotWidth = 152f;
        private const float ResidentPersonalItemSlotHeight = 40f;

        private readonly List<StrategyResidentPersonalItemEntry> residentPersonalItemScratch = new();
        private readonly Dictionary<string, Sprite> residentPersonalItemIcons = new();
        private readonly RectTransform[] residentPersonalItemSlots =
            new RectTransform[StrategyResidentPersonalInventory.SlotCapacity];
        private readonly Image[] residentPersonalItemIconImages =
            new Image[StrategyResidentPersonalInventory.SlotCapacity];
        private readonly Text[] residentPersonalItemFallbackTexts =
            new Text[StrategyResidentPersonalInventory.SlotCapacity];
        private readonly Text[] residentPersonalItemTitleTexts =
            new Text[StrategyResidentPersonalInventory.SlotCapacity];
        private readonly Text[] residentPersonalItemQuantityTexts =
            new Text[StrategyResidentPersonalInventory.SlotCapacity];

        private RectTransform residentPersonalInventoryRoot;
        private Text residentPersonalInventoryCountText;
        private Text residentPersonalInventoryEmptyText;

        internal bool IsResidentPersonalInventoryVisible =>
            residentPersonalInventoryRoot != null
            && residentPersonalInventoryRoot.gameObject.activeSelf;
        internal string ResidentPersonalInventoryCountCopy =>
            residentPersonalInventoryCountText != null
                ? residentPersonalInventoryCountText.text
                : string.Empty;
        internal string ResidentPersonalInventoryEmptyCopy =>
            residentPersonalInventoryEmptyText != null
                ? residentPersonalInventoryEmptyText.text
                : string.Empty;

        private void CreateResidentPersonalInventoryHud()
        {
            residentPersonalInventoryRoot = CreateUiObject(
                "ResidentPersonalInventory",
                residentHudRoot).GetComponent<RectTransform>();
            SetTopStretch(
                residentPersonalInventoryRoot,
                0f,
                ResidentPersonalInventoryTop,
                0f,
                ResidentPersonalInventoryHeight);

            Image background = residentPersonalInventoryRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.10f, 0.90f);
            background.raycastTarget = false;

            Text title = CreateText(
                "Title",
                residentPersonalInventoryRoot,
                13,
                TextAnchor.UpperLeft,
                new Color(0.86f, 0.70f, 0.42f));
            title.fontStyle = FontStyle.Bold;
            title.text = "Personal Items";
            SetTopStretch(title.rectTransform, 10f, 9f, 80f, 20f);

            residentPersonalInventoryCountText = CreateText(
                "Count",
                residentPersonalInventoryRoot,
                11,
                TextAnchor.UpperRight,
                new Color(0.74f, 0.82f, 0.79f));
            residentPersonalInventoryCountText.fontStyle = FontStyle.Bold;
            SetTopStretch(residentPersonalInventoryCountText.rectTransform, 190f, 10f, 10f, 18f);

            residentPersonalInventoryEmptyText = CreateText(
                "Empty",
                residentPersonalInventoryRoot,
                12,
                TextAnchor.MiddleCenter,
                new Color(0.69f, 0.77f, 0.74f));
            residentPersonalInventoryEmptyText.text =
                "No personal items\nSpecial belongings will appear here.";
            residentPersonalInventoryEmptyText.lineSpacing = 1.15f;
            SetTopStretch(residentPersonalInventoryEmptyText.rectTransform, 16f, 55f, 16f, 62f);

            for (int index = 0; index < residentPersonalItemSlots.Length; index++)
            {
                CreateResidentPersonalItemSlot(index);
            }
        }

        private void CreateResidentPersonalItemSlot(int index)
        {
            int column = index % 2;
            int row = index / 2;
            RectTransform slot = CreateUiObject(
                "PersonalItemSlot_" + index,
                residentPersonalInventoryRoot).GetComponent<RectTransform>();
            SetTopLeft(
                slot,
                6f + column * 158f,
                35f + row * 46f,
                ResidentPersonalItemSlotWidth,
                ResidentPersonalItemSlotHeight);
            residentPersonalItemSlots[index] = slot;

            Image slotBackground = slot.gameObject.AddComponent<Image>();
            slotBackground.color = new Color(0.12f, 0.17f, 0.16f, 0.96f);
            slotBackground.raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", slot).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 6f, 6f, 28f, 28f);
            Image iconBackground = iconFrame.gameObject.AddComponent<Image>();
            iconBackground.color = new Color(1f, 1f, 1f, 0.06f);
            iconBackground.raycastTarget = false;

            residentPersonalItemIconImages[index] = CreateUiObject("Icon", iconFrame).AddComponent<Image>();
            residentPersonalItemIconImages[index].preserveAspect = true;
            residentPersonalItemIconImages[index].raycastTarget = false;
            SetOffsets(residentPersonalItemIconImages[index].rectTransform, 3f, 3f, 3f, 3f);

            residentPersonalItemFallbackTexts[index] = CreateText(
                "Fallback",
                iconFrame,
                12,
                TextAnchor.MiddleCenter,
                new Color(0.95f, 0.78f, 0.40f));
            residentPersonalItemFallbackTexts[index].fontStyle = FontStyle.Bold;
            SetOffsets(residentPersonalItemFallbackTexts[index].rectTransform, 0f, 0f, 0f, 0f);

            residentPersonalItemTitleTexts[index] = CreateText(
                "Title",
                slot,
                11,
                TextAnchor.MiddleLeft,
                Color.white);
            residentPersonalItemTitleTexts[index].fontStyle = FontStyle.Bold;
            residentPersonalItemTitleTexts[index].resizeTextForBestFit = true;
            residentPersonalItemTitleTexts[index].resizeTextMinSize = 8;
            residentPersonalItemTitleTexts[index].resizeTextMaxSize = 11;
            SetOffsets(residentPersonalItemTitleTexts[index].rectTransform, 40f, 0f, 34f, 0f);

            residentPersonalItemQuantityTexts[index] = CreateText(
                "Quantity",
                slot,
                10,
                TextAnchor.MiddleRight,
                new Color(0.82f, 0.88f, 0.85f));
            residentPersonalItemQuantityTexts[index].fontStyle = FontStyle.Bold;
            SetOffsets(residentPersonalItemQuantityTexts[index].rectTransform, 112f, 0f, 7f, 0f);
            slot.gameObject.SetActive(false);
        }

        private void RefreshResidentPersonalInventoryHud(StrategyResidentAgent resident)
        {
            bool visible = resident != null && resident.IsAdult;
            residentPersonalInventoryRoot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            resident.CopyPersonalItems(residentPersonalItemScratch);
            int itemCount = residentPersonalItemScratch.Count;
            residentPersonalInventoryCountText.text = itemCount
                + " / "
                + StrategyResidentPersonalInventory.SlotCapacity;
            residentPersonalInventoryEmptyText.gameObject.SetActive(itemCount == 0);

            for (int index = 0; index < residentPersonalItemSlots.Length; index++)
            {
                bool hasItem = index < itemCount;
                residentPersonalItemSlots[index].gameObject.SetActive(hasItem);
                if (!hasItem)
                {
                    continue;
                }

                StrategyResidentPersonalItemEntry entry = residentPersonalItemScratch[index];
                if (!resident.PersonalItemCatalog.TryGet(
                        entry.ItemId,
                        out StrategyResidentItemDefinition definition))
                {
                    continue;
                }

                Sprite icon = ResolveResidentPersonalItemIcon(definition);
                residentPersonalItemIconImages[index].sprite = icon;
                residentPersonalItemIconImages[index].color = icon != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0f);
                residentPersonalItemFallbackTexts[index].text = icon == null
                    ? GetPersonalItemFallback(definition.Title)
                    : string.Empty;
                residentPersonalItemTitleTexts[index].text = definition.Title;
                residentPersonalItemQuantityTexts[index].text = entry.Quantity > 1
                    ? "x" + entry.Quantity
                    : string.Empty;
            }
        }

        private Sprite ResolveResidentPersonalItemIcon(StrategyResidentItemDefinition definition)
        {
            if (residentPersonalItemIcons.TryGetValue(definition.Id, out Sprite cached))
            {
                return cached;
            }

            Sprite icon = string.IsNullOrWhiteSpace(definition.IconResourcePath)
                ? null
                : Resources.Load<Sprite>(definition.IconResourcePath);
            residentPersonalItemIcons.Add(definition.Id, icon);
            return icon;
        }

        private static string GetPersonalItemFallback(string title)
        {
            return string.IsNullOrWhiteSpace(title)
                ? "?"
                : char.ToUpperInvariant(title[0]).ToString();
        }
    }
}
