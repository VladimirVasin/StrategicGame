using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyPopulationRosterHudController : MonoBehaviour
    {
        private const float RefreshInterval = 0.35f;
        private const float RowHeight = 38f;

        private readonly List<StrategyResidentAgent> visibleResidents = new();
        private readonly List<StrategyPopulationRosterRowView> rowPool = new();
        private readonly List<Button> filterButtons = new();

        private StrategyPopulationController population;
        private RectTransform panel, contentRoot;
        private Text titleText, statsText, emptyText;
        private bool initialized;
        private bool isOpen;
        private float refreshTimer;
        private ResidentFilter activeFilter;

        private enum ResidentFilter
        {
            All,
            Adults,
            Children,
            Workers,
            Hungry,
            Homeless
        }

        public bool IsOpen => isOpen;

        public void Configure(StrategyPopulationController populationController)
        {
            population = populationController != null
                ? populationController
                : population ?? Object.FindAnyObjectByType<StrategyPopulationController>();

            if (!initialized)
            {
                initialized = true;
                BuildUi();
            }

            RefreshNow();
        }

        public void Toggle()
        {
            SetOpen(!isOpen);
        }

        public void SetOpen(bool open)
        {
            if (!initialized)
            {
                Configure(null);
            }

            bool changed = isOpen != open;
            isOpen = open;
            if (panel != null)
            {
                panel.gameObject.SetActive(open);
            }

            refreshTimer = 0f;
            if (open)
            {
                RefreshNow();
            }

            if (changed)
            {
                StrategyHudSfxAudio.Play(open ? StrategyHudSfxKind.Open : StrategyHudSfxKind.Close);
            }
        }

        private void Awake()
        {
            Configure(null);
            SetOpen(false);
        }

        private void Update()
        {
            if (!initialized)
            {
                Configure(null);
            }

            if (!isOpen)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = RefreshInterval;
            RefreshNow();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("PopulationRosterHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 34;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            panel = CreateUiObject("PopulationRosterPanel", canvasObject.transform).GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -18f);
            panel.sizeDelta = new Vector2(920f, 590f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.07f, 0.09f, 0.10f, 0.96f);

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            titleText = CreateText("Title", panel, "Residents", 24, TextAnchor.MiddleLeft, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            SetTopLeft(titleText.rectTransform, 24f, 17f, 360f, 34f);

            Button closeButton = CreateButton("CloseButton", panel, "X", 18, new Color(0.18f, 0.20f, 0.22f, 1f));
            closeButton.onClick.AddListener(() => SetOpen(false));
            SetTopRight(closeButton.GetComponent<RectTransform>(), 18f, 18f, 42f, 34f);
            BuildFamilyTreeButton();

            statsText = CreateText("Stats", panel, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            SetTopLeft(statsText.rectTransform, 24f, 59f, 820f, 24f);

            BuildFilters();
            BuildHeader();
            BuildScrollArea();
            panel.gameObject.SetActive(false);
        }

        private void BuildFilters()
        {
            RectTransform filtersRoot = CreateUiObject("Filters", panel).GetComponent<RectTransform>();
            SetTopLeft(filtersRoot, 24f, 94f, 720f, 32f);

            CreateFilterButton(filtersRoot, "All", ResidentFilter.All, 0);
            CreateFilterButton(filtersRoot, "Adults", ResidentFilter.Adults, 1);
            CreateFilterButton(filtersRoot, "Children", ResidentFilter.Children, 2);
            CreateFilterButton(filtersRoot, "Workers", ResidentFilter.Workers, 3);
            CreateFilterButton(filtersRoot, "Hungry", ResidentFilter.Hungry, 4);
            CreateFilterButton(filtersRoot, "Homeless", ResidentFilter.Homeless, 5);
            UpdateFilterButtons();
        }

        private void BuildHeader()
        {
            RectTransform header = CreateUiObject("Header", panel).GetComponent<RectTransform>();
            SetTopLeft(header, 24f, 137f, 872f, 28f);
            Image image = header.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.15f, 0.16f, 0.95f);

            AddHeaderText(header, "Name", 12f, 168f);
            AddHeaderText(header, "Age", 190f, 56f);
            AddHeaderText(header, "Home", 252f, 92f);
            AddHeaderText(header, "Role", 352f, 112f);
            AddHeaderText(header, "Status", 472f, 258f);
            AddHeaderText(header, "Food", 738f, 120f);
        }

        private void BuildScrollArea()
        {
            RectTransform viewport = CreateUiObject("RosterViewport", panel).GetComponent<RectTransform>();
            Stretch(viewport, 24f, 171f, 24f, 24f);
            Image viewImage = viewport.gameObject.AddComponent<Image>();
            viewImage.color = new Color(0.04f, 0.055f, 0.06f, 0.92f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            contentRoot = CreateUiObject("RosterContent", viewport).GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(0f, 1f);

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRoot;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            emptyText = CreateText("Empty", panel, "No residents", 15, TextAnchor.MiddleCenter, new Color(0.70f, 0.78f, 0.76f));
            Stretch(emptyText.rectTransform, 24f, 250f, 24f, 260f);
        }

        private void RefreshNow()
        {
            if (population == null)
            {
                population = Object.FindAnyObjectByType<StrategyPopulationController>();
            }

            RefreshStats();
            BuildVisibleResidents();
            EnsureRowCount(visibleResidents.Count);

            float contentHeight = Mathf.Max(1f, visibleResidents.Count * RowHeight);
            contentRoot.sizeDelta = new Vector2(0f, contentHeight);

            for (int i = 0; i < rowPool.Count; i++)
            {
                bool active = i < visibleResidents.Count;
                rowPool[i].Root.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                rowPool[i].Root.anchoredPosition = new Vector2(0f, -i * RowHeight);
                rowPool[i].Set(visibleResidents[i], i);
            }

            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(visibleResidents.Count <= 0);
            }
        }

        private void RefreshStats()
        {
            int total = 0;
            int adults = 0;
            int children = 0;
            int housed = 0;
            int workers = 0;
            int freeAdults = 0;
            int hungry = 0;
            int starving = 0;

            if (population != null)
            {
                foreach (StrategyResidentAgent resident in population.Residents)
                {
                    if (!IsRosterResident(resident))
                    {
                        continue;
                    }

                    total++;
                    adults += resident.IsAdult ? 1 : 0;
                    children += resident.IsAdult ? 0 : 1;
                    housed += resident.Home != null ? 1 : 0;
                    workers += resident.HasWorkplace || resident.HasConstructionAssignment ? 1 : 0;
                    freeAdults += IsFreeAdult(resident) ? 1 : 0;
                    hungry += resident.IsHungry ? 1 : 0;
                    starving += resident.IsStarving ? 1 : 0;
                }
            }

            titleText.text = "Residents " + total;
            statsText.text = "Adults " + adults
                + " / Children " + children
                + "   Housed " + housed
                + " / Camp " + Mathf.Max(0, total - housed)
                + "   Workers " + workers
                + " / Free adults " + freeAdults
                + "   Hungry " + hungry
                + " / Starving " + starving;
        }

        private void BuildVisibleResidents()
        {
            visibleResidents.Clear();
            if (population == null)
            {
                return;
            }

            foreach (StrategyResidentAgent resident in population.Residents)
            {
                if (IsRosterResident(resident) && MatchesFilter(resident))
                {
                    visibleResidents.Add(resident);
                }
            }

            visibleResidents.Sort(CompareResidents);
        }

        private bool MatchesFilter(StrategyResidentAgent resident)
        {
            return activeFilter switch
            {
                ResidentFilter.Adults => resident.IsAdult,
                ResidentFilter.Children => !resident.IsAdult,
                ResidentFilter.Workers => resident.HasWorkplace || resident.HasConstructionAssignment,
                ResidentFilter.Hungry => resident.IsHungry,
                ResidentFilter.Homeless => resident.Home == null,
                _ => true
            };
        }

        private static bool IsRosterResident(StrategyResidentAgent resident)
        {
            return resident != null && !resident.IsPendingRefugee;
        }

        private static bool IsFreeAdult(StrategyResidentAgent resident)
        {
            return resident != null
                && resident.CanWork
                && !resident.HasWorkplace
                && !resident.HasConstructionAssignment
                && !resident.IsFuneralDutyActive;
        }

        private void SetFilter(ResidentFilter filter)
        {
            bool changed = activeFilter != filter;
            activeFilter = filter;
            UpdateFilterButtons();
            RefreshNow();
            StrategyHudSfxAudio.Play(changed ? StrategyHudSfxKind.Step : StrategyHudSfxKind.Click);
        }

        private void CreateFilterButton(RectTransform root, string label, ResidentFilter filter, int index)
        {
            Button button = CreateButton("Filter_" + label, root, label, 12, new Color(0.12f, 0.15f, 0.16f, 1f));
            button.onClick.AddListener(() => SetFilter(filter));
            SetTopLeft(button.GetComponent<RectTransform>(), index * 105f, 0f, 96f, 30f);
            filterButtons.Add(button);
        }

        private void UpdateFilterButtons()
        {
            for (int i = 0; i < filterButtons.Count; i++)
            {
                bool active = i == (int)activeFilter;
                if (filterButtons[i].targetGraphic != null)
                {
                    filterButtons[i].targetGraphic.color = active
                        ? new Color(0.33f, 0.25f, 0.13f, 1f)
                        : new Color(0.12f, 0.15f, 0.16f, 1f);
                }
            }
        }

        private void EnsureRowCount(int count)
        {
            while (rowPool.Count < count)
            {
                rowPool.Add(CreateRow(rowPool.Count));
            }
        }

        private StrategyPopulationRosterRowView CreateRow(int index)
        {
            RectTransform root = CreateUiObject("ResidentRow_" + index, contentRoot).GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(0f, RowHeight);

            Image background = root.gameObject.AddComponent<Image>();
            StrategyPopulationRosterRowView row = new StrategyPopulationRosterRowView
            {
                Root = root,
                Background = background,
                Name = AddRowText(root, 12f, 2f, 168f, 34f, 13),
                Age = AddRowText(root, 190f, 2f, 56f, 34f, 12),
                Home = AddRowText(root, 252f, 2f, 92f, 34f, 12),
                Role = AddRowText(root, 352f, 2f, 112f, 34f, 12),
                Status = AddRowText(root, 472f, 2f, 258f, 34f, 12),
                Food = AddRowText(root, 738f, 2f, 120f, 34f, 12)
            };

            return row;
        }

        private Text AddHeaderText(Transform parent, string value, float x, float width)
        {
            return CreateSortableHeaderText(parent, value, x, width);
        }

        private static Text AddRowText(Transform parent, float x, float y, float width, float height, int fontSize)
        {
            Text text = CreateText("Text", parent, string.Empty, fontSize, TextAnchor.MiddleLeft, new Color(0.86f, 0.91f, 0.88f));
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = fontSize;
            SetTopLeft(text.rectTransform, x, y, width, height);
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, int size, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", root, label, size, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            Stretch(text.rectTransform, 4f, 0f, 4f, 0f);
            return button;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            RectTransform root = CreateUiObject(name, parent).GetComponent<RectTransform>();
            Text text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = StrategyUiThemeProvider.Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRight(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

    }
}
