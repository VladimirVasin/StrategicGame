using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    [DisallowMultipleComponent]
    public sealed partial class StrategyStoryPointOfInterestController : MonoBehaviour
    {
        private const int SpareAnchorCount = 2;
        private const int PlacementSeedSalt = 4187;

        private readonly List<StrategyStoryPointOfInterestAnchor> anchors = new();
        private readonly Queue<StoryNotice> pendingNotices = new();

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

        private void GenerateDefaultAnchors()
        {
            ClearAnchorObjects();
            nextSequenceIndex = 0;
            if (!configured || catalog == null || catalog.Count <= 0)
            {
                return;
            }

            int targetCount = catalog.Count + SpareAnchorCount;
            List<Vector2Int> cells = StrategyPointOfInterestPlacement.SelectCells(
                map.Width,
                map.Height,
                map.ActiveSeed ^ PlacementSeedSalt,
                campCell,
                targetCount,
                map.IsCellWalkable,
                CanUseStoryAnchorCell);
            for (int i = 0; i < cells.Count; i++)
            {
                TryCreateAnchor(
                    StrategyStoryPointOfInterestAnchor.BuildStableId(cells[i]),
                    cells[i],
                    StrategyStoryPointOfInterestState.Latent,
                    string.Empty,
                    -1,
                    0);
            }

            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "AnchorsGenerated",
                StrategyDebugLogger.F("anchors", anchors.Count),
                StrategyDebugLogger.F("target", targetCount),
                StrategyDebugLogger.F("definitions", catalog.Count));
        }

        private bool CanUseStoryAnchorCell(Vector2Int cell)
        {
            return map != null
                && map.IsCellWalkable(cell)
                && map.IsCellBuildable(cell)
                && resourcePoints?.HasPointAt(cell) != true
                && !HasForageAt(cell);
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
            int committedResidentId)
        {
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
                    stableId,
                    cell,
                    state,
                    definitionId,
                    sequenceIndex,
                    committedResidentId,
                    renderer);
                anchors.Add(anchor);
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
