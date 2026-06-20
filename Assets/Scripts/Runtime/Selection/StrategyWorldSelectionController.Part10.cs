using UnityEngine;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {
        private const int StorageChipHaulers = 0;
        private const int StorageChipBuilders = 1;
        private const int StorageChipSources = 2;
        private const int StorageChipCount = 3;
        private const int StorageResourceCount = 7;

        private static readonly StrategyResourceType[] StorageResourceTypes =
        {
            StrategyResourceType.Logs,
            StrategyResourceType.Stone,
            StrategyResourceType.Planks,
            StrategyResourceType.Iron,
            StrategyResourceType.Coal,
            StrategyResourceType.Clay,
            StrategyResourceType.Pottery
        };

        private RectTransform storageYardHudRoot;
        private readonly Image[] storageChipBackgrounds = new Image[StorageChipCount];
        private readonly Image[] storageChipIcons = new Image[StorageChipCount];
        private readonly Text[] storageChipTexts = new Text[StorageChipCount];
        private readonly Image[] storageResourceBackgrounds = new Image[StorageResourceCount];
        private readonly Image[] storageResourceIcons = new Image[StorageResourceCount];
        private readonly Text[] storageResourceNameTexts = new Text[StorageResourceCount];
        private readonly Text[] storageResourceAmountTexts = new Text[StorageResourceCount];
        private readonly Text[] storageResourceDetailTexts = new Text[StorageResourceCount];
        private Text storageStatusTitleText;
        private Text storageStatusBodyText;
        private Image storageStatusBackground;

        private void CreateStorageYardHud()
        {
            storageYardHudRoot = CreateUiObject("StorageYardHud", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(storageYardHudRoot, 18f, 128f, 18f, 508f);
            storageYardHudRoot.gameObject.SetActive(false);

            CreateStorageChip(StorageChipHaulers, "HaulersChip", 0f, "Haulers");
            CreateStorageChip(StorageChipBuilders, "BuildersChip", 108f, "Builders");
            CreateStorageChip(StorageChipSources, "SourcesChip", 216f, "Sources");

            Text stockTitle = CreateText("StockTitle", storageYardHudRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            stockTitle.fontStyle = FontStyle.Bold;
            stockTitle.text = "Stock";
            SetTopStretch(stockTitle.rectTransform, 0f, 48f, 0f, 20f);

            CreateStorageResourceCard(0, 0f, 74f);
            CreateStorageResourceCard(1, 166f, 74f);
            CreateStorageResourceCard(2, 0f, 152f);
            CreateStorageResourceCard(3, 166f, 152f);
            CreateStorageResourceCard(4, 0f, 230f);
            CreateStorageResourceCard(5, 166f, 230f);
            CreateStorageResourceCard(6, 0f, 308f);
            CreateStorageStatusPanel();
        }

        private void RefreshStorageYardHud(StrategyStorageYard yard)
        {
            SetProfileSectionVisible(false);
            SetStatusSectionVisible(false);
            SetContextSectionVisible(false);
            SetResidentsSectionVisible(false);
            SetWorkersSectionVisible(false);
            SetResourcesVisible(false);
            SetUpgradeActionsVisible(false);
            SetStorageYardHudVisible(true);

            int sourceCount = yard != null ? yard.GetAvailableSourceCount() : 0;
            SetStorageChip(
                StorageChipHaulers,
                "Haulers " + (yard != null ? yard.WorkerCount : 0),
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.StorageWorker),
                GetStorageLaborColor(yard != null ? yard.WorkerCount : 0));
            SetStorageChip(
                StorageChipBuilders,
                "Builders " + (yard != null ? yard.BuilderCount : 0),
                StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder),
                GetStorageLaborColor(yard != null ? yard.BuilderCount : 0));
            SetStorageChip(
                StorageChipSources,
                "Sources " + sourceCount,
                GetStorageYardIcon(),
                sourceCount > 0 ? new Color(0.14f, 0.22f, 0.18f, 0.96f) : new Color(0.17f, 0.16f, 0.13f, 0.96f));

            for (int i = 0; i < StorageResourceTypes.Length; i++)
            {
                StrategyResourceType type = StorageResourceTypes[i];
                int amount = GetStorageResourceAmount(yard, type);
                SetStorageResourceCard(i, type, amount, GetStorageResourceDetail(yard, type));
            }

            storageStatusTitleText.text = GetStorageStatusTitle(yard, sourceCount);
            storageStatusBodyText.text = GetStorageStatusBody(yard, sourceCount);
            storageStatusBackground.color = GetStorageStatusColor(yard, sourceCount);
        }

        private void SetStorageYardHudVisible(bool visible)
        {
            if (storageYardHudRoot != null)
            {
                storageYardHudRoot.gameObject.SetActive(visible);
            }
        }

        private void CreateStorageChip(int index, string name, float left, string label)
        {
            RectTransform rect = CreateUiObject(name, storageYardHudRoot).GetComponent<RectTransform>();
            SetTopLeft(rect, left, 0f, 100f, 34f);
            storageChipBackgrounds[index] = rect.gameObject.AddComponent<Image>();
            storageChipBackgrounds[index].color = new Color(0.10f, 0.15f, 0.14f, 0.92f);
            storageChipBackgrounds[index].raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", rect).GetComponent<RectTransform>();
            SetTopLeft(iconRect, 7f, 6f, 22f, 22f);
            storageChipIcons[index] = iconRect.gameObject.AddComponent<Image>();
            storageChipIcons[index].preserveAspect = true;
            storageChipIcons[index].raycastTarget = false;

            storageChipTexts[index] = CreateText("Text", rect, 11, TextAnchor.MiddleLeft, Color.white);
            storageChipTexts[index].fontStyle = FontStyle.Bold;
            storageChipTexts[index].resizeTextForBestFit = true;
            storageChipTexts[index].resizeTextMinSize = 8;
            storageChipTexts[index].resizeTextMaxSize = 11;
            storageChipTexts[index].text = label;
            SetOffsets(storageChipTexts[index].rectTransform, 34f, 0f, 6f, 0f);
        }

        private void CreateStorageResourceCard(int index, float left, float top)
        {
            RectTransform card = CreateUiObject("Resource_" + index, storageYardHudRoot).GetComponent<RectTransform>();
            SetTopLeft(card, left, top, 150f, 66f);
            storageResourceBackgrounds[index] = card.gameObject.AddComponent<Image>();
            storageResourceBackgrounds[index].color = new Color(0.08f, 0.11f, 0.10f, 0.88f);
            storageResourceBackgrounds[index].raycastTarget = false;

            RectTransform iconFrame = CreateUiObject("IconFrame", card).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 10f, 14f, 34f, 34f);
            Image frame = iconFrame.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0.06f);
            frame.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 5f, 5f, 5f, 5f);
            storageResourceIcons[index] = iconRect.gameObject.AddComponent<Image>();
            storageResourceIcons[index].preserveAspect = true;
            storageResourceIcons[index].raycastTarget = false;

            storageResourceNameTexts[index] = CreateText("Name", card, 12, TextAnchor.UpperLeft, Color.white);
            storageResourceNameTexts[index].fontStyle = FontStyle.Bold;
            SetTopStretch(storageResourceNameTexts[index].rectTransform, 54f, 10f, 8f, 17f);

            storageResourceAmountTexts[index] = CreateText("Amount", card, 17, TextAnchor.UpperLeft, new Color(0.95f, 0.78f, 0.40f));
            storageResourceAmountTexts[index].fontStyle = FontStyle.Bold;
            SetTopStretch(storageResourceAmountTexts[index].rectTransform, 54f, 28f, 8f, 22f);

            storageResourceDetailTexts[index] = CreateText("Detail", card, 10, TextAnchor.UpperLeft, new Color(0.70f, 0.78f, 0.75f));
            storageResourceDetailTexts[index].resizeTextForBestFit = true;
            storageResourceDetailTexts[index].resizeTextMinSize = 8;
            storageResourceDetailTexts[index].resizeTextMaxSize = 10;
            SetTopStretch(storageResourceDetailTexts[index].rectTransform, 54f, 50f, 8f, 12f);
        }

        private void CreateStorageStatusPanel()
        {
            RectTransform row = CreateUiObject("Status", storageYardHudRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 0f, 386f, 0f, 76f);
            storageStatusBackground = row.gameObject.AddComponent<Image>();
            storageStatusBackground.color = new Color(0.10f, 0.15f, 0.14f, 0.92f);
            storageStatusBackground.raycastTarget = false;

            storageStatusTitleText = CreateText("Title", row, 13, TextAnchor.UpperLeft, Color.white);
            storageStatusTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(storageStatusTitleText.rectTransform, 14f, 12f, 14f, 18f);

            storageStatusBodyText = CreateText("Body", row, 12, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.82f));
            storageStatusBodyText.lineSpacing = 1.08f;
            SetTopStretch(storageStatusBodyText.rectTransform, 14f, 34f, 14f, 34f);
        }

        private void SetStorageChip(int index, string text, Sprite icon, Color color)
        {
            storageChipBackgrounds[index].color = color;
            storageChipTexts[index].text = text;
            storageChipIcons[index].sprite = icon;
            storageChipIcons[index].color = icon != null ? Color.white : Color.clear;
        }

        private void SetStorageResourceCard(int index, StrategyResourceType type, int amount, string detail)
        {
            bool hasStock = amount > 0;
            storageResourceBackgrounds[index].color = hasStock
                ? new Color(0.10f, 0.15f, 0.14f, 0.94f)
                : new Color(0.07f, 0.09f, 0.09f, 0.80f);
            storageResourceIcons[index].sprite = StrategyResourceIconFactory.GetSprite(type);
            storageResourceIcons[index].color = hasStock ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            storageResourceNameTexts[index].text = GetResourceTitle(type);
            storageResourceNameTexts[index].color = hasStock ? Color.white : new Color(0.60f, 0.66f, 0.64f);
            storageResourceAmountTexts[index].text = amount.ToString();
            storageResourceAmountTexts[index].color = hasStock ? new Color(0.95f, 0.78f, 0.40f) : new Color(0.48f, 0.50f, 0.48f);
            storageResourceDetailTexts[index].text = detail;
        }

        private static int GetStorageResourceAmount(StrategyStorageYard yard, StrategyResourceType type)
        {
            if (yard == null)
            {
                return 0;
            }

            return type switch
            {
                StrategyResourceType.Logs => yard.LogsStored,
                StrategyResourceType.Stone => yard.StoneStored,
                StrategyResourceType.Planks => yard.PlanksStored,
                StrategyResourceType.Iron => yard.IronStored,
                StrategyResourceType.Coal => yard.CoalStored,
                StrategyResourceType.Clay => yard.ClayStored,
                StrategyResourceType.Pottery => yard.PotteryStored,
                _ => 0
            };
        }

        private static string GetStorageResourceDetail(StrategyStorageYard yard, StrategyResourceType type)
        {
            if (yard == null)
            {
                return "no stock";
            }

            return type switch
            {
                StrategyResourceType.Logs => "available " + yard.AvailableConstructionLogs,
                StrategyResourceType.Stone => "available " + yard.AvailableConstructionStone,
                StrategyResourceType.Planks => "available " + yard.AvailableConstructionPlanks,
                StrategyResourceType.Iron => "stored",
                StrategyResourceType.Coal => "stored",
                StrategyResourceType.Clay => "stored",
                StrategyResourceType.Pottery => "stored",
                _ => "stored"
            };
        }

        private static string GetStorageStatusTitle(StrategyStorageYard yard, int sourceCount)
        {
            if (yard == null || yard.WorkerCount <= 0)
            {
                return "No haulers assigned";
            }

            return sourceCount > 0 ? "Ready for hauling" : "Idle logistics";
        }

        private static string GetStorageStatusBody(StrategyStorageYard yard, int sourceCount)
        {
            if (yard == null)
            {
                return "Storage data unavailable.";
            }

            if (yard.WorkerCount <= 0)
            {
                return "Assign Haulers in the Professions HUD to move resources.";
            }

            if (sourceCount > 0)
            {
                return sourceCount + " source" + (sourceCount == 1 ? " has" : "s have") + " stock waiting for pickup.";
            }

            return yard.BuilderCount > 0
                ? "No source stock is waiting. Builders are ready for construction jobs."
                : "No source stock is waiting. Builders can be assigned separately.";
        }

        private static Color GetStorageStatusColor(StrategyStorageYard yard, int sourceCount)
        {
            if (yard == null || yard.WorkerCount <= 0)
            {
                return new Color(0.25f, 0.13f, 0.08f, 0.94f);
            }

            return sourceCount > 0
                ? new Color(0.12f, 0.20f, 0.16f, 0.94f)
                : new Color(0.12f, 0.15f, 0.16f, 0.94f);
        }

        private static Color GetStorageLaborColor(int count)
        {
            return count > 0
                ? new Color(0.14f, 0.22f, 0.18f, 0.96f)
                : new Color(0.23f, 0.16f, 0.09f, 0.96f);
        }

        private static Sprite GetStorageYardIcon()
        {
            return StrategyBuildingSpriteFactory.TryGetBuildSprite(StrategyBuildTool.StorageYard, out Sprite sprite)
                ? sprite
                : null;
        }
    }
}
