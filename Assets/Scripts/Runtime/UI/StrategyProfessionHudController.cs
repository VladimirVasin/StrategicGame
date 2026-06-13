using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed class StrategyProfessionHudController : MonoBehaviour
    {
        private const float AnimationSpeed = 9f;
        private const float RowHeight = 56f;

        private static readonly StrategyProfessionType[] DisplayOrder =
        {
            StrategyProfessionType.Lumberjack,
            StrategyProfessionType.Stonecutter,
            StrategyProfessionType.Hunter,
            StrategyProfessionType.Fisher,
            StrategyProfessionType.StorageWorker,
            StrategyProfessionType.Builder,
            StrategyProfessionType.GranaryWorker
        };

        private readonly ProfessionRow[] rows = new ProfessionRow[DisplayOrder.Length];
        private StrategyPopulationController population;
        private RectTransform panelRoot;
        private RectTransform contentRoot;
        private ScrollRect professionScroll;
        private CanvasGroup panelGroup;
        private Text freeWorkersText;
        private Text actionStatusText;
        private Text buttonText;
        private bool initialized;
        private bool isOpen;
        private bool isDirty = true;
        private float panelT;

        public void Configure(StrategyPopulationController populationController)
        {
            population = populationController != null
                ? populationController
                : population ?? UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>();

            if (!initialized)
            {
                initialized = true;
                EnsureEventSystem();
                BuildUi();
            }

            isDirty = true;
            RefreshUi();
        }

        private void Awake()
        {
            Configure(null);
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure(null);
            }

            HandleInput();
            UpdateAnimation();

            if (isDirty || Time.frameCount % 15 == 0)
            {
                RefreshUi();
            }
        }

        private void ToggleOpen()
        {
            isOpen = !isOpen;
            isDirty = true;
            if (isOpen && professionScroll != null)
            {
                professionScroll.verticalNormalizedPosition = 1f;
            }

            StrategyDebugLogger.Info(
                "ProfessionHud",
                isOpen ? "Opened" : "Closed");
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            isDirty = true;
            StrategyDebugLogger.Info("ProfessionHud", "Closed");
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && isOpen)
            {
                Close();
            }

            Mouse mouse = Mouse.current;
            if (mouse != null
                && mouse.leftButton.wasPressedThisFrame
                && isOpen
                && EventSystem.current != null
                && !EventSystem.current.IsPointerOverGameObject())
            {
                Close();
            }
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("ProfessionHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 160;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366f, 768f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateTopButton(canvasObject.transform);
            CreatePanel(canvasObject.transform);
            RefreshUi();
            UpdateAnimation(true);
        }

        private void CreateTopButton(Transform parent)
        {
            RectTransform root = CreateUiObject("ProfessionButton", parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(18f, -106f);
            root.sizeDelta = new Vector2(178f, 42f);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.16f, 0.18f, 0.94f);

            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.36f);
            outline.effectDistance = new Vector2(1.3f, -1.3f);

            RectTransform icon = CreateUiObject("Icon", root).GetComponent<RectTransform>();
            SetTopLeft(icon, 10f, 7f, 28f, 28f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = StrategyProfessionIconFactory.GetIcon(StrategyProfessionType.Builder);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            buttonText = CreateText("Label", root, "Professions", 15, TextAnchor.MiddleLeft, new Color(0.95f, 0.88f, 0.62f));
            buttonText.fontStyle = FontStyle.Bold;
            SetOffsets(buttonText.rectTransform, 46f, 0f, 12f, 0f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(ToggleOpen);
            ConfigureButtonColors(button, background.color);
        }

        private void CreatePanel(Transform parent)
        {
            panelRoot = CreateUiObject("ProfessionPanel", parent).GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 1f);
            panelRoot.anchorMax = new Vector2(0.5f, 1f);
            panelRoot.pivot = new Vector2(0.5f, 1f);
            panelRoot.anchoredPosition = new Vector2(0f, -76f);
            panelRoot.sizeDelta = new Vector2(620f, 540f);

            Image background = panelRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.09f, 0.09f, 0.96f);

            Outline outline = panelRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);

            panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();

            RectTransform accent = CreateUiObject("Accent", panelRoot).GetComponent<RectTransform>();
            accent.anchorMin = new Vector2(0f, 0f);
            accent.anchorMax = new Vector2(0f, 1f);
            accent.pivot = new Vector2(0f, 0.5f);
            accent.sizeDelta = new Vector2(5f, 0f);
            accent.anchoredPosition = Vector2.zero;
            Image accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.86f, 0.63f, 0.28f, 1f);
            accentImage.raycastTarget = false;

            Text title = CreateText("Title", panelRoot, "Professions", 25, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopStretch(title.rectTransform, 24f, 18f, 84f, 34f);

            Text subtitle = CreateText("Subtitle", panelRoot, "settlement workers", 13, TextAnchor.UpperLeft, new Color(0.86f, 0.70f, 0.42f));
            subtitle.fontStyle = FontStyle.Bold;
            SetTopStretch(subtitle.rectTransform, 24f, 52f, 84f, 20f);

            RectTransform closeRoot = CreateUiObject("Close", panelRoot).GetComponent<RectTransform>();
            closeRoot.anchorMin = new Vector2(1f, 1f);
            closeRoot.anchorMax = new Vector2(1f, 1f);
            closeRoot.pivot = new Vector2(1f, 1f);
            closeRoot.anchoredPosition = new Vector2(-18f, -18f);
            closeRoot.sizeDelta = new Vector2(38f, 34f);
            Image closeImage = closeRoot.gameObject.AddComponent<Image>();
            closeImage.color = new Color(0.10f, 0.14f, 0.15f, 0.95f);
            Button closeButton = closeRoot.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            ConfigureButtonColors(closeButton, closeImage.color);
            Text closeText = CreateText("CloseText", closeRoot, "X", 16, TextAnchor.MiddleCenter, Color.white);
            closeText.fontStyle = FontStyle.Bold;
            SetOffsets(closeText.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform line = CreateUiObject("Line", panelRoot).GetComponent<RectTransform>();
            SetTopStretch(line, 24f, 84f, 24f, 2f);
            Image lineImage = line.gameObject.AddComponent<Image>();
            lineImage.color = new Color(1f, 1f, 1f, 0.22f);
            lineImage.raycastTarget = false;

            RectTransform viewport = CreateUiObject("ListViewport", panelRoot).GetComponent<RectTransform>();
            SetOffsets(viewport, 18f, 96f, 38f, 70f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            contentRoot = CreateUiObject("ListContent", viewport).GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.offsetMin = new Vector2(0f, 0f);
            contentRoot.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform scrollbarRoot = CreateUiObject("Scrollbar", panelRoot).GetComponent<RectTransform>();
            scrollbarRoot.anchorMin = new Vector2(1f, 0f);
            scrollbarRoot.anchorMax = new Vector2(1f, 1f);
            scrollbarRoot.pivot = new Vector2(1f, 0.5f);
            scrollbarRoot.offsetMin = new Vector2(-30f, 70f);
            scrollbarRoot.offsetMax = new Vector2(-18f, -96f);
            Image scrollbarTrack = scrollbarRoot.gameObject.AddComponent<Image>();
            scrollbarTrack.color = new Color(0f, 0f, 0f, 0.32f);

            RectTransform scrollbarHandle = CreateUiObject("Handle", scrollbarRoot).GetComponent<RectTransform>();
            SetOffsets(scrollbarHandle, 2f, 2f, 2f, 2f);
            Image scrollbarHandleImage = scrollbarHandle.gameObject.AddComponent<Image>();
            scrollbarHandleImage.color = new Color(0.86f, 0.70f, 0.42f, 0.88f);

            Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = scrollbarHandleImage;
            scrollbar.handleRect = scrollbarHandle;
            ConfigureScrollbarColors(scrollbar, scrollbarHandleImage.color);

            professionScroll = viewport.gameObject.AddComponent<ScrollRect>();
            professionScroll.content = contentRoot;
            professionScroll.viewport = viewport;
            professionScroll.horizontal = false;
            professionScroll.vertical = true;
            professionScroll.movementType = ScrollRect.MovementType.Clamped;
            professionScroll.inertia = true;
            professionScroll.decelerationRate = 0.12f;
            professionScroll.scrollSensitivity = 34f;
            professionScroll.verticalScrollbar = scrollbar;
            professionScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            professionScroll.verticalScrollbarSpacing = 8f;

            for (int i = 0; i < DisplayOrder.Length; i++)
            {
                rows[i] = CreateRow(DisplayOrder[i], contentRoot);
            }

            freeWorkersText = CreateText("FreeWorkers", panelRoot, string.Empty, 13, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            SetBottomStretch(freeWorkersText.rectTransform, 24f, 30f, 260f, 20f);

            actionStatusText = CreateText("ActionStatus", panelRoot, string.Empty, 13, TextAnchor.UpperRight, new Color(0.86f, 0.70f, 0.42f));
            actionStatusText.fontStyle = FontStyle.Bold;
            SetBottomStretch(actionStatusText.rectTransform, 260f, 30f, 24f, 20f);
        }

        private ProfessionRow CreateRow(StrategyProfessionType type, Transform parent)
        {
            RectTransform root = CreateUiObject(type + "Row", parent).GetComponent<RectTransform>();
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;
            layout.minHeight = RowHeight;

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.055f);

            RectTransform iconFrame = CreateUiObject("IconFrame", root).GetComponent<RectTransform>();
            SetTopLeft(iconFrame, 10f, 8f, 40f, 40f);
            Image iconFrameImage = iconFrame.gameObject.AddComponent<Image>();
            iconFrameImage.color = new Color(0f, 0f, 0f, 0.28f);
            iconFrameImage.raycastTarget = false;

            RectTransform iconRect = CreateUiObject("Icon", iconFrame).GetComponent<RectTransform>();
            SetOffsets(iconRect, 5f, 5f, 5f, 5f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = StrategyProfessionIconFactory.GetIcon(type);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text title = CreateText("Title", root, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 14;
            SetTopStretch(title.rectTransform, 62f, 8f, 174f, 18f);

            Text subtitle = CreateText("Subtitle", root, string.Empty, 11, TextAnchor.UpperLeft, new Color(0.75f, 0.83f, 0.79f));
            subtitle.resizeTextForBestFit = true;
            subtitle.resizeTextMinSize = 9;
            subtitle.resizeTextMaxSize = 11;
            SetTopStretch(subtitle.rectTransform, 62f, 30f, 174f, 16f);

            Text count = CreateText("Count", root, "0/0", 17, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.62f));
            count.fontStyle = FontStyle.Bold;
            SetRightMiddle(count.rectTransform, 132f, 0f, 58f, 34f);

            Button minusButton = CreateSquareButton("Minus", root, 68f, "-");
            Button plusButton = CreateSquareButton("Plus", root, 24f, "+");

            StrategyProfessionType capturedType = type;
            minusButton.onClick.AddListener(() => ChangeProfession(capturedType, false));
            plusButton.onClick.AddListener(() => ChangeProfession(capturedType, true));

            return new ProfessionRow
            {
                Type = type,
                Root = root,
                Background = background,
                Title = title,
                Subtitle = subtitle,
                Count = count,
                MinusButton = minusButton,
                PlusButton = plusButton
            };
        }

        private Button CreateSquareButton(string name, Transform parent, float right, string label)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(1f, 0.5f);
            root.anchorMax = new Vector2(1f, 0.5f);
            root.pivot = new Vector2(1f, 0.5f);
            root.anchoredPosition = new Vector2(-right, 0f);
            root.sizeDelta = new Vector2(36f, 34f);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.11f, 0.16f, 0.17f, 0.96f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ConfigureButtonColors(button, image.color);

            Text text = CreateText("Label", root, label, 22, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            SetOffsets(text.rectTransform, 0f, 0f, 0f, 2f);
            return button;
        }

        private void RefreshUi()
        {
            isDirty = false;
            population ??= UnityEngine.Object.FindAnyObjectByType<StrategyPopulationController>();
            int freeWorkers = CountFreeWorkers();
            int visibleRows = 0;

            for (int i = 0; i < rows.Length; i++)
            {
                ProfessionSnapshot snapshot = BuildSnapshot(rows[i].Type, freeWorkers);
                bool visible = snapshot.Capacity > 0;
                rows[i].Root.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                visibleRows++;
                ApplySnapshot(rows[i], snapshot);
            }

            float contentHeight = Mathf.Max(0f, visibleRows * RowHeight + Mathf.Max(0, visibleRows - 1) * 8f);
            contentRoot.sizeDelta = new Vector2(0f, contentHeight);
            if (professionScroll != null && !isOpen)
            {
                professionScroll.verticalNormalizedPosition = 1f;
            }

            if (freeWorkersText != null)
            {
                freeWorkersText.text = "Free adults: " + freeWorkers;
            }

            if (actionStatusText != null && string.IsNullOrEmpty(actionStatusText.text))
            {
                actionStatusText.text = visibleRows > 0
                    ? "Available: " + visibleRows
                    : "No workplaces";
            }
        }

        private void ApplySnapshot(ProfessionRow row, ProfessionSnapshot snapshot)
        {
            row.Title.text = snapshot.Title;
            row.Subtitle.text = snapshot.Subtitle;
            row.Count.text = snapshot.IsUnlimited
                ? snapshot.Assigned + "/\u221e"
                : snapshot.Assigned + "/" + snapshot.Capacity;
            row.Count.color = !snapshot.IsUnlimited && snapshot.Assigned >= snapshot.Capacity
                ? new Color(0.95f, 0.72f, 0.32f)
                : new Color(0.95f, 0.88f, 0.62f);
            row.Background.color = snapshot.Assigned > 0
                ? new Color(snapshot.Accent.r, snapshot.Accent.g, snapshot.Accent.b, 0.18f)
                : new Color(1f, 1f, 1f, 0.055f);
            row.MinusButton.interactable = snapshot.Assigned > 0;
            row.PlusButton.interactable = (snapshot.IsUnlimited || snapshot.Assigned < snapshot.Capacity)
                && snapshot.FreeWorkers > 0;
        }

        private ProfessionSnapshot BuildSnapshot(StrategyProfessionType type, int freeWorkers)
        {
            ProfessionSnapshot snapshot = CreateBaseSnapshot(type);
            snapshot.FreeWorkers = freeWorkers;

            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    snapshot.Assigned = CountAssigned(lumberCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = lumberCamps.Length * StrategyLumberjackCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    snapshot.Assigned = CountAssigned(stoneCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = stoneCamps.Length * StrategyStonecutterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    snapshot.Assigned = CountAssigned(hunterCamps, camp => camp.WorkerCount);
                    snapshot.Capacity = hunterCamps.Length * StrategyHunterCamp.MaxWorkers;
                    break;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    snapshot.Assigned = CountAssigned(fisherHuts, hut => hut.WorkerCount);
                    snapshot.Capacity = fisherHuts.Length * StrategyFisherHut.MaxWorkers;
                    break;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(storageYards, yard => yard.WorkerCount);
                    snapshot.Capacity = storageYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = storageYards.Length > 0;
                    break;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    snapshot.Assigned = CountAssigned(builderYards, yard => yard.BuilderCount);
                    snapshot.Capacity = builderYards.Length > 0 ? int.MaxValue : 0;
                    snapshot.IsUnlimited = builderYards.Length > 0;
                    break;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    snapshot.Assigned = CountAssigned(granaries, granary => granary.WorkerCount);
                    snapshot.Capacity = granaries.Length * StrategyGranary.MaxWorkers;
                    break;
            }

            return snapshot;
        }

        private ProfessionSnapshot CreateBaseSnapshot(StrategyProfessionType type)
        {
            return type switch
            {
                StrategyProfessionType.Lumberjack => new ProfessionSnapshot(type, "Lumberjacks", "chop trees and stockpile Logs", new Color(0.45f, 0.62f, 0.32f)),
                StrategyProfessionType.Stonecutter => new ProfessionSnapshot(type, "Stonecutters", "mine Stone with pickaxes", new Color(0.47f, 0.53f, 0.55f)),
                StrategyProfessionType.Hunter => new ProfessionSnapshot(type, "Hunters", "hunt rabbits", new Color(0.56f, 0.43f, 0.26f)),
                StrategyProfessionType.Fisher => new ProfessionSnapshot(type, "Fishers", "catch fish near water", new Color(0.32f, 0.54f, 0.63f)),
                StrategyProfessionType.StorageWorker => new ProfessionSnapshot(type, "Storekeepers", "haul Logs and Stone", new Color(0.58f, 0.49f, 0.37f)),
                StrategyProfessionType.Builder => new ProfessionSnapshot(type, "Builders", "build structures", new Color(0.75f, 0.55f, 0.27f)),
                StrategyProfessionType.GranaryWorker => new ProfessionSnapshot(type, "Granary Workers", "haul food to the granary", new Color(0.62f, 0.51f, 0.28f)),
                _ => new ProfessionSnapshot(type, "Profession", string.Empty, Color.white)
            };
        }

        private void ChangeProfession(StrategyProfessionType type, bool assign)
        {
            bool success = assign
                ? TryAssign(type, out StrategyResidentAgent worker)
                : TryRemove(type, out worker);

            actionStatusText.text = GetActionMessage(type, assign, success, worker);
            StrategyDebugLogger.Info(
                "ProfessionHud",
                "ProfessionChanged",
                StrategyDebugLogger.F("profession", type),
                StrategyDebugLogger.F("action", assign ? "assign" : "remove"),
                StrategyDebugLogger.F("success", success),
                StrategyDebugLogger.F("worker", worker != null ? worker.FullName : string.Empty));
            isDirty = true;
            RefreshUi();
        }

        private bool TryAssign(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    foreach (StrategyLumberjackCamp camp in FindSorted<StrategyLumberjackCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    foreach (StrategyStonecutterCamp camp in FindSorted<StrategyStonecutterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    foreach (StrategyHunterCamp camp in FindSorted<StrategyHunterCamp>())
                    {
                        if (camp != null && camp.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    foreach (StrategyFisherHut hut in FindSorted<StrategyFisherHut>())
                    {
                        if (hut != null && hut.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    foreach (StrategyStorageYard yard in FindSorted<StrategyStorageYard>())
                    {
                        if (yard != null && yard.TryAssignNextAvailableBuilder(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    foreach (StrategyGranary granary in FindSorted<StrategyGranary>())
                    {
                        if (granary != null && granary.TryAssignNextAvailableWorker(out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private bool TryRemove(StrategyProfessionType type, out StrategyResidentAgent worker)
        {
            worker = null;
            switch (type)
            {
                case StrategyProfessionType.Lumberjack:
                    StrategyLumberjackCamp[] lumberCamps = FindSorted<StrategyLumberjackCamp>();
                    for (int i = lumberCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(lumberCamps[i], lumberCamps[i].WorkerCount, out worker, index => lumberCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Stonecutter:
                    StrategyStonecutterCamp[] stoneCamps = FindSorted<StrategyStonecutterCamp>();
                    for (int i = stoneCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(stoneCamps[i], stoneCamps[i].WorkerCount, out worker, index => stoneCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Hunter:
                    StrategyHunterCamp[] hunterCamps = FindSorted<StrategyHunterCamp>();
                    for (int i = hunterCamps.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(hunterCamps[i], hunterCamps[i].WorkerCount, out worker, index => hunterCamps[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Fisher:
                    StrategyFisherHut[] fisherHuts = FindSorted<StrategyFisherHut>();
                    for (int i = fisherHuts.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(fisherHuts[i], fisherHuts[i].WorkerCount, out worker, index => fisherHuts[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.StorageWorker:
                    StrategyStorageYard[] storageYards = FindSorted<StrategyStorageYard>();
                    for (int i = storageYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(storageYards[i], storageYards[i].WorkerCount, out worker, index => storageYards[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.Builder:
                    StrategyStorageYard[] builderYards = FindSorted<StrategyStorageYard>();
                    for (int i = builderYards.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveBuilder(builderYards[i], out worker))
                        {
                            return true;
                        }
                    }

                    return false;
                case StrategyProfessionType.GranaryWorker:
                    StrategyGranary[] granaries = FindSorted<StrategyGranary>();
                    for (int i = granaries.Length - 1; i >= 0; i--)
                    {
                        if (TryRemoveWorker(granaries[i], granaries[i].WorkerCount, out worker, index => granaries[i].UnassignWorkerAt(index)))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static bool TryRemoveWorker<T>(T site, int workerCount, out StrategyResidentAgent worker, Action<int> unassignAt)
            where T : Component
        {
            worker = null;
            if (site == null || workerCount <= 0)
            {
                return false;
            }

            int index = workerCount - 1;
            switch (site)
            {
                case StrategyLumberjackCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyStonecutterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyHunterCamp camp:
                    camp.TryGetWorker(index, out worker);
                    break;
                case StrategyFisherHut hut:
                    hut.TryGetWorker(index, out worker);
                    break;
                case StrategyStorageYard yard:
                    yard.TryGetWorker(index, out worker);
                    break;
                case StrategyGranary granary:
                    granary.TryGetWorker(index, out worker);
                    break;
            }

            unassignAt(index);
            return true;
        }

        private static bool TryRemoveBuilder(StrategyStorageYard yard, out StrategyResidentAgent worker)
        {
            worker = null;
            if (yard == null || yard.BuilderCount <= 0)
            {
                return false;
            }

            int index = yard.BuilderCount - 1;
            yard.TryGetBuilder(index, out worker);
            yard.UnassignBuilderAt(index);
            return true;
        }

        private string GetActionMessage(StrategyProfessionType type, bool assign, bool success, StrategyResidentAgent worker)
        {
            string title = CreateBaseSnapshot(type).Title;
            if (!success)
            {
                return assign
                    ? title + ": no free residents or workplaces"
                    : title + ": nobody to remove";
            }

            return worker != null
                ? worker.FullName
                : assign
                    ? title + ": assigned"
                    : title + ": removed";
        }

        private int CountFreeWorkers()
        {
            if (population == null)
            {
                return 0;
            }

            int count = 0;
            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (resident != null
                    && resident.CanWork
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAssigned<T>(T[] items, Func<T, int> getCount)
            where T : Component
        {
            int total = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    total += getCount(items[i]);
                }
            }

            return total;
        }

        private static T[] FindSorted<T>()
            where T : Component
        {
            T[] items = UnityEngine.Object.FindObjectsByType<T>();
            Array.Sort(items, (left, right) => CompareOrigin(GetOrigin(left), GetOrigin(right)));
            return items;
        }

        private static int CompareOrigin(Vector2Int left, Vector2Int right)
        {
            int y = left.y.CompareTo(right.y);
            return y != 0 ? y : left.x.CompareTo(right.x);
        }

        private static Vector2Int GetOrigin(Component component)
        {
            return component switch
            {
                StrategyLumberjackCamp camp => camp.Origin,
                StrategyStonecutterCamp camp => camp.Origin,
                StrategyHunterCamp camp => camp.Origin,
                StrategyFisherHut hut => hut.Origin,
                StrategyStorageYard yard => yard.Origin,
                StrategyGranary granary => granary.Origin,
                _ => Vector2Int.zero
            };
        }

        private void UpdateAnimation(bool instant = false)
        {
            float target = isOpen ? 1f : 0f;
            panelT = instant
                ? target
                : Mathf.MoveTowards(panelT, target, Time.unscaledDeltaTime * AnimationSpeed);

            if (panelGroup == null || panelRoot == null)
            {
                return;
            }

            float smooth = Smooth01(panelT);
            panelGroup.alpha = smooth;
            panelGroup.interactable = isOpen;
            panelGroup.blocksRaycasts = isOpen;
            panelRoot.anchoredPosition = new Vector2(0f, -76f - (1f - smooth) * 18f);
            panelRoot.gameObject.SetActive(panelT > 0.001f || isOpen);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                UnityEngine.Object.Destroy(standalone);
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ConfigureButtonColors(Button button, Color baseColor)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.14f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.09f, 0.11f, 0.11f, 0.88f);
            button.colors = colors;
        }

        private static void ConfigureScrollbarColors(Scrollbar scrollbar, Color baseColor)
        {
            ColorBlock colors = scrollbar.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.20f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f);
            scrollbar.colors = colors;
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

        private static void SetBottomStretch(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetRightMiddle(RectTransform rect, float right, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-right, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private struct ProfessionSnapshot
        {
            public ProfessionSnapshot(StrategyProfessionType type, string title, string subtitle, Color accent)
            {
                Type = type;
                Title = title;
                Subtitle = subtitle;
                Accent = accent;
                Assigned = 0;
                Capacity = 0;
                FreeWorkers = 0;
                IsUnlimited = false;
            }

            public StrategyProfessionType Type;
            public string Title;
            public string Subtitle;
            public Color Accent;
            public int Assigned;
            public int Capacity;
            public int FreeWorkers;
            public bool IsUnlimited;
        }

        private sealed class ProfessionRow
        {
            public StrategyProfessionType Type;
            public RectTransform Root;
            public Image Background;
            public Text Title;
            public Text Subtitle;
            public Text Count;
            public Button MinusButton;
            public Button PlusButton;
        }
    }
}
