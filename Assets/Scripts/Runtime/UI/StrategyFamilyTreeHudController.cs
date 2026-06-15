using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyFamilyTreeHudController : MonoBehaviour
    {
        private const string PauseReason = "FamilyTrees";
        private const float CardWidth = 150f;
        private const float CardHeight = 76f;
        private const float CardSpacing = 22f;
        private const float GenerationGap = 42f;
        private const float RelationshipLabelHeight = 16f;
        private const float FamilyColumnWidth = 380f;
        private const float FamilySectionGap = 28f;
        private const float MinimumContentWidth = FamilyColumnWidth;

        private sealed class FamilyTreeGroup
        {
            public readonly List<StrategyResidentFamilyRecord> Records = new();
            public string FamilyName;
            public int DuplicateIndex;
            public int DuplicateCount;
            public int SortAge;
            public int SortResidentId;
        }

        private readonly List<StrategyResidentFamilyRecord> allRecords = new();
        private readonly List<StrategyResidentFamilyRecord> familyRecords = new();
        private readonly List<FamilyTreeGroup> familyGroups = new();
        private readonly Dictionary<int, StrategyResidentFamilyRecord> recordsById = new();
        private readonly Dictionary<int, int> generationsById = new();
        private readonly Dictionary<int, Vector2> cardTopLeftById = new();
        private readonly Dictionary<int, Vector2> cardCenterById = new();
        private readonly Dictionary<int, List<StrategyResidentFamilyRecord>> rowsByGeneration = new();
        private readonly Dictionary<int, Text> relationshipLabelsById = new();
        private readonly HashSet<int> familyIds = new();

        private StrategyPopulationController population;
        private StrategyTimeScaleController timeScale;
        private RectTransform panel, contentRoot;
        private Text summaryText, emptyText;
        private int hoveredResidentId;
        private bool initialized;
        private bool isOpen;
        private bool pausePushed;

        public void Configure(StrategyPopulationController populationController, StrategyTimeScaleController timeScaleController)
        {
            population = populationController != null
                ? populationController
                : population ?? Object.FindAnyObjectByType<StrategyPopulationController>();
            timeScale = timeScaleController != null
                ? timeScaleController
                : timeScale ?? Object.FindAnyObjectByType<StrategyTimeScaleController>();

            if (!initialized)
            {
                initialized = true;
                BuildUi();
            }
        }

        public void SetOpen(bool open)
        {
            if (!initialized)
            {
                Configure(null, null);
            }

            isOpen = open;
            panel.gameObject.SetActive(open);

            if (open)
            {
                PushPause();
                RefreshTrees();
            }
            else
            {
                ReleasePause();
            }
        }

        private void Awake()
        {
            Configure(null, null);
            SetOpen(false);
        }

        private void OnDisable()
        {
            ReleasePause();
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        private void PushPause()
        {
            if (pausePushed)
            {
                return;
            }

            timeScale = timeScale != null ? timeScale : Object.FindAnyObjectByType<StrategyTimeScaleController>();
            timeScale?.PushPauseLock(PauseReason);
            pausePushed = true;
        }

        private void ReleasePause()
        {
            if (!pausePushed)
            {
                return;
            }

            timeScale = timeScale != null ? timeScale : Object.FindAnyObjectByType<StrategyTimeScaleController>();
            timeScale?.PopPauseLock(PauseReason);
            pausePushed = false;
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("FamilyTreeSceneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            panel = CreateUiObject("FamilyTreeScene", canvasObject.transform).GetComponent<RectTransform>();
            Stretch(panel, 0f, 0f, 0f, 0f);
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.045f, 0.05f, 1f);

            Text title = CreateText("Title", panel, "Family Trees", 28, TextAnchor.MiddleLeft, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetTopLeft(title.rectTransform, 30f, 20f, 420f, 42f);

            Button close = CreateButton("BackButton", panel, "Back", 15, new Color(0.18f, 0.20f, 0.22f, 1f));
            close.onClick.AddListener(() => SetOpen(false));
            SetTopRight(close.GetComponent<RectTransform>(), 30f, 24f, 86f, 36f);

            summaryText = CreateText("Summary", panel, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.78f, 0.86f, 0.82f));
            SetTopLeft(summaryText.rectTransform, 30f, 66f, 1080f, 24f);

            BuildScrollArea();
            panel.gameObject.SetActive(false);
        }

        private void BuildScrollArea()
        {
            RectTransform viewport = CreateUiObject("TreeViewport", panel).GetComponent<RectTransform>();
            Stretch(viewport, 30f, 104f, 56f, 56f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0.06f, 0.075f, 0.08f, 0.92f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            contentRoot = CreateUiObject("TreeContent", viewport).GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(0f, 1f);
            contentRoot.pivot = new Vector2(0f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(MinimumContentWidth, 1f);

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRoot;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 58f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.verticalScrollbar = CreateVerticalScrollbar(panel);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.horizontalScrollbar = CreateHorizontalScrollbar(panel);
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            emptyText = CreateText("Empty", panel, "No family records", 17, TextAnchor.MiddleCenter, new Color(0.72f, 0.80f, 0.76f));
            Stretch(emptyText.rectTransform, 30f, 320f, 30f, 320f);
        }

        private void RefreshTrees()
        {
            ClearContent();
            BuildAllRecords();

            bool hasRecords = allRecords.Count > 0;
            emptyText.gameObject.SetActive(!hasRecords);
            if (!hasRecords)
            {
                summaryText.text = "No accepted residents yet.";
                contentRoot.sizeDelta = new Vector2(MinimumContentWidth, 1f);
                return;
            }

            float x = 0f;
            float contentWidth = MinimumContentWidth;
            float contentHeight = 1f;
            BuildFamilyGroups();
            familyGroups.Sort(CompareFamilyGroups);
            SortFamilyGroupsByRelationshipAffinity();
            AssignDuplicateFamilyIndexes();
            for (int i = 0; i < familyGroups.Count; i++)
            {
                CopyFamilyGroup(familyGroups[i]);
                float sectionWidth = BuildFamilySection(
                    GetFamilyHeader(familyGroups[i]),
                    familyRecords,
                    x,
                    0f,
                    out float sectionHeight);
                x += sectionWidth + FamilySectionGap;
                contentWidth = Mathf.Max(contentWidth, x - FamilySectionGap);
                contentHeight = Mathf.Max(contentHeight, sectionHeight);
            }

            contentRoot.sizeDelta = new Vector2(contentWidth, contentHeight + 24f);
            summaryText.text = familyGroups.Count + " families / " + allRecords.Count + " recorded members";
        }

        private void BuildAllRecords()
        {
            allRecords.Clear();
            recordsById.Clear();
            familyGroups.Clear();

            if (population == null)
            {
                population = Object.FindAnyObjectByType<StrategyPopulationController>();
            }

            if (population == null)
            {
                return;
            }

            foreach (StrategyResidentFamilyRecord record in population.FamilyRecords)
            {
                if (record == null || record.ResidentId <= 0)
                {
                    continue;
                }

                allRecords.Add(record);
                recordsById[record.ResidentId] = record;
            }
        }

        private void BuildFamilyGroups()
        {
            HashSet<int> visitedIds = new();
            for (int i = 0; i < allRecords.Count; i++)
            {
                StrategyResidentFamilyRecord record = allRecords[i];
                if (visitedIds.Contains(record.ResidentId))
                {
                    continue;
                }

                FamilyTreeGroup group = new();
                AddConnectedFamilyRecords(record.ResidentId, visitedIds, group.Records);
                if (group.Records.Count <= 0)
                {
                    continue;
                }

                group.Records.Sort(CompareFamilyMembers);
                group.FamilyName = GetPrimaryFamilyName(group.Records);
                group.SortAge = group.Records[0].DisplayAgeYears;
                group.SortResidentId = group.Records[0].ResidentId;
                familyGroups.Add(group);
            }
        }

        private void AddConnectedFamilyRecords(
            int startId,
            HashSet<int> visitedIds,
            List<StrategyResidentFamilyRecord> records)
        {
            Queue<int> queue = new();
            queue.Enqueue(startId);
            visitedIds.Add(startId);

            while (queue.Count > 0)
            {
                int residentId = queue.Dequeue();
                if (!recordsById.TryGetValue(residentId, out StrategyResidentFamilyRecord record))
                {
                    continue;
                }

                records.Add(record);
                EnqueueFamilyRecord(record.FatherId, visitedIds, queue);
                EnqueueFamilyRecord(record.MotherId, visitedIds, queue);
                IReadOnlyList<int> childIds = record.ChildIds;
                for (int i = 0; i < childIds.Count; i++)
                {
                    EnqueueFamilyRecord(childIds[i], visitedIds, queue);
                }
            }
        }

        private void EnqueueFamilyRecord(int residentId, HashSet<int> visitedIds, Queue<int> queue)
        {
            if (residentId <= 0 || visitedIds.Contains(residentId) || !recordsById.ContainsKey(residentId))
            {
                return;
            }

            visitedIds.Add(residentId);
            queue.Enqueue(residentId);
        }

        private void AssignDuplicateFamilyIndexes()
        {
            Dictionary<string, int> totals = new();
            for (int i = 0; i < familyGroups.Count; i++)
            {
                string familyName = familyGroups[i].FamilyName;
                totals.TryGetValue(familyName, out int total);
                totals[familyName] = total + 1;
            }

            Dictionary<string, int> indexes = new();
            for (int i = 0; i < familyGroups.Count; i++)
            {
                FamilyTreeGroup group = familyGroups[i];
                totals.TryGetValue(group.FamilyName, out int total);
                indexes.TryGetValue(group.FamilyName, out int index);
                group.DuplicateCount = total;
                group.DuplicateIndex = index + 1;
                indexes[group.FamilyName] = group.DuplicateIndex;
            }
        }

        private void CopyFamilyGroup(FamilyTreeGroup group)
        {
            familyRecords.Clear();
            if (group == null)
            {
                return;
            }

            familyRecords.AddRange(group.Records);
            familyRecords.Sort(CompareFamilyMembers);
        }

        private static string GetFamilyHeader(FamilyTreeGroup group)
        {
            if (group == null)
            {
                return "Unknown family";
            }

            string header = group.FamilyName + " family";
            return group.DuplicateCount > 1 ? header + " " + group.DuplicateIndex : header;
        }

        private static string GetPrimaryFamilyName(List<StrategyResidentFamilyRecord> records)
        {
            if (records == null || records.Count <= 0)
            {
                return "Unknown";
            }

            Dictionary<string, int> counts = new();
            string bestName = GetFamilyName(records[0]);
            int bestCount = 0;
            for (int i = 0; i < records.Count; i++)
            {
                string familyName = GetFamilyName(records[i]);
                counts.TryGetValue(familyName, out int count);
                count++;
                counts[familyName] = count;
                if (count > bestCount)
                {
                    bestName = familyName;
                    bestCount = count;
                }
            }

            return bestName;
        }

        private static int CompareFamilyGroups(FamilyTreeGroup left, FamilyTreeGroup right)
        {
            int nameCompare = string.Compare(left.FamilyName, right.FamilyName, System.StringComparison.Ordinal);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            int ageCompare = left.SortAge.CompareTo(right.SortAge);
            return ageCompare != 0 ? -ageCompare : left.SortResidentId.CompareTo(right.SortResidentId);
        }

        private static string GetFamilyName(StrategyResidentFamilyRecord record)
        {
            return record != null && !string.IsNullOrWhiteSpace(record.FamilyName)
                ? record.FamilyName
                : "Unknown";
        }

        private static int CompareFamilyMembers(StrategyResidentFamilyRecord left, StrategyResidentFamilyRecord right)
        {
            int generationCompare = left.DisplayAgeYears.CompareTo(right.DisplayAgeYears);
            return generationCompare != 0
                ? -generationCompare
                : string.Compare(left.FullName, right.FullName, System.StringComparison.Ordinal);
        }
    }
}
