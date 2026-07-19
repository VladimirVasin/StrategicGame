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
            canvas.sortingOrder = 180;

            StrategyHudStyle.ConfigureScaler(canvasObject.GetComponent<CanvasScaler>());

            hudPanel = CreateUiObject("SelectionSideHud", canvasObject.transform).GetComponent<RectTransform>();
            hudPanel.anchorMin = new Vector2(1f, 0f);
            hudPanel.anchorMax = new Vector2(1f, 1f);
            hudPanel.pivot = new Vector2(1f, 0.5f);
            hudPanel.sizeDelta = new Vector2(HudWidth, -StrategyHudStyle.TopRailHeight);
            hudPanel.anchoredPosition = new Vector2(HudWidth, -StrategyHudStyle.TopRailHeight * 0.5f);

            Image background = hudPanel.gameObject.AddComponent<Image>();
            StrategyHudStyle.StylePanel(background, new Color(
                StrategyHudStyle.Background.r,
                StrategyHudStyle.Background.g,
                StrategyHudStyle.Background.b,
                0.98f), true);
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
            accentImage.color = StrategyHudStyle.Primary;
            accentImage.raycastTarget = false;

            RectTransform previewFrame = CreateUiObject("PreviewFrame", hudPanel).GetComponent<RectTransform>();
            SetTopLeft(previewFrame, 24f, 24f, 70f, 70f);
            Image previewBackground = previewFrame.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(previewBackground, new Color(1f, 1f, 1f, 0.12f));

            RectTransform previewInset = CreateUiObject("PreviewInset", previewFrame).GetComponent<RectTransform>();
            SetOffsets(previewInset, 4f, 4f, 4f, 4f);
            Image previewInsetImage = previewInset.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleInset(previewInsetImage, new Color(0.02f, 0.03f, 0.03f, 0.82f));

            RectTransform previewImageRect = CreateUiObject("PreviewImage", previewInset).GetComponent<RectTransform>();
            SetOffsets(previewImageRect, 3f, 3f, 3f, 3f);
            hudPreviewImage = previewImageRect.gameObject.AddComponent<Image>();
            hudPreviewImage.preserveAspect = true;
            hudPreviewImage.raycastTarget = false;

            hudTitleText = CreateText("Title", hudPanel, 22, TextAnchor.UpperLeft, Color.white);
            hudTitleText.fontStyle = FontStyle.Bold;
            hudTitleText.resizeTextForBestFit = true;
            hudTitleText.resizeTextMinSize = 17;
            hudTitleText.resizeTextMaxSize = 22;
            SetTopStretch(hudTitleText.rectTransform, 104f, 27f, 24f, 34f);
            hudSubtitleText = CreateText("Subtitle", hudPanel, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            hudSubtitleText.fontStyle = FontStyle.Bold;
            hudSubtitleText.resizeTextForBestFit = true;
            hudSubtitleText.resizeTextMinSize = 11;
            hudSubtitleText.resizeTextMaxSize = 13;
            SetTopStretch(hudSubtitleText.rectTransform, 104f, 64f, 24f, 22f);
            RectTransform line = CreateUiObject("Divider", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(line, 24f, 112f, 24f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = StrategyHudStyle.Divider;
            lineImage.raycastTarget = false;

            CreateScrollableContent();

            summaryBackground = CreateSectionBackground("SummaryBackground", hudContent, 128f, 128f);
            hudSummaryTitleText = CreateText("SummaryTitle", hudContent, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            hudSummaryTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudSummaryTitleText.rectTransform, 24f, 140f, 24f, 20f);

            hudBodyText = CreateText("Body", hudContent, 14, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            hudBodyText.lineSpacing = 1.15f;
            SetTopStretch(hudBodyText.rectTransform, 24f, 166f, 24f, 92f);

            residentsRoot = CreateUiObject("HouseResidents", hudContent).GetComponent<RectTransform>();
            SetTopStretch(residentsRoot, 18f, 128f, 18f, 236f);
            Image residentsBackground = residentsRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(residentsBackground, WithAlpha(StrategyHudStyle.Surface, 0.90f));
            residentsRoot.gameObject.SetActive(false);

            Text residentsTitle = CreateText("ResidentsTitle", residentsRoot, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            residentsTitle.fontStyle = FontStyle.Bold;
            residentsTitle.text = "Residents";
            SetTopStretch(residentsTitle.rectTransform, 6f, 10f, 6f, 18f);

            residentsEmptyText = CreateText("ResidentsEmpty", residentsRoot, 13, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            residentsEmptyText.text = "no residents yet";
            SetTopStretch(residentsEmptyText.rectTransform, 6f, 44f, 6f, 24f);

            EnsureResidentRowCount(StrategyPlacedBuilding.MaxHouseResidents);

            workersRoot = CreateUiObject("LumberjackWorkers", hudContent).GetComponent<RectTransform>();
            SetTopStretch(workersRoot, 18f, 128f, 18f, 250f);
            Image workersBackground = workersRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(workersBackground, WithAlpha(StrategyHudStyle.Surface, 0.90f));
            workersRoot.gameObject.SetActive(false);

            Text workersTitle = CreateText("WorkersTitle", workersRoot, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            workersTitle.fontStyle = FontStyle.Bold;
            workersTitle.text = "Workers";
            SetTopStretch(workersTitle.rectTransform, 6f, 10f, 6f, 18f);

            workersEmptyText = CreateText("WorkersEmpty", workersRoot, 12, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            workersEmptyText.text = "assign residents";
            SetTopStretch(workersEmptyText.rectTransform, 6f, 220f, 6f, 22f);

            for (int i = 0; i < workerRows.Length; i++)
            {
                CreateWorkerRow(i);
            }

            statusBackground = CreateSectionBackground("StatusBackground", hudContent, 272f, 76f);
            hudStatusTitleText = CreateText("StatusTitle", hudContent, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            hudStatusTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudStatusTitleText.rectTransform, 24f, 284f, 24f, 20f);

            hudStatusBodyText = CreateText("StatusBody", hudContent, 14, TextAnchor.UpperLeft, StrategyHudStyle.TextPrimary);
            hudStatusBodyText.lineSpacing = 1.12f;
            SetTopStretch(hudStatusBodyText.rectTransform, 24f, 310f, 24f, 28f);

            contextBackground = CreateSectionBackground("ContextBackground", hudContent, 366f, 118f);
            hudContextTitleText = CreateText("ContextTitle", hudContent, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            hudContextTitleText.fontStyle = FontStyle.Bold;
            SetTopStretch(hudContextTitleText.rectTransform, 24f, 378f, 24f, 20f);

            hudContextBodyText = CreateText("ContextBody", hudContent, 13, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            hudContextBodyText.lineSpacing = 1.1f;
            SetTopStretch(hudContextBodyText.rectTransform, 24f, 404f, 24f, 70f);

            CreateResidentHud();
            EnsureBuildingHudRenderer();
            CreateTradingPostHud();
            CreateProductionUpgradeHud();

            resourcesRoot = CreateUiObject("HouseResources", hudContent).GetComponent<RectTransform>();
            SetTopStretch(resourcesRoot, 24f, 382f, 24f, 274f);
            Image resourcesBackground = resourcesRoot.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(resourcesBackground, WithAlpha(StrategyHudStyle.Surface, 0.90f));
            resourcesRoot.gameObject.SetActive(false);

            Text resourcesTitle = CreateText("ResourcesTitle", resourcesRoot, 13, TextAnchor.UpperLeft, StrategyHudStyle.Primary);
            resourcesTitle.fontStyle = FontStyle.Bold;
            resourcesTitle.text = "Dinner";
            SetTopStretch(resourcesTitle.rectTransform, 6f, 8f, 6f, 18f);

            RectTransform foodStatusRow = CreateUiObject("FoodStatusRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodStatusRow, 6f, 32f, 6f, 60f);
            foodStatusRowImage = foodStatusRow.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(foodStatusRowImage, WithAlpha(StrategyHudStyle.Elevated, 0.94f));

            foodStatusText = CreateText("FoodStatusText", foodStatusRow, 11, TextAnchor.MiddleLeft, Color.white);
            foodStatusText.fontStyle = FontStyle.Bold;
            foodStatusText.lineSpacing = 1.05f;
            SetOffsets(foodStatusText.rectTransform, 8f, 0f, 128f, 0f);

            foodMealText = CreateText("FoodMealText", foodStatusRow, 11, TextAnchor.MiddleRight, StrategyHudStyle.TextPrimary);
            foodMealText.fontStyle = FontStyle.Bold;
            foodMealText.lineSpacing = 1.05f;
            foodMealText.resizeTextForBestFit = true;
            foodMealText.resizeTextMinSize = 8;
            foodMealText.resizeTextMaxSize = 11;
            SetOffsets(foodMealText.rectTransform, 182f, 0f, 8f, 0f);

            RectTransform foodMeter = CreateUiObject("FoodMealMeter", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodMeter, 6f, 98f, 6f, 8f);
            Image foodMeterBackground = foodMeter.gameObject.AddComponent<Image>();
            foodMeterBackground.color = WithAlpha(StrategyHudStyle.Background, 0.88f);
            foodMeterBackground.raycastTarget = false;

            foodMealFillRect = CreateUiObject("FoodMealMeterFill", foodMeter).GetComponent<RectTransform>();
            foodMealFillRect.anchorMin = Vector2.zero;
            foodMealFillRect.anchorMax = new Vector2(0f, 1f);
            foodMealFillRect.offsetMin = Vector2.zero;
            foodMealFillRect.offsetMax = Vector2.zero;
            foodMealFillImage = foodMealFillRect.gameObject.AddComponent<Image>();
            foodMealFillImage.color = WithAlpha(StrategyHudStyle.Success, 0.95f);
            foodMealFillImage.raycastTarget = false;

            RectTransform granaryRow = CreateUiObject("GranaryFoodRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(granaryRow, 6f, 114f, 6f, 22f);
            Image granaryBackground = granaryRow.gameObject.AddComponent<Image>();
            StrategyHudStyle.StyleCompactPanel(granaryBackground, WithAlpha(StrategyHudStyle.Surface, 0.72f));

            foodGranaryText = CreateText("GranaryFoodText", granaryRow, 10, TextAnchor.MiddleLeft, StrategyHudStyle.TextMuted);
            foodGranaryText.fontStyle = FontStyle.Bold;
            SetOffsets(foodGranaryText.rectTransform, 8f, 0f, 8f, 0f);

            resourcesEmptyText = CreateText("ResourcesEmptyText", resourcesRoot, 11, TextAnchor.UpperLeft, StrategyHudStyle.TextMuted);
            resourcesEmptyText.text = "No food stored at this house";
            SetTopStretch(resourcesEmptyText.rectTransform, 6f, 142f, 6f, 24f);

            for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length; i++)
            {
                CreateResourceSlot(i);
            }

            upgradeActionsRoot = CreateUiObject("HouseUpgradeActions", hudContent).GetComponent<RectTransform>();
            SetTopStretch(upgradeActionsRoot, 24f, 666f, 24f, 196f);
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

        private void CreateScrollableContent()
        {
            RectTransform viewport = CreateUiObject("ContentViewport", hudPanel).GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -122f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform scrollContent = CreateUiObject("ScrollContent", viewport).GetComponent<RectTransform>();
            scrollContent.anchorMin = new Vector2(0f, 1f);
            scrollContent.anchorMax = new Vector2(1f, 1f);
            scrollContent.pivot = new Vector2(0.5f, 1f);
            scrollContent.anchoredPosition = Vector2.zero;
            scrollContent.sizeDelta = new Vector2(0f, 1050f);
            hudScrollContent = scrollContent;

            hudContent = CreateUiObject("ContextSections", scrollContent).GetComponent<RectTransform>();
            hudContent.anchorMin = new Vector2(0f, 1f);
            hudContent.anchorMax = new Vector2(1f, 1f);
            hudContent.pivot = new Vector2(0.5f, 1f);
            hudContent.anchoredPosition = new Vector2(0f, 122f);
            hudContent.sizeDelta = new Vector2(0f, 1050f);

            hudScrollRect = hudPanel.gameObject.AddComponent<ScrollRect>();
            hudScrollRect.viewport = viewport;
            hudScrollRect.content = scrollContent;
            hudScrollRect.horizontal = false;
            hudScrollRect.vertical = true;
            hudScrollRect.movementType = ScrollRect.MovementType.Clamped;
            hudScrollRect.scrollSensitivity = 36f;
            hudScrollRect.inertia = !StrategyHudStyle.ReducedMotion;
            hudScrollRect.verticalNormalizedPosition = 1f;
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
                        : camp != null && camp.CanHuntDeer ? "hunts rabbits/deer" : "hunts rabbits";
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
