using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public enum StrategyStoryPointOfInterestState
    {
        Latent = 0,
        Materialized = 1,
        Committed = 2,
        Resolved = 3
    }

    [DisallowMultipleComponent]
    public sealed class StrategyStoryPointOfInterestAnchor : MonoBehaviour
    {
        private CityMapController map;
        private StrategyStoryPointOfInterestCatalog catalog;
        private SpriteRenderer spriteRenderer;
        private StrategyResidentAgent committedResident;
        private bool buildabilityBlocked;

        public string StableId { get; private set; } = string.Empty;
        public Vector2Int Cell { get; private set; }
        public StrategyStoryPointOfInterestState State { get; private set; }
        public string DefinitionId { get; private set; } = string.Empty;
        public int SequenceIndex { get; private set; } = -1;
        public int CommittedResidentId { get; private set; }
        public bool IsLatent => State == StrategyStoryPointOfInterestState.Latent;
        public bool IsResolved => State == StrategyStoryPointOfInterestState.Resolved;

        internal void Configure(
            CityMapController mapController,
            StrategyStoryPointOfInterestCatalog storyCatalog,
            string stableId,
            Vector2Int cell,
            StrategyStoryPointOfInterestState state,
            string definitionId,
            int sequenceIndex,
            int committedResidentId,
            SpriteRenderer renderer)
        {
            ReleaseMapBuildability();
            map = mapController;
            catalog = storyCatalog;
            StableId = string.IsNullOrWhiteSpace(stableId) ? BuildStableId(cell) : stableId;
            Cell = cell;
            State = state;
            DefinitionId = definitionId ?? string.Empty;
            SequenceIndex = sequenceIndex;
            CommittedResidentId = committedResidentId;
            committedResident = null;
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            BlockMapBuildability();
            RefreshVisual();
        }

        internal bool TryCommit(
            StrategyStoryPointOfInterestDefinition definition,
            int sequenceIndex,
            StrategyResidentAgent resident)
        {
            if (definition == null
                || resident == null
                || State != StrategyStoryPointOfInterestState.Latent)
            {
                return false;
            }

            DefinitionId = definition.Id;
            SequenceIndex = sequenceIndex;
            State = StrategyStoryPointOfInterestState.Committed;
            committedResident = resident;
            CommittedResidentId = resident.ResidentId;
            RefreshVisual();
            return true;
        }

        internal bool TryClaimMaterialized(StrategyResidentAgent resident)
        {
            if (resident == null
                || State != StrategyStoryPointOfInterestState.Materialized
                || string.IsNullOrWhiteSpace(DefinitionId))
            {
                return false;
            }

            State = StrategyStoryPointOfInterestState.Committed;
            committedResident = resident;
            CommittedResidentId = resident.ResidentId;
            RefreshVisual();
            return true;
        }

        internal bool TryBindCommittedResident(StrategyResidentAgent resident)
        {
            if (resident == null
                || State != StrategyStoryPointOfInterestState.Committed
                || CommittedResidentId != resident.ResidentId)
            {
                return false;
            }

            committedResident = resident;
            return true;
        }

        public bool IsCommittedTo(StrategyResidentAgent resident)
        {
            return resident != null
                && State == StrategyStoryPointOfInterestState.Committed
                && CommittedResidentId == resident.ResidentId
                && (committedResident == null || committedResident == resident);
        }

        internal SpriteRenderer VisualRenderer => spriteRenderer;

        internal void ReleaseCommitment(StrategyResidentAgent resident)
        {
            if (!IsCommittedTo(resident))
            {
                return;
            }

            State = StrategyStoryPointOfInterestState.Materialized;
            committedResident = null;
            CommittedResidentId = 0;
            RefreshVisual();
        }

        internal bool MarkResolved(StrategyResidentAgent resident)
        {
            if (!IsCommittedTo(resident))
            {
                return false;
            }

            State = StrategyStoryPointOfInterestState.Resolved;
            committedResident = null;
            CommittedResidentId = 0;
            RefreshVisual();
            return true;
        }

        internal void ReleaseMapBuildability()
        {
            if (!buildabilityBlocked)
            {
                return;
            }

            map?.SetCellsBuildable(Cell, Vector2Int.one, true);
            buildabilityBlocked = false;
        }

        internal static string BuildStableId(Vector2Int cell)
        {
            return "story-anchor-" + cell.x + "-" + cell.y;
        }

        private void BlockMapBuildability()
        {
            if (map == null || buildabilityBlocked)
            {
                return;
            }

            map.SetCellsBuildable(Cell, Vector2Int.one, false);
            buildabilityBlocked = true;
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool visible = State != StrategyStoryPointOfInterestState.Latent;
            spriteRenderer.enabled = visible;
            if (!visible)
            {
                return;
            }

            Sprite authored = LoadAuthoredSprite();
            spriteRenderer.sprite = authored != null
                ? authored
                : StrategyPointOfInterestSpriteFactory.GetSprite(IsResolved);
            spriteRenderer.color = authored != null
                ? IsResolved
                    ? new Color(0.88f, 0.90f, 0.86f, 0.96f)
                    : Color.white
                : IsResolved
                    ? new Color(0.82f, 0.92f, 0.84f, 0.92f)
                    : State == StrategyStoryPointOfInterestState.Committed
                        ? new Color(1f, 0.82f, 0.48f, 1f)
                        : new Color(0.82f, 0.68f, 1f, 1f);
            StrategyWorldSorting.Apply(spriteRenderer, transform.position, 1);
        }

        private Sprite LoadAuthoredSprite()
        {
            if (catalog == null
                || string.IsNullOrWhiteSpace(DefinitionId)
                || !catalog.TryGet(DefinitionId, out StrategyStoryPointOfInterestDefinition definition))
            {
                return null;
            }

            string path = IsResolved
                ? definition.ResolvedSpriteResourcePath
                : definition.UnresolvedSpriteResourcePath;
            if (string.IsNullOrWhiteSpace(path) && IsResolved)
            {
                path = definition.UnresolvedSpriteResourcePath;
            }

            return string.IsNullOrWhiteSpace(path) ? null : Resources.Load<Sprite>(path);
        }

        private void OnDestroy()
        {
            ReleaseMapBuildability();
        }
    }
}
