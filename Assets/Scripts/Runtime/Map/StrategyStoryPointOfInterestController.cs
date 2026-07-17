using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyStoryPointOfInterestController : MonoBehaviour
    {
        private const int SpareCandidateCount = 2;
        private const int PlacementSeedSalt = 4187;
        private const int ResourceSeparation = 6;

        private readonly List<StrategyStoryPointOfInterestAnchor> anchors = new();
        private readonly List<StrategyStoryPointOfInterestCandidatePlan> latentCandidates = new();
        private readonly Queue<StoryNotice> pendingNotices = new();
        private readonly Dictionary<string, IStrategyStoryPointOfInterestEncounter> encounters =
            new(StringComparer.Ordinal);

        private CityMapController map;
        private StrategyFogOfWarController fog;
        private StrategyPopulationController population;
        private StrategyPointOfInterestController resourcePoints;
        private StrategyStoryPointOfInterestCatalog catalog;
        private StrategyTimeScaleController timeScale;
        private StrategyPointOfInterestDialogController dialog;
        private Transform anchorRoot;
        private Vector2Int campCell;
        private int nextSequenceIndex;
        private bool configured;
        private bool noticeOpen;
        private bool pauseHeld;
        private int activeNoticeToken;
        private int noticeOpenedFrame;

        public static StrategyStoryPointOfInterestController Active { get; private set; }
        public IReadOnlyList<StrategyStoryPointOfInterestAnchor> Anchors => anchors;
        public StrategyStoryPointOfInterestCatalog Catalog => catalog;
        public int NextSequenceIndex => nextSequenceIndex;
        public int LatentCandidateCount => latentCandidates.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActive()
        {
            Active = null;
        }

        public void Configure(
            CityMapController mapController,
            StrategyFogOfWarController fogController,
            StrategyPopulationController populationController,
            StrategyPointOfInterestController resourcePointController,
            StrategyTimeScaleController timeScaleController,
            StrategyPointOfInterestDialogController dialogController,
            Vector2Int startupCampCell,
            StrategyStoryPointOfInterestCatalog storyCatalog,
            bool generateImmediately)
        {
            ClearAnchorObjects();
            CancelPendingNotices();
            map = mapController;
            fog = fogController;
            population = populationController;
            resourcePoints = resourcePointController;
            timeScale = timeScaleController;
            dialog = dialogController;
            campCell = startupCampCell;
            catalog = storyCatalog ?? StrategyStoryPointOfInterestCatalog.Production;
            nextSequenceIndex = 0;
            configured = map != null && fog != null;
            Active = this;
            EnsureAnchorRoot();

            if (configured && generateImmediately)
            {
                GenerateDefaultAnchors();
            }

            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "Configured",
                StrategyDebugLogger.F("definitions", catalog.Count),
                StrategyDebugLogger.F("anchors", anchors.Count),
                StrategyDebugLogger.F("generateImmediately", generateImmediately));
        }

        public bool HasAnchorAt(Vector2Int cell)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i] != null && anchors[i].Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasCommittedForResident(int residentId)
        {
            if (residentId <= 0)
            {
                return false;
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                StrategyStoryPointOfInterestAnchor anchor = anchors[i];
                if (anchor != null
                    && anchor.State == StrategyStoryPointOfInterestState.Committed
                    && anchor.CommittedResidentId == residentId)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryGetDefinition(
            StrategyStoryPointOfInterestAnchor anchor,
            out StrategyStoryPointOfInterestDefinition definition)
        {
            definition = null;
            return anchor != null
                && catalog != null
                && catalog.TryGet(anchor.DefinitionId, out definition);
        }

        public void RegisterEncounter(IStrategyStoryPointOfInterestEncounter encounter)
        {
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.EncounterId))
            {
                throw new ArgumentException("A story encounter with a stable ID is required.", nameof(encounter));
            }

            encounters[encounter.EncounterId] = encounter;
        }

        private void GenerateDefaultAnchors()
        {
            ClearAnchorObjects();
            nextSequenceIndex = 0;
            RebuildLatentCandidates();
        }

        public void RebuildLatentCandidates()
        {
            latentCandidates.Clear();
            if (!configured
                || catalog == null
                || nextSequenceIndex < 0
                || nextSequenceIndex >= catalog.Count)
            {
                return;
            }

            foreach (StrategyStoryPointOfInterestDistanceTier tier in Enum.GetValues(
                typeof(StrategyStoryPointOfInterestDistanceTier)))
            {
                int remainingDefinitions = CountRemainingDefinitions(tier);
                if (remainingDefinitions <= 0)
                {
                    continue;
                }

                int targetCount = Mathf.Max(
                    StrategyStoryPointOfInterestPlacement.GetMinimumCandidateCount(tier),
                    remainingDefinitions + SpareCandidateCount);
                latentCandidates.AddRange(
                    StrategyStoryPointOfInterestPlacement.SelectCandidates(
                        map.Width,
                        map.Height,
                        map.ActiveSeed ^ PlacementSeedSalt,
                        campCell,
                        tier,
                        targetCount,
                        map.IsCellWalkable,
                        CanUseStoryCandidateCell));
            }

            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "CandidatesGenerated",
                StrategyDebugLogger.F("candidates", latentCandidates.Count),
                StrategyDebugLogger.F("durableAnchors", anchors.Count),
                StrategyDebugLogger.F("remainingDefinitions", catalog.Count - nextSequenceIndex));
        }

        private int CountRemainingDefinitions(StrategyStoryPointOfInterestDistanceTier tier)
        {
            int count = 0;
            for (int i = nextSequenceIndex; i < catalog.Count; i++)
            {
                if (catalog.Definitions[i].DistanceTier == tier)
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanUseStoryCandidateCell(Vector2Int cell)
        {
            return map != null
                && map.IsCellWalkable(cell)
                && map.IsCellBuildable(cell)
                && fog?.IsCellPersistentlyExplored(cell) != true
                && fog?.IsCellVisibleAtDaylightRange(cell) != true
                && !HasAnchorAt(cell)
                && !IsNearResourcePoint(cell)
                && !HasForageAt(cell)
                && StrategyTrailController.Active?.HasRouteRoadAt(cell) != true;
        }

        private bool IsNearResourcePoint(Vector2Int cell)
        {
            IReadOnlyList<StrategyPointOfInterest> points = resourcePoints?.Points;
            if (points == null)
            {
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                StrategyPointOfInterest point = points[i];
                if (point == null)
                {
                    continue;
                }

                Vector2Int markerDelta = point.Cell - cell;
                if (Mathf.Max(Mathf.Abs(markerDelta.x), Mathf.Abs(markerDelta.y)) <= ResourceSeparation
                    || point.HasMineralSite
                    && StrategyPointOfInterestPlacement.DistanceToFootprint(
                        cell,
                        point.MineralOrigin,
                        StrategyPointOfInterestPlacement.MineralFootprint) <= ResourceSeparation)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasForageAt(Vector2Int cell)
        {
            StrategyForageResourceController forage = StrategyForageResourceController.Active;
            if (forage == null)
            {
                return false;
            }

            IReadOnlyList<StrategyForageNode> nodes = forage.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryCreateAnchor(
            string stableId,
            Vector2Int cell,
            StrategyStoryPointOfInterestState state,
            string definitionId,
            int sequenceIndex,
            int committedResidentId,
            StrategyStoryPointOfInterestDistanceTier distanceTier,
            out StrategyStoryPointOfInterestAnchor createdAnchor)
        {
            createdAnchor = null;
            EnsureAnchorRoot();
            GameObject anchorObject = null;
            try
            {
                Vector3 world = map.GetCellCenterWorld(cell.x, cell.y);
                anchorObject = new GameObject("Story Point " + stableId);
                anchorObject.transform.SetParent(anchorRoot, false);
                anchorObject.transform.position = new Vector3(world.x, world.y, -0.105f);
                SpriteRenderer renderer = anchorObject.AddComponent<SpriteRenderer>();
                StrategyStoryPointOfInterestAnchor anchor =
                    anchorObject.AddComponent<StrategyStoryPointOfInterestAnchor>();
                anchor.Configure(
                    map,
                    catalog,
                    stableId,
                    cell,
                    distanceTier,
                    state,
                    definitionId,
                    sequenceIndex,
                    committedResidentId,
                    renderer);
                anchors.Add(anchor);
                createdAnchor = anchor;
                return true;
            }
            catch (Exception exception)
            {
                if (anchorObject != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(anchorObject);
                    }
                    else
                    {
                        DestroyImmediate(anchorObject);
                    }
                }

                StrategyDebugLogger.Error(
                    "StoryPointOfInterest",
                    "AnchorCreateFailed",
                    StrategyDebugLogger.F("id", stableId),
                    StrategyDebugLogger.F("cell", cell),
                    StrategyDebugLogger.F("error", exception.GetType().Name));
                return false;
            }
        }

        private void ClearAnchorObjects()
        {
            latentCandidates.Clear();
            for (int i = anchors.Count - 1; i >= 0; i--)
            {
                StrategyStoryPointOfInterestAnchor anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                anchor.ReleaseMapBuildability();
                if (Application.isPlaying)
                {
                    Destroy(anchor.gameObject);
                }
                else
                {
                    DestroyImmediate(anchor.gameObject);
                }
            }

            anchors.Clear();
        }

        private void EnsureAnchorRoot()
        {
            if (anchorRoot != null)
            {
                return;
            }

            GameObject root = new("Story Points Of Interest");
            root.transform.SetParent(transform, false);
            anchorRoot = root.transform;
        }

        private void OnDestroy()
        {
            CancelPendingNotices();
            ClearAnchorObjects();
            if (Active == this)
            {
                Active = null;
            }
        }

        private readonly struct StoryNotice
        {
            public StoryNotice(string definitionId, string title, string body)
            {
                DefinitionId = definitionId;
                Title = title;
                Body = body;
            }

            public string DefinitionId { get; }
            public string Title { get; }
            public string Body { get; }
        }
    }
}
