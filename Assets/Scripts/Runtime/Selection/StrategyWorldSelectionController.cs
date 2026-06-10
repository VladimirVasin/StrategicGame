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

        private Camera strategyCamera;
        private StrategyBuildMenuController buildMenu;
        private StrategyBuildingUpgradeController upgradeController;
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
        private readonly RectTransform[] residentRows = new RectTransform[2];
        private readonly Image[] residentPortraitImages = new Image[2];
        private readonly Text[] residentNameTexts = new Text[2];
        private readonly Text[] residentStatusTexts = new Text[2];
        private RectTransform workersRoot;
        private Text workersEmptyText;
        private readonly RectTransform[] workerRows = new RectTransform[StrategyLumberjackCamp.MaxWorkers];
        private readonly Image[] workerPortraitImages = new Image[StrategyLumberjackCamp.MaxWorkers];
        private readonly Text[] workerNameTexts = new Text[StrategyLumberjackCamp.MaxWorkers];
        private readonly Text[] workerStatusTexts = new Text[StrategyLumberjackCamp.MaxWorkers];
        private readonly Button[] workerButtons = new Button[StrategyLumberjackCamp.MaxWorkers];
        private readonly Text[] workerActionTexts = new Text[StrategyLumberjackCamp.MaxWorkers];
        private RectTransform resourcesRoot;
        private Text resourceCropText;
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
            strategyCamera = camera;
            buildMenu = menu;
            upgradeController = upgrades;
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

            HandleSelectionInput();
            UpdateHudAnimation();
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

            StrategyResidentAgent resident = selectedTransform.GetComponent<StrategyResidentAgent>();
            if (resident != null)
            {
                hudTitleText.text = resident.FullName;
                hudSubtitleText.text = "\u0416\u0438\u0442\u0435\u043b\u044c";
                hudSummaryTitleText.text = "\u041f\u0440\u043e\u0444\u0438\u043b\u044c";
                hudBodyText.text = "\u041f\u043e\u043b: "
                    + GetResidentGenderTitle(resident.Gender)
                    + "\n"
                    + "\u0420\u043e\u043b\u044c: "
                    + (resident.ConstructionSite != null
                        ? "\u0441\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u044c"
                        : resident.Workplace != null
                        ? "\u0434\u0440\u043e\u0432\u043e\u0441\u0435\u043a"
                        : resident.StoneWorkplace != null
                            ? "\u043a\u0430\u043c\u0435\u043d\u043e\u0442\u0451\u0441"
                            : resident.StorageWorkplace != null
                                ? "\u043a\u043b\u0430\u0434\u043e\u0432\u0449\u0438\u043a"
                                : "\u043f\u043e\u0441\u0435\u043b\u0435\u043d\u0435\u0446")
                    + "\n"
                    + "\u041c\u043e\u0434\u0435\u043b\u044c: #"
                    + (resident.VisualVariant + 1);
                hudStatusTitleText.text = "\u0421\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435";
                hudStatusBodyText.text = GetResidentStatus(resident);
                hudContextTitleText.text = "\u0414\u043e\u043c";
                hudContextBodyText.text = resident.Home != null
                    ? GetBuildingTitle(resident.Home.Tool)
                        + "\n"
                        + "\u043f\u0440\u0438\u043a\u0440\u0435\u043f\u043b\u0435\u043d \u043a \u0436\u0438\u043b\u0438\u0449\u0443"
                    : "\u041b\u0430\u0433\u0435\u0440\u044c"
                    + "\n"
                    + "\u0436\u0434\u0435\u0442 \u0441\u0432\u043e\u0439 \u0434\u043e\u043c";
                SetPreviewSprite(StrategyResidentSpriteFactory.GetPortraitSprite(resident.Gender, resident.VisualVariant));
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
                hudSubtitleText.text = "\u0417\u0434\u0430\u043d\u0438\u0435";
                SetBuildingPreviewSprite(building);
                SetProfileSectionVisible(false);
                SetStatusSectionVisible(false);
                SetContextSectionVisible(false);

                bool isHouse = building.Tool == StrategyBuildTool.House;
                StrategyLumberjackCamp camp = building.GetComponent<StrategyLumberjackCamp>();
                StrategyStonecutterCamp stoneCamp = building.GetComponent<StrategyStonecutterCamp>();
                StrategyStorageYard yard = building.GetComponent<StrategyStorageYard>();
                bool isLumberjackCamp = camp != null;
                bool isStonecutterCamp = stoneCamp != null;
                bool isStorageYard = yard != null;
                SetResidentsSectionVisible(isHouse);
                if (isHouse)
                {
                    RefreshResidents(building);
                }

                SetWorkersSectionVisible(isLumberjackCamp || isStonecutterCamp || isStorageYard);
                if (isLumberjackCamp)
                {
                    RefreshWorkers(camp);
                    hudContextTitleText.text = "\u041b\u0435\u0441 \u0438 \u0441\u043a\u043b\u0430\u0434";
                    hudContextBodyText.text = camp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isStonecutterCamp)
                {
                    RefreshWorkers(stoneCamp);
                    hudContextTitleText.text = "\u041a\u0430\u043c\u0435\u043d\u044c \u0438 \u0441\u043a\u043b\u0430\u0434";
                    hudContextBodyText.text = stoneCamp.GetHudStatusText();
                    SetContextSectionVisible(true);
                }
                else if (isStorageYard)
                {
                    RefreshWorkers(yard);
                    hudContextTitleText.text = "\u0421\u043a\u043b\u0430\u0434";
                    hudContextBodyText.text = yard.GetHudStatusText();
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
                hudTitleText.text = "\u0421\u0442\u0440\u043e\u0439\u043a\u0430";
                hudSubtitleText.text = constructionSite.Title;
                int stage = constructionSite.ResourcesComplete
                    ? Mathf.Clamp(1 + Mathf.FloorToInt(constructionSite.Progress * (StrategyConstructionSpriteFactory.StageCount - 1)), 1, StrategyConstructionSpriteFactory.StageCount - 1)
                    : 0;
                SetPreviewSprite(StrategyConstructionSpriteFactory.GetConstructionSprite(constructionSite.Tool, constructionSite.VisualVariant, stage));
                hudSummaryTitleText.text = "\u041f\u043b\u0430\u043d";
                hudBodyText.text = GetBuildingTitle(constructionSite.Tool)
                    + "\n"
                    + "Logs: "
                    + constructionSite.Cost.Logs
                    + "\n"
                    + "\u041a\u0430\u043c\u0435\u043d\u044c: "
                    + constructionSite.Cost.Stone;
                hudStatusTitleText.text = "\u0425\u043e\u0434 \u0441\u0442\u0440\u043e\u0439\u043a\u0438";
                hudStatusBodyText.text = constructionSite.GetHudStatusText();
                hudContextTitleText.text = "\u0421\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u0438";
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
            SetTopStretch(hudBodyText.rectTransform, 24f, 166f, 24f, 78f);

            residentsRoot = CreateUiObject("HouseResidents", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(residentsRoot, 18f, 128f, 18f, 128f);
            Image residentsBackground = residentsRoot.gameObject.AddComponent<Image>();
            residentsBackground.color = new Color(1f, 1f, 1f, 0.055f);
            residentsBackground.raycastTarget = false;
            residentsRoot.gameObject.SetActive(false);

            Text residentsTitle = CreateText("ResidentsTitle", residentsRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            residentsTitle.fontStyle = FontStyle.Bold;
            residentsTitle.text = "\u0416\u0438\u043b\u044c\u0446\u044b";
            SetTopStretch(residentsTitle.rectTransform, 6f, 10f, 6f, 18f);

            residentsEmptyText = CreateText("ResidentsEmpty", residentsRoot, 13, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            residentsEmptyText.text = "\u043f\u043e\u043a\u0430 \u043d\u0438\u043a\u0442\u043e \u043d\u0435 \u0436\u0438\u0432\u0435\u0442";
            SetTopStretch(residentsEmptyText.rectTransform, 6f, 44f, 6f, 24f);

            for (int i = 0; i < residentRows.Length; i++)
            {
                CreateResidentRow(i);
            }

            workersRoot = CreateUiObject("LumberjackWorkers", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(workersRoot, 18f, 128f, 18f, 176f);
            Image workersBackground = workersRoot.gameObject.AddComponent<Image>();
            workersBackground.color = new Color(1f, 1f, 1f, 0.055f);
            workersBackground.raycastTarget = false;
            workersRoot.gameObject.SetActive(false);

            Text workersTitle = CreateText("WorkersTitle", workersRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            workersTitle.fontStyle = FontStyle.Bold;
            workersTitle.text = "\u0420\u0430\u0431\u043e\u0447\u0438\u0435";
            SetTopStretch(workersTitle.rectTransform, 6f, 10f, 6f, 18f);

            workersEmptyText = CreateText("WorkersEmpty", workersRoot, 12, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            workersEmptyText.text = "\u043d\u0430\u0437\u043d\u0430\u0447\u044c\u0442\u0435 \u0436\u0438\u0442\u0435\u043b\u0435\u0439";
            SetTopStretch(workersEmptyText.rectTransform, 6f, 142f, 6f, 22f);

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
            SetTopStretch(resourcesRoot, 24f, 276f, 24f, 176f);
            Image resourcesBackground = resourcesRoot.gameObject.AddComponent<Image>();
            resourcesBackground.color = new Color(1f, 1f, 1f, 0.055f);
            resourcesBackground.raycastTarget = false;
            resourcesRoot.gameObject.SetActive(false);

            Text resourcesTitle = CreateText("ResourcesTitle", resourcesRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            resourcesTitle.fontStyle = FontStyle.Bold;
            resourcesTitle.text = "\u0420\u0435\u0441\u0443\u0440\u0441\u044b";
            SetTopStretch(resourcesTitle.rectTransform, 0f, 0f, 0f, 20f);

            resourceCropText = CreateText("CropText", resourcesRoot, 12, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            SetTopStretch(resourceCropText.rectTransform, 0f, 25f, 0f, 18f);

            for (int i = 0; i < StrategyHouseResourceStore.DisplayOrder.Length; i++)
            {
                CreateResourceSlot(i);
            }

            upgradeActionsRoot = CreateUiObject("HouseUpgradeActions", hudPanel).GetComponent<RectTransform>();
            SetTopStretch(upgradeActionsRoot, 24f, 478f, 24f, 196f);
            Image upgradesBackground = upgradeActionsRoot.gameObject.AddComponent<Image>();
            upgradesBackground.color = new Color(0.05f, 0.08f, 0.075f, 0.86f);
            upgradesBackground.raycastTarget = false;
            upgradeActionsRoot.gameObject.SetActive(false);

            Text upgradesTitle = CreateText("UpgradeTitle", upgradeActionsRoot, 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            upgradesTitle.fontStyle = FontStyle.Bold;
            upgradesTitle.text = "\u0423\u043b\u0443\u0447\u0448\u0435\u043d\u0438\u044f";
            SetTopStretch(upgradesTitle.rectTransform, 0f, 0f, 0f, 20f);

            gardenBedsButton = CreateUpgradeButton(
                "GardenBedsButton",
                upgradeActionsRoot,
                34f,
                "\u0413\u0440\u044f\u0434\u043a\u0438",
                out gardenBedsButtonText,
                out gardenBedsStateText,
                out gardenBedsActionText);
            gardenBedsButton.onClick.AddListener(() => TryInstallSelectedUpgrade(StrategyBuildingUpgradeType.GardenBeds));

            chickenCoopButton = CreateUpgradeButton(
                "ChickenCoopButton",
                upgradeActionsRoot,
                92f,
                "\u041a\u0443\u0440\u044f\u0442\u043d\u0438\u043a",
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

            if (residentsEmptyText != null)
            {
                residentsEmptyText.gameObject.SetActive(residentCount <= 0);
            }

            for (int i = 0; i < residentRows.Length; i++)
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
                    residentPortraitImages[i].sprite = StrategyResidentSpriteFactory.GetPortraitSprite(resident.Gender, resident.VisualVariant);
                    residentPortraitImages[i].color = Color.white;
                }

                if (residentNameTexts[i] != null)
                {
                    residentNameTexts[i].text = resident.FullName;
                }

                if (residentStatusTexts[i] != null)
                {
                    residentStatusTexts[i].text = GetResidentStatus(resident);
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
                    ? "\u043d\u0430\u0437\u043d\u0430\u0447\u044c\u0442\u0435 \u0436\u0438\u0442\u0435\u043b\u0435\u0439"
                    : "\u043d\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0445 \u0436\u0438\u0442\u0435\u043b\u0435\u0439";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                StrategyResidentAgent worker = null;
                bool hasWorker = camp != null && camp.TryGetWorker(i, out worker);
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(true);
                }

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0435 \u043c\u0435\u0441\u0442\u043e";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "\u0434\u043e 2 \u0440\u0430\u0431\u043e\u0447\u0438\u0445";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? "\u0421\u043d\u044f\u0442\u044c"
                        : "\u041d\u0430\u0437\u043d\u0430\u0447\u0438\u0442\u044c";
                    workerActionTexts[i].color = buttonEnabled ? Color.white : new Color(0.55f, 0.61f, 0.59f);
                }
            }
        }

        private void RefreshWorkers(StrategyStorageYard yard)
        {
            int workerCount = yard != null ? yard.WorkerCount : 0;
            bool canAssign = yard != null && yard.CanAssignNextAvailableWorker();

            if (workersEmptyText != null)
            {
                workersEmptyText.gameObject.SetActive(workerCount <= 0);
                workersEmptyText.text = canAssign
                    ? "\u043d\u0430\u0437\u043d\u0430\u0447\u044c\u0442\u0435 \u0436\u0438\u0442\u0435\u043b\u0435\u0439"
                    : "\u043d\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0445 \u0436\u0438\u0442\u0435\u043b\u0435\u0439";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                StrategyResidentAgent worker = null;
                bool hasWorker = yard != null && yard.TryGetWorker(i, out worker);
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(true);
                }

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0435 \u043c\u0435\u0441\u0442\u043e";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "\u0434\u043e 2 \u0440\u0430\u0431\u043e\u0447\u0438\u0445";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? "\u0421\u043d\u044f\u0442\u044c"
                        : "\u041d\u0430\u0437\u043d\u0430\u0447\u0438\u0442\u044c";
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
                    ? "\u043d\u0430\u0437\u043d\u0430\u0447\u044c\u0442\u0435 \u0436\u0438\u0442\u0435\u043b\u0435\u0439"
                    : "\u043d\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0445 \u0436\u0438\u0442\u0435\u043b\u0435\u0439";
            }

            for (int i = 0; i < workerRows.Length; i++)
            {
                StrategyResidentAgent worker = null;
                bool hasWorker = camp != null && camp.TryGetWorker(i, out worker);
                if (workerRows[i] != null)
                {
                    workerRows[i].gameObject.SetActive(true);
                }

                if (workerPortraitImages[i] != null)
                {
                    workerPortraitImages[i].sprite = hasWorker
                        ? StrategyResidentSpriteFactory.GetPortraitSprite(worker.Gender, worker.VisualVariant)
                        : null;
                    workerPortraitImages[i].color = hasWorker ? Color.white : new Color(1f, 1f, 1f, 0f);
                }

                if (workerNameTexts[i] != null)
                {
                    workerNameTexts[i].text = hasWorker
                        ? worker.FullName
                        : "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0435 \u043c\u0435\u0441\u0442\u043e";
                    workerNameTexts[i].color = hasWorker ? Color.white : new Color(0.72f, 0.80f, 0.76f);
                }

                if (workerStatusTexts[i] != null)
                {
                    workerStatusTexts[i].text = hasWorker
                        ? GetResidentStatus(worker)
                        : "\u0434\u043e 2 \u0440\u0430\u0431\u043e\u0447\u0438\u0445";
                }

                bool buttonEnabled = hasWorker || (i == workerCount && canAssign);
                if (workerButtons[i] != null)
                {
                    workerButtons[i].interactable = buttonEnabled;
                }

                if (workerActionTexts[i] != null)
                {
                    workerActionTexts[i].text = hasWorker
                        ? "\u0421\u043d\u044f\u0442\u044c"
                        : "\u041d\u0430\u0437\u043d\u0430\u0447\u0438\u0442\u044c";
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
                upgradeStatusMessage = "\u0423\u0436\u0435 \u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043e.";
                RefreshHud();
                return;
            }

            if (upgradeController == null)
            {
                upgradeStatusMessage = "\u0421\u0438\u0441\u0442\u0435\u043c\u0430 \u0443\u043b\u0443\u0447\u0448\u0435\u043d\u0438\u0439 \u043d\u0435 \u0433\u043e\u0442\u043e\u0432\u0430.";
                RefreshHud();
                return;
            }

            if (upgradeController.TryInstallUpgrade(building, type, out _))
            {
                upgradeStatusMessage = GetUpgradeTitle(type)
                    + " "
                    + "\u0434\u043e\u0431\u0430\u0432\u043b\u0435\u043d\u044b \u0440\u044f\u0434\u043e\u043c \u0441 \u0434\u043e\u043c\u043e\u043c.";
            }
            else
            {
                upgradeStatusMessage = "\u041d\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0433\u043e \u043c\u0435\u0441\u0442\u0430 \u0440\u044f\u0434\u043e\u043c \u0441 \u0434\u043e\u043c\u043e\u043c.";
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
                "\u0413\u0440\u044f\u0434\u043a\u0438");
            RefreshUpgradeButton(
                chickenCoopButton,
                chickenCoopButtonText,
                chickenCoopStateText,
                chickenCoopActionText,
                building,
                StrategyBuildingUpgradeType.ChickenCoop,
                "\u041a\u0443\u0440\u044f\u0442\u043d\u0438\u043a");

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
            button.interactable = !installed && upgradeController != null;
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (stateText != null)
            {
                stateText.text = installed
                    ? "\u0443\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u043e"
                    : upgradeController != null
                        ? "\u043c\u043e\u0436\u043d\u043e \u0434\u043e\u0431\u0430\u0432\u0438\u0442\u044c"
                        : "\u043d\u0435 \u0433\u043e\u0442\u043e\u0432\u043e";
                stateText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : new Color(0.76f, 0.83f, 0.80f);
            }

            if (actionText != null)
            {
                actionText.text = installed
                    ? "\u0413\u043e\u0442\u043e\u0432\u043e"
                    : "\u0414\u043e\u0431\u0430\u0432\u0438\u0442\u044c";
                actionText.color = installed
                    ? new Color(0.70f, 0.88f, 0.74f)
                    : Color.white;
            }
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

            if (resourceCropText != null)
            {
                resourceCropText.text = building.TryGetUpgrade(StrategyBuildingUpgradeType.GardenBeds, out StrategyBuildingUpgrade garden)
                    ? "\u041a\u0443\u043b\u044c\u0442\u0443\u0440\u0430: " + GetResourceTitle(garden.ProducedResource)
                    : "\u041a\u0443\u043b\u044c\u0442\u0443\u0440\u0430: \u043d\u0435\u0442";
            }

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
                        resourceSlots[i].anchoredPosition = new Vector2(column * ResourceCellWidth, -54f - row * 36f);
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
        }

        private void SetResourcesVisible(bool visible)
        {
            if (resourcesRoot != null)
            {
                resourcesRoot.gameObject.SetActive(visible);
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
            slot.anchoredPosition = new Vector2(column * ResourceCellWidth, -54f - row * 36f);
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

        private void CreateResidentRow(int index)
        {
            RectTransform row = CreateUiObject("Resident_" + index, residentsRoot).GetComponent<RectTransform>();
            SetTopStretch(row, 6f, 36f + index * 43f, 6f, 38f);
            residentRows[index] = row;

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
            residentPortraitImages[index] = portrait;

            Text nameText = CreateText("Name", row, 12, TextAnchor.UpperLeft, Color.white);
            nameText.fontStyle = FontStyle.Bold;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 9;
            nameText.resizeTextMaxSize = 12;
            SetTopStretch(nameText.rectTransform, 44f, 4f, 8f, 16f);
            residentNameTexts[index] = nameText;

            Text statusText = CreateText("Status", row, 11, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 9;
            statusText.resizeTextMaxSize = 11;
            SetTopStretch(statusText.rectTransform, 44f, 22f, 8f, 13f);
            residentStatusTexts[index] = statusText;

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
            iconText.text = label == "\u0413\u0440\u044f\u0434\u043a\u0438" ? "G" : "K";
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
                StrategyBuildTool.House => "\u0414\u043e\u043c",
                StrategyBuildTool.LumberjackCamp => "\u041b\u0430\u0433\u0435\u0440\u044c \u0434\u0440\u043e\u0432\u043e\u0441\u0435\u043a\u043e\u0432",
                StrategyBuildTool.StonecutterCamp => "\u041b\u0430\u0433\u0435\u0440\u044c \u043a\u0430\u043c\u0435\u043d\u043e\u0442\u0451\u0441\u043e\u0432",
                StrategyBuildTool.StorageYard => "\u0421\u043a\u043b\u0430\u0434",
                _ => tool.ToString()
            };
        }

        private static string GetUpgradeTitle(StrategyBuildingUpgradeType type)
        {
            return type == StrategyBuildingUpgradeType.GardenBeds
                ? "\u0413\u0440\u044f\u0434\u043a\u0438"
                : "\u041a\u0443\u0440\u044f\u0442\u043d\u0438\u043a";
        }

        private static string GetResourceTitle(StrategyResourceType type)
        {
            return type switch
            {
                StrategyResourceType.Eggs => "\u042f\u0439\u0446\u0430",
                StrategyResourceType.Turnip => "\u0420\u0435\u043f\u0430",
                StrategyResourceType.Cabbage => "\u041a\u0430\u043f\u0443\u0441\u0442\u0430",
                StrategyResourceType.Onion => "\u041b\u0443\u043a",
                StrategyResourceType.Carrot => "\u041c\u043e\u0440\u043a\u043e\u0432\u044c",
                StrategyResourceType.Potato => "\u041a\u0430\u0440\u0442\u043e\u0444\u0435\u043b\u044c",
                _ => "\u043d\u0435\u0442"
            };
        }

        private static string GetResidentStatus(StrategyResidentAgent resident)
        {
            string status = resident.Activity switch
            {
                StrategyResidentAgent.ResidentActivity.MovingHome => "\u0438\u0434\u0435\u0442 \u043a \u0434\u043e\u043c\u0443",
                StrategyResidentAgent.ResidentActivity.WorkingGarden => "\u0440\u0430\u0431\u043e\u0442\u0430\u0435\u0442 \u043d\u0430 \u0433\u0440\u044f\u0434\u043a\u0435",
                StrategyResidentAgent.ResidentActivity.MovingToGarden => "\u0438\u0434\u0435\u0442 \u043a \u0433\u0440\u044f\u0434\u043a\u0435",
                StrategyResidentAgent.ResidentActivity.MovingToTree => "\u0438\u0434\u0435\u0442 \u043a \u0434\u0435\u0440\u0435\u0432\u0443",
                StrategyResidentAgent.ResidentActivity.ChoppingTree => "\u0440\u0443\u0431\u0438\u0442 \u0434\u0435\u0440\u0435\u0432\u043e",
                StrategyResidentAgent.ResidentActivity.BuckingTree => "\u0440\u0443\u0431\u0438\u0442 \u0441\u0442\u0432\u043e\u043b",
                StrategyResidentAgent.ResidentActivity.MovingToLogs => "\u0438\u0434\u0435\u0442 \u043a Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogs => "\u043d\u0435\u0441\u0435\u0442 Logs",
                StrategyResidentAgent.ResidentActivity.DepositingLogs => "\u0441\u043a\u043b\u0430\u0434\u0438\u0440\u0443\u0435\u0442 Logs",
                StrategyResidentAgent.ResidentActivity.MovingToStoragePickup => "\u0438\u0434\u0435\u0442 \u0437\u0430 Logs",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageLogs => "\u0437\u0430\u0431\u0438\u0440\u0430\u0435\u0442 Logs",
                StrategyResidentAgent.ResidentActivity.CarryingLogsToStorage => "\u043d\u0435\u0441\u0435\u0442 Logs \u043d\u0430 \u0441\u043a\u043b\u0430\u0434",
                StrategyResidentAgent.ResidentActivity.DepositingStorageLogs => "\u0441\u043a\u043b\u0430\u0434\u0438\u0440\u0443\u0435\u0442 Logs",
                StrategyResidentAgent.ResidentActivity.MovingToPlantTree => "\u0438\u0449\u0435\u0442 \u043c\u0435\u0441\u0442\u043e \u0434\u043b\u044f \u0441\u0430\u0436\u0435\u043d\u0446\u0430",
                StrategyResidentAgent.ResidentActivity.PlantingTree => "\u0441\u0430\u0436\u0430\u0435\u0442 \u0434\u0435\u0440\u0435\u0432\u043e",
                StrategyResidentAgent.ResidentActivity.MovingToStone => "\u0438\u0434\u0435\u0442 \u043a \u0437\u0430\u043b\u0435\u0436\u0438",
                StrategyResidentAgent.ResidentActivity.MiningStone => "\u0434\u043e\u0431\u044b\u0432\u0430\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c",
                StrategyResidentAgent.ResidentActivity.CarryingStone => "\u043d\u0435\u0441\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c",
                StrategyResidentAgent.ResidentActivity.DepositingStone => "\u0441\u043a\u043b\u0430\u0434\u0438\u0440\u0443\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c",
                StrategyResidentAgent.ResidentActivity.MovingToStorageStonePickup => "\u0438\u0434\u0435\u0442 \u0437\u0430 \u043a\u0430\u043c\u043d\u0435\u043c",
                StrategyResidentAgent.ResidentActivity.PickingUpStorageStone => "\u0437\u0430\u0431\u0438\u0440\u0430\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c",
                StrategyResidentAgent.ResidentActivity.CarryingStoneToStorage => "\u043d\u0435\u0441\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c \u043d\u0430 \u0441\u043a\u043b\u0430\u0434",
                StrategyResidentAgent.ResidentActivity.DepositingStorageStone => "\u0441\u043a\u043b\u0430\u0434\u0438\u0440\u0443\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionStorage => "\u0438\u0434\u0435\u0442 \u0437\u0430 \u043c\u0430\u0442\u0435\u0440\u0438\u0430\u043b\u0430\u043c\u0438",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionLogs => "\u0431\u0435\u0440\u0435\u0442 Logs \u0434\u043b\u044f \u0441\u0442\u0440\u043e\u0439\u043a\u0438",
                StrategyResidentAgent.ResidentActivity.PickingUpConstructionStone => "\u0431\u0435\u0440\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c \u0434\u043b\u044f \u0441\u0442\u0440\u043e\u0439\u043a\u0438",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionLogs => "\u043d\u0435\u0441\u0435\u0442 Logs \u043d\u0430 \u0441\u0442\u0440\u043e\u0439\u043a\u0443",
                StrategyResidentAgent.ResidentActivity.CarryingConstructionStone => "\u043d\u0435\u0441\u0435\u0442 \u043a\u0430\u043c\u0435\u043d\u044c \u043d\u0430 \u0441\u0442\u0440\u043e\u0439\u043a\u0443",
                StrategyResidentAgent.ResidentActivity.DepositingConstructionResource => "\u0441\u043a\u043b\u0430\u0434\u044b\u0432\u0430\u0435\u0442 \u043c\u0430\u0442\u0435\u0440\u0438\u0430\u043b\u044b",
                StrategyResidentAgent.ResidentActivity.MovingToConstructionSite => "\u0438\u0434\u0435\u0442 \u0441\u0442\u0440\u043e\u0438\u0442\u044c",
                StrategyResidentAgent.ResidentActivity.BuildingConstruction => "\u0441\u0442\u0440\u043e\u0438\u0442",
                _ => "idle"
            };

            if (resident.Home == null && resident.Activity == StrategyResidentAgent.ResidentActivity.Idle)
            {
                return "\u0443 \u043a\u043e\u0441\u0442\u0440\u0430";
            }

            return status;
        }

        private static string GetConstructionBuildersText(StrategyConstructionSite site)
        {
            if (site == null || site.BuilderCount <= 0)
            {
                return "\u0441\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u0438 \u043d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u044b";
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

            return string.IsNullOrEmpty(text) ? "\u0441\u0442\u0440\u043e\u0438\u0442\u0435\u043b\u0438 \u043d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u044b" : text;
        }

        private static string GetResidentGenderTitle(StrategyResidentGender gender)
        {
            return gender == StrategyResidentGender.Male
                ? "\u043c\u0443\u0436\u0447\u0438\u043d\u0430"
                : "\u0436\u0435\u043d\u0449\u0438\u043d\u0430";
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
