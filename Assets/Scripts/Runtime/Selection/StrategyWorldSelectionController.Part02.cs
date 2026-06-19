using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWorldSelectionController
    {

        private void SetPreviewSprite(Sprite sprite)
        {
            if (hudPreviewImage == null)
            {
                return;
            }

            hudPreviewImage.sprite = sprite;
            hudPreviewImage.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        private void EnsureHud()
        {
            if (hudPanel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("SelectionHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            hudPanel = CreateUiObject("SelectionSideHud", canvasObject.transform).GetComponent<RectTransform>();
            hudPanel.anchorMin = new Vector2(1f, 0f);
            hudPanel.anchorMax = new Vector2(1f, 1f);
            hudPanel.pivot = new Vector2(1f, 0.5f);
            hudPanel.sizeDelta = new Vector2(HudWidth, 0f);
            hudPanel.anchoredPosition = new Vector2(HudWidth, 0f);

            Image background = hudPanel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.052f, 0.050f, 0.97f);

            hudGroup = hudPanel.gameObject.AddComponent<CanvasGroup>();
            hudGroup.alpha = 0f;
            hudGroup.blocksRaycasts = false;
            hudGroup.interactable = false;

            RectTransform accent = CreateUiObject("Accent", hudPanel).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(5f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.85f, 0.64f, 0.28f, 1f);
            accentImage.raycastTarget = false;

            RectTransform previewFrame = CreateUiObject("PreviewFrame", hudPanel).GetComponent<RectTransform>();
            SetTopLeft(previewFrame, 24f, 24f, 70f, 70f);
            Image previewBackground = previewFrame.gameObject.AddComponent<Image>();
            previewBackground.color = new Color(1f, 1f, 1f, 0.08f);
            previewBackground.raycastTarget = false;

            RectTransform previewInset = CreateUiObject("PreviewInset", previewFrame).GetComponent<RectTransform>();
            SetOffsets(previewInset, 4f, 4f, 4f, 4f);
            Image previewInsetImage = previewInset.gameObject.AddComponent<Image>();
            previewInsetImage.color = new Color(0.02f, 0.03f, 0.03f, 0.82f);
            previewInsetImage.raycastTarget = false;

            RectTransform previewImageRect = CreateUiObject("PreviewImage", previewInset).GetComponent<RectTransform>();
            SetOffsets(previewImageRect, 3f, 3f, 3f, 3f);
            hudPreviewImage = previewImageRect.gameObject.AddComponent<Image>();
            hudPreviewImage.preserveAspect = true;
            hudPreviewImage.raycastTarget = false;

            hudTitleText = CreateText("Title", hudPanel, 24, TextAnchor.UpperLeft, Color.white);
            hudTitleText.fontStyle = FontStyle.Bold;
            hudTitleText.resizeTextForBestFit = true;
            hudTitleText.resizeTextMinSize = 18;
            hudTitleText.resizeTextMaxSize = 24;
            SetTopStretch(hudTitleText.rectTransform, 108f, 27f, 24f, 34f);

            hudSubtitleText = CreateText("Subtitle", hudPanel, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            hudSubtitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudSubtitleText.rectTransform, 108f, 64f, 24f, 22f);

            RectTransform line = CreateUiObject("Divider", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(line, 24f, 112f, 24f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.15f);
            lineImage.raycastTarget = false;

            summaryBackground = CreateSectionBackground("SummaryBackground", hudPanel, 128f, 128f);
            hudSummaryTitleText = CreateText("SummaryTitle", hudPanel, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            hudSummaryTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudSummaryTitleText.rectTransform, 24f, 140f, 24f, 20f);

            hudBodyText = CreateText("Body", hudPanel, 14, TextAnchor.UpperLeft, new Color(0.84f, 0.89f, 0.91f));
            hudBodyText.lineSpacing = 1.15f;
            SetTopStretch(hudBodyText.rectTransform, 24f, 166f, 24f, 92f);

            residentsRoot = CreateUiObject("HouseResidents", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(residentsRoot, 18f, 128f, 18f, 236f);
            Image residentsBackground = residentsRoot.gameObject.AddComponent<Image>();
            residentsBackground.color = new Color(1f, 1f, 1f, 0.055f);
            residentsBackground.raycastTarget = false;
            residentsRoot.gameObject.SetActive(false);

            Text residentsTitle = CreateText("ResidentsTitle", residentsRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            residentsTitle.fontStyle = FontStyle.Bold;
            residentsTitle.text = "Residents";
            SetTopStretch(residentsTitle.rectTransform, 6f, 10f, 6f, 18f);

            residentsEmptyText = CreateText("ResidentsEmpty", residentsRoot, 13, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            residentsEmptyText.text = "no residents yet";
            SetTopStretch(residentsEmptyText.rectTransform, 6f, 44f, 6f, 24f);

            EnsureResidentRowCount(StrategyPlacedBuilding.MaxHouseResidents);

            workersRoot = CreateUiObject("LumberjackWorkers", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(workersRoot, 18f, 128f, 18f, 250f);
            Image workersBackground = workersRoot.gameObject.AddComponent<Image>();
            workersBackground.color = new Color(1f, 1f, 1f, 0.055f);
            workersBackground.raycastTarget = false;
            workersRoot.gameObject.SetActive(false);

            Text workersTitle = CreateText("WorkersTitle", workersRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            workersTitle.fontStyle = FontStyle.Bold;
            workersTitle.text = "Workers";
            SetTopStretch(workersTitle.rectTransform, 6f, 10f, 6f, 18f);

            workersEmptyText = CreateText("WorkersEmpty", workersRoot, 12, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            workersEmptyText.text = "assign residents";
            SetTopStretch(workersEmptyText.rectTransform, 6f, 220f, 6f, 22f);

            for (int i = 0; i < workerRows.Length; i++)
            {
                CreateWorkerRow(i);
            }

            statusBackground = CreateSectionBackground("StatusBackground", hudPanel, 272f, 76f);
            hudStatusTitleText = CreateText("StatusTitle", hudPanel, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            hudStatusTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudStatusTitleText.rectTransform, 24f, 284f, 24f, 20f);

            hudStatusBodyText = CreateText("StatusBody", hudPanel, 14, TextAnchor.UpperLeft, new Color(0.84f, 0.89f, 0.91f));
            hudStatusBodyText.lineSpacing = 1.12f;
            SetTopStretch(hudStatusBodyText.rectTransform, 24f, 310f, 24f, 28f);

            contextBackground = CreateSectionBackground("ContextBackground", hudPanel, 366f, 118f);
            hudContextTitleText = CreateText("ContextTitle", hudPanel, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            hudContextTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudContextTitleText.rectTransform, 24f, 378f, 24f, 20f);

            hudContextBodyText = CreateText("ContextBody", hudPanel, 13, TextAnchor.UpperLeft, new Color(0.77f, 0.86f, 0.81f));
            hudContextBodyText.lineSpacing = 1.1f;
            SetTopStretch(hudContextBodyText.rectTransform, 24f, 404f, 24f, 70f);

            CreateResidentHud();
            CreateStorageYardHud();

            resourcesRoot = CreateUiObject("HouseResources", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(resourcesRoot, 24f, 382f, 24f, 206f);
            Image resourcesBackground = resourcesRoot.gameObject.AddComponent<Image>();
            resourcesBackground.color = new Color(0.08f, 0.11f, 0.10f, 0.86f);
            resourcesBackground.raycastTarget = false;
            resourcesRoot.gameObject.SetActive(false);

            Text resourcesTitle = CreateText("ResourcesTitle", resourcesRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            resourcesTitle.fontStyle = FontStyle.Bold;
            resourcesTitle.text = "House Food";
            SetTopStretch(resourcesTitle.rectTransform, 6f, 8f, 6f, 18f);

            RectTransform foodStatusRow = CreateUiObject("FoodStatusRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodStatusRow, 6f, 32f, 6f, 40f);
            foodStatusRowImage = foodStatusRow.gameObject.AddComponent<Image>();
            foodStatusRowImage.color = new Color(0.16f, 0.25f, 0.22f, 0.92f);
            foodStatusRowImage.raycastTarget = false;

            RectTransform foodIconRect = CreateUiObject("FoodIcon", foodStatusRow).GetComponent<RectTransform>();
            SetTopLeft(foodIconRect, 8f, 6f, 20f, 20f);
            Image foodIcon = foodIconRect.gameObject.AddComponent<Image>();
            foodIcon.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Game);
            foodIcon.preserveAspect = true;
            foodIcon.raycastTarget = false;

            foodStatusText = CreateText("FoodStatusText", foodStatusRow, 12, TextAnchor.MiddleLeft, Color.white);
            foodStatusText.fontStyle = FontStyle.Bold;
            SetOffsets(foodStatusText.rectTransform, 36f, 0f, 96f, 0f);

            foodMealText = CreateText("FoodMealText", foodStatusRow, 10, TextAnchor.MiddleRight, new Color(0.88f, 0.93f, 0.90f));
            foodMealText.fontStyle = FontStyle.Bold;
            SetOffsets(foodMealText.rectTransform, 184f, 0f, 8f, 0f);

            RectTransform foodMeter = CreateUiObject("FoodMealMeter", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodMeter, 6f, 78f, 6f, 8f);
            Image foodMeterBackground = foodMeter.gameObject.AddComponent<Image>();
            foodMeterBackground.color = new Color(0.01f, 0.03f, 0.025f, 0.88f);
            foodMeterBackground.raycastTarget = false;

            foodMealFillRect = CreateUiObject("FoodMealMeterFill", foodMeter).GetComponent<RectTransform>();
            foodMealFillRect.anchorMin = Vector2.zero;
            foodMealFillRect.anchorMax = new Vector2(0f, 1f);
            foodMealFillRect.offsetMin = Vector2.zero;
            foodMealFillRect.offsetMax = Vector2.zero;
            foodMealFillImage = foodMealFillRect.gameObject.AddComponent<Image>();
            foodMealFillImage.color = new Color(0.63f, 0.74f, 0.42f, 0.95f);
            foodMealFillImage.raycastTarget = false;

            RectTransform granaryRow = CreateUiObject("GranaryFoodRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(granaryRow, 6f, 94f, 6f, 38f);
            Image granaryBackground = granaryRow.gameObject.AddComponent<Image>();
            granaryBackground.color = new Color(1f, 1f, 1f, 0.035f);
            granaryBackground.raycastTarget = false;

            RectTransform granaryIconRect = CreateUiObject("GranaryIcon", granaryRow).GetComponent<RectTransform>();
            SetTopLeft(granaryIconRect, 8f, 11f, 16f, 16f);
            Image granaryIcon = granaryIconRect.gameObject.AddComponent<Image>();
            granaryIcon.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Fish);
            granaryIcon.preserveAspect = true;
            granaryIcon.color = new Color(0.82f, 0.90f, 0.87f, 0.88f);
            granaryIcon.raycastTarget = false;

            foodGranaryText = CreateText("GranaryFoodText", granaryRow, 11, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            foodGranaryText.fontStyle = FontStyle.Bold;
            SetOffsets(foodGranaryText.rectTransform, 34f, 0f, 8f, 0f);

            RectTransform cropRow = CreateUiObject("CropRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(cropRow, 6f, 138f, 6f, 24f);
            Image cropBackground = cropRow.gameObject.AddComponent<Image>();
            cropBackground.color = new Color(1f, 1f, 1f, 0.035f);
            cropBackground.raycastTarget = false;

            RectTransform cropIconRect = CreateUiObject("CropIcon", cropRow).GetComponent<RectTransform>();
            SetTopLeft(cropIconRect, 8f, 4f, 16f, 16f);
            cropIconImage = cropIconRect.gameObject.AddComponent<Image>();
            cropIconImage.preserveAspect = true;
            cropIconImage.raycastTarget = false;

            cropValueText = CreateText("CropValueText", cropRow, 11, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            cropValueText.fontStyle = FontStyle.Bold;
            SetOffsets(cropValueText.rectTransform, 34f, 0f, 8f, 0f);

            resourcesEmptyText = CreateText("ResourcesEmptyText", resourcesRoot, 11, TextAnchor.UpperLeft, new Color(0.62f, 0.70f, 0.66f));
            resourcesEmptyText.text = "No food stored at this house";
            SetTopStretch(resourcesEmptyText.rectTransform, 6f, 170f, 6f, 24f);

            for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length; i++)
            {
                CreateResourceSlot(i);
            }

            upgradeActionsRoot = CreateUiObject("HouseUpgradeActions", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(upgradeActionsRoot, 24f, 592f, 24f, 196f);
            Image upgradesBackground = upgradeActionsRoot.gameObject.AddComponent<Image>();
            upgradesBackground.color = new Color(0.05f, 0.08f, 0.075f, 0.86f);
            upgradesBackground.raycastTarget = false;
            upgradeActionsRoot.gameObject.SetActive(false);

            Text upgradesTitle = CreateText("UpgradeTitle", upgradeActionsRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            upgradesTitle.fontStyle = FontStyle.Bold;
            upgradesTitle.text = "Upgrades";
            SetTopStretch(upgradesTitle.rectTransform, 0f, 0f, 0f, 20f);

            gardenBedsButton = CreateUpgradeButton(
                "GardenBedsButton",
                upgradeActionsRoot,
                34f,
                "Garden Beds",
                out gardenBedsButtonText,
                out gardenBedsStateText,
                out gardenBedsActionText);
            gardenBedsButton.onClick.AddListener(() => TryInstallSelectedUpgrade(StrategyBuildingUpgradeType.GardenBeds));

            chickenCoopButton = CreateUpgradeButton(
                "ChickenCoopButton",
                upgradeActionsRoot,
                92f,
                "Chicken Coop",
                out chickenCoopButtonText,
                out chickenCoopStateText,
                out chickenCoopActionText);
            chickenCoopButton.onClick.AddListener(() => TryInstallSelectedUpgrade(StrategyBuildingUpgradeType.ChickenCoop));

            upgradeStatusText = CreateText("UpgradeStatus", upgradeActionsRoot, 12, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            upgradeStatusText.lineSpacing = 1.05f;
            SetTopStretch(upgradeStatusText.rectTransform, 0f, 152f, 0f, 34f);
        }

        private void RefreshResidents(StrategyPlacedBuilding building)
        {
            int residentCount = building != null && building.Residents != null ? building.Residents.Count : 0;
            EnsureResidentRowCount(Mathf.Max(StrategyPlacedBuilding.MaxHouseResidents, residentCount));

            if (residentsEmptyText != null)
            {
                residentsEmptyText.gameObject.SetActive(residentCount <= 0);
            }

            for (int i = 0; i < residentRows.Count; i++)
            {
                StrategyResidentAgent resident = i < residentCount ? building.Residents[i] : null;
                bool visible = resident != null;
                if (residentRows[i] != null)
                {
                    residentRows[i].gameObject.SetActive(visible);
                }

                if (!visible)
                {
                    continue;
                }

                if (residentPortraitImages[i] != null)
                {
                    residentPortraitImages[i].sprite = StrategyResidentSpriteFactory.GetPortraitSprite(
                        resident.Gender,
                        resident.VisualVariant,
                        resident.LifeStage);
                    residentPortraitImages[i].color = Color.white;
                }

                if (residentNameTexts[i] != null)
                {
                    residentNameTexts[i].text = resident.FullName;
                }

                if (residentStatusTexts[i] != null)
                {
                    string householdRole = resident == building.Householder ? "Householder, " : string.Empty;
                    residentStatusTexts[i].text = householdRole
                        + GetResidentLifeStageTitle(resident)
                        + ", "
                        + resident.DisplayAgeYears
                        + " years"
                        + (resident.IsHungry ? ", " + resident.NutritionStatusText : string.Empty);
                }
            }
        }

        private void RefreshWorkers(StrategyLumberjackCamp camp)
        {
            int workerCount = camp != null ? camp.WorkerCount : 0;
            bool canAssign = camp != null && camp.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? "assign residents"
                    : "no free residents";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyLumberjackCamp.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = camp != null && camp.TryGetWorker(i, out worker);

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant, worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : "Open slot";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "up to 2 workers";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? "Remove"
                        : "Assign";
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void RefreshWorkers(StrategyHunterCamp camp)
        {
            int workerCount = camp != null ? camp.WorkerCount : 0;
            bool canAssign = camp != null && camp.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? "assign hunters"
                    : "no free residents";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyHunterCamp.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = camp != null && camp.TryGetWorker(i, out worker);

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant, worker.LifeStage)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : "Hunter: open";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "hunts rabbits";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? "Remove"
                        : "Assign";
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }
    }
}
