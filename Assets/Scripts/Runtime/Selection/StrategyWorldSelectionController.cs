using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyWorldSelectionController : MonoBehaviour
    {
        private const float HudWidth = 352f;
        private const float HudAnimationSpeed = 9f;
        private const float ResourceCellWidth = 138f;
        private const int StorageWorkerHudSlots = 2;
        private const int StorageBuilderHudSlots = 2;
        private const int MaxWorkerHudSlots = StorageWorkerHudSlots + StorageBuilderHudSlots;

        private Camera strategyCamera;
        private StrategyBuildMenuController buildMenu;
        private StrategyBuildingUpgradeController upgradeController;
        private StrategyBuildPlacementController placementController;
        private StrategyConfirmationDialogController confirmationDialog;
        private StrategyFogOfWarController fog;
        private Sprite markerSprite;
        private SpriteRenderer markerRenderer;
        private RectTransform hudPanel;
        private CanvasGroup hudGroup;
        private Image hudPreviewImage;
        private Text hudTitleText;
        private Text hudSubtitleText;
        private RectTransform summaryBackground;
        private Text hudSummaryTitleText;
        private Text hudBodyText;
        private RectTransform statusBackground;
        private Text hudStatusTitleText;
        private Text hudStatusBodyText;
        private RectTransform contextBackground;
        private Text hudContextTitleText;
        private Text hudContextBodyText;
        private RectTransform residentsRoot;
        private Text residentsEmptyText;
        private readonly List<RectTransform> residentRows = new();
        private readonly List<Image> residentPortraitImages = new();
        private readonly List<Text> residentNameTexts = new();
        private readonly List<Text> residentStatusTexts = new();
        private RectTransform workersRoot;
        private Text workersEmptyText;
        private readonly RectTransform[] workerRows = new RectTransform[MaxWorkerHudSlots];
        private readonly Image[] workerPortraitImages = new Image[MaxWorkerHudSlots];
        private readonly Text[] workerNameTexts = new Text[MaxWorkerHudSlots];
        private readonly Text[] workerStatusTexts = new Text[MaxWorkerHudSlots];
        private readonly Button[] workerButtons = new Button[MaxWorkerHudSlots];
        private readonly Text[] workerActionTexts = new Text[MaxWorkerHudSlots];
        private RectTransform resourcesRoot;
        private Image foodStatusRowImage;
        private Image foodMealFillImage;
        private RectTransform foodMealFillRect;
        private Text foodStatusText;
        private Text foodMealText;
        private Text foodGranaryText;
        private Image cropIconImage;
        private Text cropValueText;
        private Text resourcesEmptyText;
        private readonly RectTransform[] resourceSlots = new RectTransform[StrategyHouseResourceStore.DisplayOrder.Length];
        private readonly Image[] resourceIconImages = new Image[StrategyHouseResourceStore.DisplayOrder.Length];
        private readonly Text[] resourceAmountTexts = new Text[StrategyHouseResourceStore.DisplayOrder.Length];
        private RectTransform upgradeActionsRoot;
        private Button gardenBedsButton;
        private Text gardenBedsButtonText;
        private Text gardenBedsStateText;
        private Text gardenBedsActionText;
        private Button chickenCoopButton;
        private Text chickenCoopButtonText;
        private Text chickenCoopStateText;
        private Text chickenCoopActionText;
        private Text upgradeStatusText;
        private Transform selectedTransform;
        private string upgradeStatusMessage = string.Empty;
        private float hudT;

        public void Configure(Camera camera, StrategyBuildMenuController menu, StrategyBuildingUpgradeController upgrades)
        {
            Configure(camera, menu, upgrades, null, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController)
        {
            Configure(camera, menu, upgrades, fogController, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyForestryController forestryController)
        {
            Configure(camera, menu, upgrades, fogController, populationController, forestryController, null, null);
        }

        public void Configure(
            Camera camera,
            StrategyBuildMenuController menu,
            StrategyBuildingUpgradeController upgrades,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyForestryController forestryController,
            StrategyBuildPlacementController placement,
            StrategyConfirmationDialogController confirmation)
        {
            strategyCamera = camera;
            buildMenu = menu;
            upgradeController = upgrades;
            placementController = placement;
            confirmationDialog = confirmation;
            fog = fogController;
            EnsureMarker();
            EnsureHud();
        }

        private void Update()
        {
            if (strategyCamera == null)
            {
                return;
            }

            HandleDeleteInput();
            HandleSelectionInput();
            UpdateHudAnimation();
        }

        private void HandleDeleteInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null
                || !keyboard.deleteKey.wasPressedThisFrame
                || selectedTransform == null
                || placementController == null
                || confirmationDialog == null
                || confirmationDialog.IsOpen)
            {
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                RequestConstructionCancel(constructionSite);
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                RequestBuildingDemolition(building);
            }
        }

        private void RequestConstructionCancel(StrategyConstructionSite site)
        {
            if (site == null)
            {
                return;
            }

            string body = "Cancel construction of "
                + GetBuildingTitle(site.Tool)
                + "?\nDelivered Logs and Stone will be left on the ground for storage workers or other builders.";
            confirmationDialog.Show(
                "Cancel Construction",
                body,
                "Cancel Construction",
                "Keep",
                () =>
                {
                    if (placementController != null && placementController.CancelConstructionSite(site))
                    {
                        ClearSelection();
                    }
                });
        }

        private void RequestBuildingDemolition(StrategyPlacedBuilding building)
        {
            if (building == null)
            {
                return;
            }

            string body = "Demolish "
                + GetBuildingTitle(building.Tool)
                + "?\nThe building will be removed from the settlement.";
            if (building.Tool == StrategyBuildTool.House && building.ResidentCount > 0)
            {
                body += "\nResidents living here will become homeless.";
            }

            confirmationDialog.Show(
                "Demolish Building",
                body,
                "Demolish",
                "Keep",
                () =>
                {
                    if (placementController != null && placementController.DemolishBuilding(building))
                    {
                        ClearSelection();
                    }
                });
        }

        private void HandleSelectionInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || IsPointerOverUi())
            {
                return;
            }

            if (buildMenu != null && buildMenu.LastPlacementFrame == Time.frameCount)
            {
                return;
            }

            if (buildMenu != null && buildMenu.ActiveTool != StrategyBuildTool.None)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = strategyCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(strategyCamera.transform.position.z)));
            if (fog != null && !fog.IsWorldExplored(world))
            {
                ClearSelection();
                return;
            }

            Physics2D.SyncTransforms();

            Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));
            SelectBestHit(hits);
        }

        private void SelectBestHit(Collider2D[] hits)
        {
            StrategyResidentAgent bestResident = null;
            StrategyPlacedBuilding bestBuilding = null;
            StrategyConstructionSite bestConstructionSite = null;

            for (int i = 0; i < hits.Length; i++)
            {
                StrategyResidentAgent resident = hits[i].GetComponentInParent<StrategyResidentAgent>();
                if (resident != null)
                {
                    bestResident = resident;
                    break;
                }

                StrategyPlacedBuilding building = hits[i].GetComponentInParent<StrategyPlacedBuilding>();
                if (building != null)
                {
                    bestBuilding = building;
                }

                StrategyConstructionSite constructionSite = hits[i].GetComponentInParent<StrategyConstructionSite>();
                if (constructionSite != null)
                {
                    bestConstructionSite = constructionSite;
                }
            }

            if (bestResident != null)
            {
                Select(bestResident.transform, bestResident.SelectionBounds);
                return;
            }

            if (bestBuilding != null)
            {
                Select(bestBuilding.transform, bestBuilding.SelectionBounds);
                return;
            }

            if (bestConstructionSite != null)
            {
                Select(bestConstructionSite.transform, bestConstructionSite.SelectionBounds);
                return;
            }

            ClearSelection();
        }

        private void Select(Transform target, Bounds bounds)
        {
            bool changedSelection = selectedTransform != target;
            selectedTransform = target;
            EnsureMarker();
            if (changedSelection)
            {
                upgradeStatusMessage = string.Empty;
                StrategyDebugLogger.Info(
                    "Selection",
                    "Selected",
                    StrategyDebugLogger.F("target", DescribeSelection(target)));
            }

            RefreshHud();
            UpdateSelectionMarker(bounds);
        }

        private void UpdateSelectionMarker(Bounds bounds)
        {
            EnsureMarker();
            markerRenderer.gameObject.SetActive(true);
            markerRenderer.transform.position = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.08f, -0.05f);
            markerRenderer.transform.localScale = new Vector3(
                Mathf.Max(0.45f, bounds.size.x + 0.22f),
                Mathf.Max(0.18f, Mathf.Min(bounds.size.y * 0.25f, 0.65f)),
                1f);
            StrategyWorldSorting.Apply(markerRenderer, markerRenderer.transform.position, -3);
        }

        private void ClearSelection()
        {
            if (selectedTransform != null)
            {
                StrategyDebugLogger.Info(
                    "Selection",
                    "Cleared",
                    StrategyDebugLogger.F("target", DescribeSelection(selectedTransform)));
            }

            selectedTransform = null;
            upgradeStatusMessage = string.Empty;
            if (markerRenderer != null)
            {
                markerRenderer.gameObject.SetActive(false);
            }

            RefreshHud();
        }

        public void ClearSelectionIfTarget(Component target)
        {
            if (target != null && selectedTransform == target.transform)
            {
                ClearSelection();
            }
        }

        private void LateUpdate()
        {
            if (selectedTransform == null || markerRenderer == null || !markerRenderer.gameObject.activeSelf)
            {
                return;
            }

            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                UpdateSelectionMarker(resident.SelectionBounds);
                RefreshHud();
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                UpdateSelectionMarker(building.SelectionBounds);
                RefreshHud();
                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                UpdateSelectionMarker(constructionSite.SelectionBounds);
                RefreshHud();
            }
        }

        private void UpdateHudAnimation()
        {
            if (hudPanel == null || hudGroup == null)
            {
                return;
            }

            float target = selectedTransform != null ? 1f : 0f;
            hudT = Mathf.MoveTowards(hudT, target, Time.unscaledDeltaTime * HudAnimationSpeed);
            float eased = Smooth01(hudT);
            hudPanel.anchoredPosition = new Vector2(Mathf.Lerp(HudWidth, 0f, eased), 0f);
            hudGroup.alpha = eased;
            hudGroup.blocksRaycasts = eased > 0.9f;
            hudGroup.interactable = eased > 0.9f;
        }

        private void RefreshHud()
        {
            EnsureHud();

            if (selectedTransform == null)
            {
                hudTitleText.text = string.Empty;
                hudSubtitleText.text = string.Empty;
                hudSummaryTitleText.text = string.Empty;
                hudBodyText.text = string.Empty;
                hudStatusTitleText.text = string.Empty;
                hudStatusBodyText.text = string.Empty;
                hudContextTitleText.text = string.Empty;
                hudContextBodyText.text = string.Empty;
                SetPreviewSprite(null);
                SetProfileSectionVisible(false);
                SetStatusSectionVisible(false);
                SetContextSectionVisible(false);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
                return;
            }

            LayoutContextSection(366f, 118f);
            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                hudTitleText.text = resident.FullName;
                hudSubtitleText.text = string.Empty;
                hudSummaryTitleText.text = "Profile";
                hudBodyText.text = "Gender: "
                    + GetResidentGenderTitle(resident.Gender)
                    + "\n"
                    + "Role: "
                    + (!resident.IsAdult
                        ? "child"
                        : resident.IsHouseholder
                        ? "householder"
                        : resident.BuilderWorkplace != null || resident.ConstructionSite != null
                        ? "builder"
                        : resident.Workplace != null
                        ? "lumberjack"
                        : resident.StoneWorkplace != null
                            ? "stonecutter"
                            : resident.HunterWorkplace != null
                                ? "hunter"
                                : resident.FisherWorkplace != null
                                    ? "fisher"
                                : resident.StorageWorkplace != null
                                ? "storekeeper"
                                : resident.GranaryWorkplace != null
                                ? "granary worker"
                                : "settler")
                    + "\n"
                    + "Age: "
                    + resident.DisplayAgeYears
                    + " years"
                    + "\n"
                    + "Stage: "
                    + GetResidentLifeStageTitle(resident);
                hudStatusTitleText.text = "Status";
                hudStatusBodyText.text = GetResidentStatus(resident);
                hudContextTitleText.text = "House";
                hudContextBodyText.text = resident.Home != null
                    ? GetBuildingTitle(resident.Home.Tool)
                        + "\n"
                        + "assigned to this home"
                    : "Camp"
                    + "\n"
                    + "waiting for a home";
                SetPreviewSprite(StrategyResidentSpriteFactory.GetPortraitSprite(
                    resident.Gender,
                    resident.VisualVariant,
                    resident.LifeStage));
                SetProfileSectionVisible(true);
                SetStatusSectionVisible(true);
                SetContextSectionVisible(true);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
                return;
            }

            StrategyPlacedBuilding building = selectedTransform.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                hudTitleText.text = GetBuildingTitle(building.Tool);
                hudSubtitleText.text = "Building";
                SetBuildingPreviewSprite(building);
                SetProfileSectionVisible(false);
                SetStatusSectionVisible(false);
                SetContextSectionVisible(false);

                bool isHouse = building.Tool == StrategyBuildTool.House;
                StrategyLumberjackCamp camp = building.GetComponent<StrategyLumberjackCamp>();
                StrategyStonecutterCamp stoneCamp = building.GetComponent<StrategyStonecutterCamp>();
                StrategyHunterCamp hunterCamp = building.GetComponent<StrategyHunterCamp>();
                StrategyFisherHut fisherHut = building.GetComponent<StrategyFisherHut>();
                StrategyStorageYard yard = building.GetComponent<StrategyStorageYard>();
                StrategyGranary granary = building.GetComponent<StrategyGranary>();
                bool isLumberjackCamp = camp != null;
                bool isStonecutterCamp = stoneCamp != null;
                bool isHunterCamp = hunterCamp != null;
                bool isFisherHut = fisherHut != null;
                bool isStorageYard = yard != null;
                bool isGranary = granary != null;
                SetResidentsSectionVisible(isHouse);
                if (isHouse)
                {
                    RefreshResidents(building);
                }

                SetWorkersSectionVisible(false);
                if (isLumberjackCamp || isStonecutterCamp || isHunterCamp || isFisherHut || isStorageYard || isGranary)
                {
                    LayoutContextSection(128f, 214f);
                }

                if (isLumberjackCamp)
                {
                    hudContextTitleText.text = "Forest and Stock";
                    hudContextBodyText.text = camp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isStonecutterCamp)
                {
                    hudContextTitleText.text = "Stone and Stock";
                    hudContextBodyText.text = stoneCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isHunterCamp)
                {
                    hudContextTitleText.text = "Hunting and Stock";
                    hudContextBodyText.text = hunterCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isFisherHut)
                {
                    hudContextTitleText.text = "Fishing and Stock";
                    hudContextBodyText.text = fisherHut.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isStorageYard)
                {
                    hudContextTitleText.text = "Storage Yard";
                    hudContextBodyText.text = yard.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isGranary)
                {
                    hudContextTitleText.text = "Food and Stock";
                    hudContextBodyText.text = granary.GetHudStatusText();
                    SetContextSectionVisible(true);
                }

                SetResourcesVisible(isHouse);
                if (isHouse)
                {
                    RefreshResources(building);
                }

                SetUpgradeActionsVisible(isHouse);
                if (isHouse)
                {
                    RefreshUpgradeActions(building);
                }

                return;
            }

            StrategyConstructionSite constructionSite = selectedTransform.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                hudTitleText.text = "Construction";
                hudSubtitleText.text = constructionSite.Title;
                int stage = constructionSite.ResourcesComplete
                    ? Mathf.Clamp(1 + Mathf.FloorToInt(constructionSite.Progress * (StrategyConstructionSpriteFactory.StageCount - 1)), 1, StrategyConstructionSpriteFactory.StageCount - 1)
                    : 0;
                SetPreviewSprite(constructionSite.Tool == StrategyBuildTool.Bridge
                    ? StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(constructionSite.Footprint, stage)
                    : StrategyConstructionSpriteFactory.GetConstructionSprite(constructionSite.Tool, constructionSite.VisualVariant, stage));
                hudSummaryTitleText.text = "Plan";
                hudBodyText.text = GetBuildingTitle(constructionSite.Tool)
                    + "\n"
                    + "Logs: "
                    + constructionSite.Cost.Logs
                    + "\n"
                    + "Stone: "
                    + constructionSite.Cost.Stone;
                hudStatusTitleText.text = "Construction Progress";
                hudStatusBodyText.text = constructionSite.GetHudStatusText();
                hudContextTitleText.text = "Builders";
                hudContextBodyText.text = GetConstructionBuildersText(constructionSite);
                SetProfileSectionVisible(true);
                SetStatusSectionVisible(true);
                SetContextSectionVisible(true);
                SetResidentsSectionVisible(false);
                SetWorkersSectionVisible(false);
                SetResourcesVisible(false);
                SetUpgradeActionsVisible(false);
            }
        }

        private void SetBuildingPreviewSprite(StrategyPlacedBuilding building)
        {
            if (building != null && building.Tool == StrategyBuildTool.Bridge)
            {
                SetPreviewSprite(StrategyBuildingSpriteFactory.GetBridgeSprite(building.Footprint));
                return;
            }

            if (building != null && StrategyBuildingSpriteFactory.TryGetBuildSprite(building.Tool, building.VisualVariant, out Sprite sprite))
            {
                SetPreviewSprite(sprite);
                return;
            }

            SetPreviewSprite(null);
        }

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

            resourcesRoot = CreateUiObject("HouseResources", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(resourcesRoot, 24f, 382f, 24f, 196f);
            Image resourcesBackground = resourcesRoot.gameObject.AddComponent<Image>();
            resourcesBackground.color = new Color(0.08f, 0.11f, 0.10f, 0.86f);
            resourcesBackground.raycastTarget = false;
            resourcesRoot.gameObject.SetActive(false);

            Text resourcesTitle = CreateText("ResourcesTitle", resourcesRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            resourcesTitle.fontStyle = FontStyle.Bold;
            resourcesTitle.text = "Resources";
            SetTopStretch(resourcesTitle.rectTransform, 6f, 8f, 6f, 18f);

            RectTransform foodStatusRow = CreateUiObject("FoodStatusRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodStatusRow, 6f, 32f, 6f, 32f);
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
            SetOffsets(foodStatusText.rectTransform, 36f, 0f, 104f, 0f);

            foodMealText = CreateText("FoodMealText", foodStatusRow, 11, TextAnchor.MiddleRight, new Color(0.88f, 0.93f, 0.90f));
            foodMealText.fontStyle = FontStyle.Bold;
            SetOffsets(foodMealText.rectTransform, 176f, 0f, 8f, 0f);

            RectTransform foodMeter = CreateUiObject("FoodMealMeter", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(foodMeter, 6f, 70f, 6f, 8f);
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
            SetTopStretch(granaryRow, 6f, 86f, 6f, 24f);
            Image granaryBackground = granaryRow.gameObject.AddComponent<Image>();
            granaryBackground.color = new Color(1f, 1f, 1f, 0.035f);
            granaryBackground.raycastTarget = false;

            RectTransform granaryIconRect = CreateUiObject("GranaryIcon", granaryRow).GetComponent<RectTransform>();
            SetTopLeft(granaryIconRect, 8f, 4f, 16f, 16f);
            Image granaryIcon = granaryIconRect.gameObject.AddComponent<Image>();
            granaryIcon.sprite = StrategyResourceIconFactory.GetSprite(StrategyResourceType.Fish);
            granaryIcon.preserveAspect = true;
            granaryIcon.color = new Color(0.82f, 0.90f, 0.87f, 0.88f);
            granaryIcon.raycastTarget = false;

            foodGranaryText = CreateText("GranaryFoodText", granaryRow, 11, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            foodGranaryText.fontStyle = FontStyle.Bold;
            SetOffsets(foodGranaryText.rectTransform, 34f, 0f, 8f, 0f);

            RectTransform cropRow = CreateUiObject("CropRow", resourcesRoot).GetComponent<RectTransform>();
            SetTopStretch(cropRow, 6f, 116f, 6f, 24f);
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
            resourcesEmptyText.text = "No stored household resources";
            SetTopStretch(resourcesEmptyText.rectTransform, 6f, 148f, 6f, 24f);

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
                        + " years";
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

        private void RefreshWorkers(StrategyFisherHut hut)
        {
            int workerCount = hut != null ? hut.WorkerCount : 0;
            bool canAssign = hut != null && hut.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? "assign fishers"
                    : "no free residents";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyFisherHut.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = hut != null && hut.TryGetWorker(i, out worker);

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
                        : "Fisher: open";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "catches fish near water";
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

        private void RefreshWorkers(StrategyGranary granary)
        {
            int workerCount = granary != null ? granary.WorkerCount : 0;
            bool canAssign = granary != null && granary.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? "assign granary workers"
                    : "no free residents";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                bool slotVisible = i < StrategyGranary.MaxWorkers;
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(slotVisible);
                }

                if (!slotVisible)
                {
                    continue;
                }

                StrategyResidentAgent worker = null;
                bool hasWorker = granary != null && granary.TryGetWorker(i, out worker);

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
                        : "Granary Worker: open";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "hauls food to the granary";
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

        private void RefreshWorkers(StrategyStorageYard yard)
        {
            int workerCount = yard != null ? yard.WorkerCount : 0;
            int builderCount = yard != null ? yard.BuilderCount : 0;
            bool canAssignWorker = yard != null && yard.CanAssignNextAvailableWorker();
            bool canAssignBuilder = yard != null && yard.CanAssignNextAvailableBuilder();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount + builderCount <= 0);
                workersEmptyText.text = canAssignWorker || canAssignBuilder
                    ? "hire storekeepers and builders"
                    : "no free residents";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                StrategyResidentAgent worker = null;
                bool isBuilderSlot = i >= StorageWorkerHudSlots;
                int staffIndex = isBuilderSlot ? i - StorageWorkerHudSlots : i;
                bool hasWorker = yard != null
                    && (isBuilderSlot
                        ? yard.TryGetBuilder(staffIndex, out worker)
                        : yard.TryGetWorker(staffIndex, out worker));
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(true);
                }

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
                        : isBuilderSlot
                            ? "Builder: open"
                            : "Storekeeper: open";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : isBuilderSlot
                            ? "builds structures"
                            : "hauls resources";
                }

                bool buttonEnabled = hasWorker
                    || (isBuilderSlot
                        ? staffIndex == builderCount && canAssignBuilder
                        : staffIndex == workerCount && canAssignWorker);
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

        private void RefreshWorkers(StrategyStonecutterCamp camp)
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
                bool slotVisible = i < StrategyStonecutterCamp.MaxWorkers;
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

        private void SetProfileSectionVisible(bool visible)
        {
            if (summaryBackground != null)
            {
                summaryBackground.gameObject.SetActive(visible);
            }

            if (hudSummaryTitleText != null)
            {
                hudSummaryTitleText.gameObject.SetActive(visible);
            }

            if (hudBodyText != null)
            {
                hudBodyText.gameObject.SetActive(visible);
            }
        }

        private void SetStatusSectionVisible(bool visible)
        {
            if (statusBackground != null)
            {
                statusBackground.gameObject.SetActive(visible);
            }

            if (hudStatusTitleText != null)
            {
                hudStatusTitleText.gameObject.SetActive(visible);
            }

            if (hudStatusBodyText != null)
            {
                hudStatusBodyText.gameObject.SetActive(visible);
            }
        }

        private void SetContextSectionVisible(bool visible)
        {
            if (contextBackground != null)
            {
                contextBackground.gameObject.SetActive(visible);
            }

            if (hudContextTitleText != null)
            {
                hudContextTitleText.gameObject.SetActive(visible);
            }

            if (hudContextBodyText != null)
            {
                hudContextBodyText.gameObject.SetActive(visible);
            }
        }

        private void LayoutContextSection(float top, float height)
        {
            if (contextBackground != null)
            {
                SetTopStretch(contextBackground, 18f, top, 18f, height);
            }

            if (hudContextTitleText != null)
            {
                SetTopStretch(hudContextTitleText.rectTransform, 24f, top + 12f, 24f, 20f);
            }

            if (hudContextBodyText != null)
            {
                SetTopStretch(hudContextBodyText.rectTransform, 24f, top + 38f, 24f, Mathf.Max(28f, height - 50f));
            }
        }

        private void SetResidentsSectionVisible(bool visible)
        {
            if (residentsRoot != null)
            {
                residentsRoot.gameObject.SetActive(visible);
            }
        }

        private void SetWorkersSectionVisible(bool visible)
        {
            if (workersRoot != null)
            {
                workersRoot.gameObject.SetActive(visible);
            }
        }

        private void ToggleWorkerSlot(int index)
        {
            StrategyLumberjackCamp camp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyLumberjackCamp>()
                : null;
            if (camp != null)
            {
                ToggleLumberjackWorkerSlot(camp, index);
                return;
            }

            StrategyStonecutterCamp stoneCamp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyStonecutterCamp>()
                : null;
            if (stoneCamp != null)
            {
                ToggleStonecutterWorkerSlot(stoneCamp, index);
                return;
            }

            StrategyHunterCamp hunterCamp = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyHunterCamp>()
                : null;
            if (hunterCamp != null)
            {
                ToggleHunterWorkerSlot(hunterCamp, index);
                return;
            }

            StrategyFisherHut fisherHut = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyFisherHut>()
                : null;
            if (fisherHut != null)
            {
                ToggleFisherWorkerSlot(fisherHut, index);
                return;
            }

            StrategyGranary granary = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyGranary>()
                : null;
            if (granary != null)
            {
                ToggleGranaryWorkerSlot(granary, index);
                return;
            }

            StrategyStorageYard yard = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyStorageYard>()
                : null;
            if (yard != null)
            {
                ToggleStorageWorkerSlot(yard, index);
            }
        }

        private void ToggleLumberjackWorkerSlot(StrategyLumberjackCamp camp, int index)
        {
            if (camp == null)
            {
                return;
            }

            if (index < camp.WorkerCount)
            {
                camp.TryGetWorker(index, out StrategyResidentAgent worker);
                camp.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin));
            }
            else
            {
                bool assigned = camp.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin));
            }

            RefreshHud();
        }

        private void ToggleHunterWorkerSlot(StrategyHunterCamp camp, int index)
        {
            if (camp == null)
            {
                return;
            }

            if (index < camp.WorkerCount)
            {
                camp.TryGetWorker(index, out StrategyResidentAgent worker);
                camp.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "hunter"));
            }
            else
            {
                bool assigned = camp.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "hunter"));
            }

            RefreshHud();
        }

        private void ToggleFisherWorkerSlot(StrategyFisherHut hut, int index)
        {
            if (hut == null)
            {
                return;
            }

            if (index < hut.WorkerCount)
            {
                hut.TryGetWorker(index, out StrategyResidentAgent worker);
                hut.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("hutOrigin", hut.Origin),
                    StrategyDebugLogger.F("profession", "fisher"));
            }
            else
            {
                bool assigned = hut.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("hutOrigin", hut.Origin),
                    StrategyDebugLogger.F("profession", "fisher"));
            }

            RefreshHud();
        }

        private void ToggleGranaryWorkerSlot(StrategyGranary granary, int index)
        {
            if (granary == null)
            {
                return;
            }

            if (index < granary.WorkerCount)
            {
                granary.TryGetWorker(index, out StrategyResidentAgent worker);
                granary.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                    StrategyDebugLogger.F("profession", "granary"));
            }
            else
            {
                bool assigned = granary.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("granaryOrigin", granary.Origin),
                    StrategyDebugLogger.F("profession", "granary"));
            }

            RefreshHud();
        }

        private void ToggleStonecutterWorkerSlot(StrategyStonecutterCamp camp, int index)
        {
            if (camp == null)
            {
                return;
            }

            if (index < camp.WorkerCount)
            {
                camp.TryGetWorker(index, out StrategyResidentAgent worker);
                camp.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "stonecutter"));
            }
            else
            {
                bool assigned = camp.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("campOrigin", camp.Origin),
                    StrategyDebugLogger.F("profession", "stonecutter"));
            }

            RefreshHud();
        }

        private void ToggleStorageWorkerSlot(StrategyStorageYard yard, int index)
        {
            if (yard == null)
            {
                return;
            }

            if (index >= StorageWorkerHudSlots)
            {
                int builderIndex = index - StorageWorkerHudSlots;
                if (builderIndex < yard.BuilderCount)
                {
                    yard.TryGetBuilder(builderIndex, out StrategyResidentAgent builder);
                    yard.UnassignBuilderAt(builderIndex);
                    StrategyDebugLogger.Info(
                        "Selection",
                        "WorkerSlotClicked",
                        StrategyDebugLogger.F("action", "unassign"),
                        StrategyDebugLogger.F("slot", index),
                        StrategyDebugLogger.F("worker", builder != null ? builder.FullName : string.Empty),
                        StrategyDebugLogger.F("yardOrigin", yard.Origin),
                        StrategyDebugLogger.F("profession", "builder"));
                }
                else
                {
                    bool assigned = yard.TryAssignNextAvailableBuilder(out StrategyResidentAgent builder);
                    StrategyDebugLogger.Info(
                        "Selection",
                        "WorkerSlotClicked",
                        StrategyDebugLogger.F("action", "assign"),
                        StrategyDebugLogger.F("slot", index),
                        StrategyDebugLogger.F("success", assigned),
                        StrategyDebugLogger.F("worker", builder != null ? builder.FullName : string.Empty),
                        StrategyDebugLogger.F("yardOrigin", yard.Origin),
                        StrategyDebugLogger.F("profession", "builder"));
                }

                RefreshHud();
                return;
            }

            if (index < yard.WorkerCount)
            {
                yard.TryGetWorker(index, out StrategyResidentAgent worker);
                yard.UnassignWorkerAt(index);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "unassign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
            }
            else
            {
                bool assigned = yard.TryAssignNextAvailableWorker(out StrategyResidentAgent worker);
                StrategyDebugLogger.Info(
                    "Selection",
                    "WorkerSlotClicked",
                    StrategyDebugLogger.F("action", "assign"),
                    StrategyDebugLogger.F("slot", index),
                    StrategyDebugLogger.F("success", assigned),
                    StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty),
                    StrategyDebugLogger.F("yardOrigin", yard.Origin));
            }

            RefreshHud();
        }

        private void TryInstallSelectedUpgrade(StrategyBuildingUpgradeType type)
        {
            StrategyPlacedBuilding building = selectedTransform != null
                ? selectedTransform.GetComponent<StrategyPlacedBuilding>()
                : null;
            if (building == null || building.Tool != StrategyBuildTool.House)
            {
                return;
            }

            if (building.HasUpgrade(type))
            {
                upgradeStatusMessage = "Already installed.";
                RefreshHud();
                return;
            }

            if (upgradeController == null)
            {
                upgradeStatusMessage = "Upgrade system is not ready.";
                RefreshHud();
                return;
            }

            if (upgradeController.TryInstallUpgrade(building, type, out _, out StrategyBuildingUpgradeInstallFailureReason failureReason))
            {
                upgradeStatusMessage = GetUpgradeTitle(type)
                    + " "
                    + "installed near the house.";
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.NotEnoughResources)
            {
                upgradeStatusMessage = "Not enough resources: "
                    + FormatUpgradeCost(StrategyBuildingUpgradeController.GetUpgradeCost(type))
                    + ".";
            }
            else if (failureReason == StrategyBuildingUpgradeInstallFailureReason.AlreadyInstalled)
            {
                upgradeStatusMessage = "Already installed.";
            }
            else
            {
                upgradeStatusMessage = "No free space near the house.";
            }

            RefreshHud();
        }

        private void RefreshUpgradeActions(StrategyPlacedBuilding building)
        {
            if (building == null || gardenBedsButton == null || chickenCoopButton == null)
            {
                return;
            }

            RefreshUpgradeButton(
                gardenBedsButton,
                gardenBedsButtonText,
                gardenBedsStateText,
                gardenBedsActionText,
                building,
                StrategyBuildingUpgradeType.GardenBeds,
                "Garden Beds");
            RefreshUpgradeButton(
                chickenCoopButton,
                chickenCoopButtonText,
                chickenCoopStateText,
                chickenCoopActionText,
                building,
                StrategyBuildingUpgradeType.ChickenCoop,
                "Chicken Coop");

            if (upgradeStatusText != null)
            {
                upgradeStatusText.text = upgradeStatusMessage;
            }
        }

        private void RefreshUpgradeButton(
            Button button,
            Text titleText,
            Text stateText,
            Text actionText,
            StrategyPlacedBuilding building,
            StrategyBuildingUpgradeType type,
            string title)
        {
            bool installed = building.HasUpgrade(type);
            StrategyConstructionResourceCost cost = StrategyBuildingUpgradeController.GetUpgradeCost(type);
            bool canAfford = cost.CanAfford(StrategyStorageYard.GetTotalConstructionResources());
            button.interactable = !installed && upgradeController != null && canAfford;
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (stateText != null)
            {
                stateText.text = installed
                    ? "installed"
                    : upgradeController != null
                        ? canAfford
                            ? "Cost: " + FormatUpgradeCost(cost)
                            : "Missing: " + FormatUpgradeCost(cost)
                        : "not ready";
                stateText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : canAfford
                        ? new Color(0.76f, 0.83f, 0.80f)
                        : new Color(0.95f, 0.58f, 0.45f);
            }

            if (actionText != null)
            {
                actionText.text = installed
                    ? "Done"
                    : canAfford
                        ? "Add"
                        : "No";
                actionText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : canAfford
                        ? Color.white
                        : new Color(0.65f, 0.69f, 0.67f);
            }
        }

        private static string FormatUpgradeCost(StrategyConstructionResourceCost cost)
        {
            return "Logs "
                + cost.Logs
                + " / "
                + "Stone "
                + cost.Stone;
        }

        private void SetUpgradeActionsVisible(bool visible)
        {
            if (upgradeActionsRoot != null)
            {
                upgradeActionsRoot.gameObject.SetActive(visible);
            }

            if (!visible && upgradeStatusText != null)
            {
                upgradeStatusText.text = string.Empty;
            }
        }

        private void RefreshResources(StrategyPlacedBuilding building)
        {
            if (building == null || resourcesRoot == null)
            {
                return;
            }

            RefreshHouseFoodRows(building);
            RefreshHouseCropRow(building);

            StrategyHouseResourceStore store = building.Resources;
            int visibleResourceIndex = 0;
            for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length; i++)
            {
                StrategyResourceType type = StrategyHouseResourceStore.DisplayOrder[i];
                int amount = store != null ? store.GetAmount(type) : 0;
                bool isVisible = amount > 1;

                if (resourceSlots[i] != null)
                {
                    resourceSlots[i].gameObject.SetActive(isVisible);
                    if (isVisible)
                    {
                        int column = visibleResourceIndex % 2;
                        int row = visibleResourceIndex / 2;
                        resourceSlots[i].anchoredPosition = new Vector2(column * ResourceCellWidth, -164f - row * 34f);
                        visibleResourceIndex++;
                    }
                }

                if (!isVisible)
                {
                    continue;
                }

                if (resourceIconImages[i] != null)
                {
                    resourceIconImages[i].sprite = StrategyResourceIconFactory.GetSprite(type);
                    resourceIconImages[i].color = Color.white;
                }

                if (resourceAmountTexts[i] != null)
                {
                    resourceAmountTexts[i].text = GetResourceTitle(type) + ": " + amount;
                    resourceAmountTexts[i].color = new Color(0.88f, 0.93f, 0.90f);
                }
            }

            if (resourcesEmptyText != null)
            {
                resourcesEmptyText.gameObject.SetActive(visibleResourceIndex <= 0);
            }
        }

        private void SetResourcesVisible(bool visible)
        {
            if (resourcesRoot != null)
            {
                resourcesRoot.gameObject.SetActive(visible);
            }
        }

        private void RefreshHouseFoodRows(StrategyPlacedBuilding building)
        {
            StrategyHouseholdFoodState food = building != null
                ? building.GetComponent<StrategyHouseholdFoodState>()
                : null;
            int homeFood = building != null && building.Resources != null
                ? building.Resources.GetTotalFoodAmount()
                : 0;
            int granaryFood = StrategyGranary.GetTotalSettlementFood();
            if (food == null)
            {
                ApplyFoodStatus(
                    "Food status",
                    "No household data",
                    "Meal -",
                    FormatFoodStockLine(homeFood, granaryFood),
                    0f,
                    new Color(0.30f, 0.34f, 0.34f, 0.94f),
                    new Color(0.52f, 0.58f, 0.56f, 0.95f));
                return;
            }

            int requiredFood = food.LastRequiredFood;
            int consumedFood = food.LastConsumedFood;
            float mealFill = requiredFood <= 0 ? 1f : Mathf.Clamp01(consumedFood / (float)requiredFood);
            string mealText = requiredFood <= 0
                ? "Meal -"
                : "Meal " + consumedFood + "/" + requiredFood;

            string statusText;
            string detailText;
            Color rowColor;
            Color fillColor;
            switch (food.Status)
            {
                case StrategyHouseholdFoodStatus.Settling:
                    statusText = "Settling";
                    detailText = "Next meal soon";
                    rowColor = new Color(0.16f, 0.28f, 0.31f, 0.94f);
                    fillColor = new Color(0.46f, 0.67f, 0.74f, 0.95f);
                    mealText = Mathf.CeilToInt(food.FoodGraceSecondsRemaining) + "s";
                    mealFill = 1f - Mathf.Clamp01(food.FoodGraceSecondsRemaining / Mathf.Max(1f, food.FoodGraceDurationSeconds));
                    break;
                case StrategyHouseholdFoodStatus.WaitingForFood:
                    statusText = "No food supply";
                    detailText = "Home and granary empty";
                    rowColor = new Color(0.32f, 0.26f, 0.16f, 0.94f);
                    fillColor = new Color(0.77f, 0.60f, 0.31f, 0.95f);
                    break;
                case StrategyHouseholdFoodStatus.Shortage:
                    statusText = "Food shortage";
                    detailText = "Shortage level " + food.StarvationLevel;
                    rowColor = new Color(0.36f, 0.16f, 0.13f, 0.96f);
                    fillColor = new Color(0.86f, 0.34f, 0.24f, 0.95f);
                    break;
                default:
                    statusText = "Fed";
                    detailText = "Home " + food.LastHouseFoodConsumed
                        + " / Granary " + (food.LastGameConsumed + food.LastFishConsumed);
                    rowColor = new Color(0.15f, 0.30f, 0.22f, 0.94f);
                    fillColor = new Color(0.56f, 0.76f, 0.38f, 0.95f);
                    break;
            }

            ApplyFoodStatus(
                statusText,
                detailText,
                mealText,
                FormatFoodStockLine(homeFood, granaryFood),
                mealFill,
                rowColor,
                fillColor);
        }

        private static string FormatFoodStockLine(int homeFood, int granaryFood)
        {
            return "Home food: " + homeFood + " | Granary: " + granaryFood;
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
                    ? "Crop: " + GetResourceTitle(garden.ProducedResource)
                    : "Crop: None";
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
            string meal,
            string granary,
            float mealFill,
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
                foodMealText.text = meal;
            }

            if (foodGranaryText != null)
            {
                foodGranaryText.text = granary;
            }

            if (foodMealFillRect != null)
            {
                foodMealFillRect.anchorMax = new Vector2(Mathf.Clamp01(mealFill), 1f);
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
            float cellHeight = 32f;

            RectTransform slot = CreateUiObject("Resource_" + type, resourcesRoot).GetComponent<RectTransform>();
            slot.anchorMin = new Vector2(0f, 1f);
            slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.sizeDelta = new Vector2(ResourceCellWidth, cellHeight);
            slot.anchoredPosition = new Vector2(column * ResourceCellWidth, -164f - row * 34f);
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

            Text amountText = CreateText("Amount", slot, 11, TextAnchor.MiddleLeft, new Color(0.88f, 0.93f, 0.90f));
            amountText.fontStyle = FontStyle.Bold;
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

        private static string GetBuildingTitle(StrategyBuildTool tool)
        {
            return tool switch
            {
                StrategyBuildTool.House => "House",
                StrategyBuildTool.LumberjackCamp => "Lumberjack Camp",
                StrategyBuildTool.StonecutterCamp => "Stonecutter Camp",
                StrategyBuildTool.HunterCamp => "Hunter Camp",
                StrategyBuildTool.FisherHut => "Fisher Hut",
                StrategyBuildTool.StorageYard => "Storage Yard",
                StrategyBuildTool.Granary => "Granary",
                StrategyBuildTool.Bridge => "Bridge",
                _ => tool.ToString()
            };
        }

        private static string GetUpgradeTitle(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds
                ? "Garden Beds"
                : "Chicken Coop";
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "Eggs",
                StrategyResourceType.Turnip => "Turnip",
                StrategyResourceType.Cabbage => "Cabbage",
                StrategyResourceType.Onion => "Onion",
                StrategyResourceType.Carrot => "Carrot",
                StrategyResourceType.Potato => "Potato",
                StrategyResourceType.Game => "Game",
                StrategyResourceType.Fish => "Fish",
                _ => "none"
            };
        }

        private static string GetResidentStatus(StrategyResidentAgent resident)
        {
            string status = resident.Activity switch
            {
                StrategyResidentAgent.ResidentActivity.TendingHousehold => "tending household",
                StrategyResidentAgent.ResidentActivity.StayingInsideHome => "inside home",
                StrategyResidentAgent.ResidentActivity.MovingHome => "going home",
                StrategyResidentAgent.ResidentActivity.ArrivingAsRefugee => "going to campfire",
                StrategyResidentAgent.ResidentActivity.LeavingSettlement => "leaving settlement",
                StrategyResidentAgent.ResidentActivity.WorkingGarden => "working garden beds",
                StrategyResidentAgent.ResidentActivity.MovingToGarden => "going to garden beds",
                StrategyResidentAgent.ResidentActivity.MovingToTree => "going to a tree",
                StrategyResidentAgent.ResidentActivity.ChoppingTree => "chopping tree",
                StrategyResidentAgent.ResidentActivity.BuckingTree => "bucking trunk",
                StrategyResidentAgent.ResidentActivity.MovingToLogs => "going to Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogs => "carrying Logs",
                StrategyResidentAgent.ResidentActivity.DepositingLogs => "depositing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePickup => "going for Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageLogs => "picking up Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogsToStorage => "hauling Logs to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageLogs => "depositing Logs",
                StrategyResidentAgent.ResidentActivity.MovingToPlantTree => "looking for a planting spot",
                StrategyResidentAgent.ResidentActivity.PlantingTree => "planting a tree",
                StrategyResidentAgent.ResidentActivity.MovingToStone => "going to deposit",
                StrategyResidentAgent.ResidentActivity.MiningStone => "mining Stone",
                StrategyResidentAgent.ResidentActivity.CarryingStone => "carrying Stone",
                StrategyResidentAgent.ResidentActivity.DepositingStone => "depositing Stone",
                StrategyResidentAgent.ResidentActivity.MovingToStorageStonePickup => "going for Stone",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageStone => "picking up Stone",
                StrategyResidentAgent.ResidentActivity.CarryingStoneToStorage => "hauling Stone to storage",
                StrategyResidentAgent.ResidentActivity.DepositingStorageStone => "depositing Stone",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionStorage => "going for materials",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionLogs => "picking up construction Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionStone => "picking up construction Stone",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionLogs => "carrying Logs to construction",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionStone => "carrying Stone to construction",
                StrategyResidentAgent.ResidentActivity.DepositingConstructionResource => "depositing materials",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionSite => "going to build",
                StrategyResidentAgent.ResidentActivity.BuildingConstruction => "building",
                StrategyResidentAgent.ResidentActivity.MovingToHuntingRange => "going hunting",
                StrategyResidentAgent.ResidentActivity.AimingBow => "aiming bow",
                StrategyResidentAgent.ResidentActivity.WaitingForHuntHit => "watching the arrow",
                StrategyResidentAgent.ResidentActivity.MovingToHuntCarcass => "going to game",
                StrategyResidentAgent.ResidentActivity.ButcheringRabbit => "butchering game",
                StrategyResidentAgent.ResidentActivity.CarryingGame => "carrying Game",
                StrategyResidentAgent.ResidentActivity.DepositingGame => "depositing Game",
                StrategyResidentAgent.ResidentActivity.MovingToFishingSpot => "going to shore",
                StrategyResidentAgent.ResidentActivity.CastingFishingLine => "casting line",
                StrategyResidentAgent.ResidentActivity.WaitingForFishBite => "waiting for a bite",
                StrategyResidentAgent.ResidentActivity.ReelingFish => "reeling fish",
                StrategyResidentAgent.ResidentActivity.CarryingFish => "carrying Fish",
                StrategyResidentAgent.ResidentActivity.DepositingFish => "depositing Fish",
                StrategyResidentAgent.ResidentActivity.MovingToGranaryGamePickup => "going for Game",
                StrategyResidentAgent.ResidentActivity.PickingUpGranaryGame => "picking up Game",
                StrategyResidentAgent.ResidentActivity.CarryingGameToGranary => "hauling Game to granary",
                StrategyResidentAgent.ResidentActivity.DepositingGranaryGame => "depositing Game in granary",
                StrategyResidentAgent.ResidentActivity.MovingToGranaryFishPickup => "going for Fish",
                StrategyResidentAgent.ResidentActivity.PickingUpGranaryFish => "picking up Fish",
                StrategyResidentAgent.ResidentActivity.CarryingFishToGranary => "hauling Fish to granary",
                StrategyResidentAgent.ResidentActivity.DepositingGranaryFish => "depositing Fish in granary",
                StrategyResidentAgent.ResidentActivity.ReturningLogsToStorage => "returning Logs to storage",
                StrategyResidentAgent.ResidentActivity.ReturningStoneToStorage => "returning Stone to storage",
                StrategyResidentAgent.ResidentActivity.ReturningGameToGranary => "returning Game to granary",
                StrategyResidentAgent.ResidentActivity.ReturningFishToGranary => "returning Fish to granary",
                StrategyResidentAgent.ResidentActivity.MovingToFuneral => "going to funeral",
                StrategyResidentAgent.ResidentActivity.MourningCorpse => "mourning",
                StrategyResidentAgent.ResidentActivity.CarryingCorpseToCemetery => "carrying the dead",
                StrategyResidentAgent.ResidentActivity.MovingToBurial => "going to burial",
                StrategyResidentAgent.ResidentActivity.BuryingGrave => "burying the dead",
                StrategyResidentAgent.ResidentActivity.WaitingAtFuneral => "attending funeral",
                _ => "idle"
            };

            if (resident.IsPendingRefugee && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "refugee";
            }

            if (resident.BuilderWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "waiting for construction";
            }

            if (resident.HunterWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "waiting for prey";
            }

            if (resident.FisherWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "waiting for a bite";
            }

            if (resident.GranaryWorkplace != null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "waiting for food";
            }

            if (resident.IsHouseholder && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "tending household";
            }

            if (resident.Home == null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "at the campfire";
            }

            if (!resident.IsAdult && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "playing near home";
            }

            return status;
        }

        private static string GetConstructionBuildersText(StrategyConstructionSite site)
        {
            if (site == null || site.BuilderCount <= 0)
            {
                return "no builders assigned";
            }

            string text = string.Empty;
            for (int i = 0; i < site.BuilderCount; i++)
            {
                if (!site.TryGetBuilder(i, out StrategyResidentAgent builder) || builder == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    text += "\n";
                }

                text += builder.FullName + " - " + GetResidentStatus(builder);
            }

            return string.IsNullOrEmpty(text) ? "no builders assigned" : text;
        }

        private static string GetResidentGenderTitle(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male
                ? "male"
                : "female";
        }

        private static string GetResidentLifeStageTitle(StrategyResidentAgent resident)
        {
            return resident != null && resident.LifeStage == StrategyResidentLifeStage.Child
                ? "child"
                : "adult";
        }

        private static string DescribeSelection(Transform target)
        {
            if (target == null)
            {
                return "none";
            }

            StrategyResidentAgent resident = target.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                return "resident:" + resident.FullName;
            }

            StrategyPlacedBuilding building = target.GetComponent<StrategyPlacedBuilding>();
            if (building != null)
            {
                return "building:" + building.Tool + "@" + building.Origin.x + "," + building.Origin.y;
            }

            StrategyConstructionSite constructionSite = target.GetComponent<StrategyConstructionSite>();
            if (constructionSite != null)
            {
                return "construction:" + constructionSite.Tool + "@" + constructionSite.Origin.x + "," + constructionSite.Origin.y;
            }

            return target.name;
        }

        private void EnsureMarker()
        {
            if (markerSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Selection Marker Pixel",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, false);
                markerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }

            if (markerRenderer == null)
            {
                GameObject marker = new GameObject("World Selection Marker");
                marker.transform.SetParent(transform, false);
                markerRenderer = marker.AddComponent<SpriteRenderer>();
                markerRenderer.sprite = markerSprite;
                markerRenderer.color = new Color(1f, 0.88f, 0.18f, 0.38f);
                markerRenderer.gameObject.SetActive(false);
            }
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
