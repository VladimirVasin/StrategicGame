using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStoryPointOfInterestController
    {
        public void CapturePersistentState(
            List<StrategyStoryPointOfInterestSaveData> target,
            out int savedNextSequenceIndex)
        {
            savedNextSequenceIndex = nextSequenceIndex;
            if (target == null)
            {
                return;
            }

            target.Clear();
            List<StrategyStoryPointOfInterestAnchor> sorted = new(anchors);
            sorted.Sort((left, right) => string.CompareOrdinal(left?.StableId, right?.StableId));
            for (int i = 0; i < sorted.Count; i++)
            {
                StrategyStoryPointOfInterestAnchor anchor = sorted[i];
                if (anchor == null)
                {
                    continue;
                }

                if (anchor.State == StrategyStoryPointOfInterestState.Latent)
                {
                    continue;
                }

                target.Add(new StrategyStoryPointOfInterestSaveData
                {
                    stableId = anchor.StableId,
                    cellX = anchor.Cell.x,
                    cellY = anchor.Cell.y,
                    state = (int)anchor.State,
                    definitionId = anchor.DefinitionId,
                    sequenceIndex = anchor.SequenceIndex,
                    committedResidentId = anchor.CommittedResidentId
                });
            }
        }

        public void ClearForLoad()
        {
            CancelPendingNotices();
            ClearAnchorObjects();
            nextSequenceIndex = 0;
        }

        public void RestorePersistentState(
            IReadOnlyList<StrategyStoryPointOfInterestSaveData> savedAnchors,
            int savedNextSequenceIndex)
        {
            RestorePersistentState(savedAnchors, savedNextSequenceIndex, true);
        }

        internal void RestorePersistentState(
            IReadOnlyList<StrategyStoryPointOfInterestSaveData> savedAnchors,
            int savedNextSequenceIndex,
            bool rebuildCandidates)
        {
            ClearForLoad();
            if (!configured || map == null)
            {
                return;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<Vector2Int> cells = new();
            bool failed = savedNextSequenceIndex < 0
                || catalog == null
                || savedNextSequenceIndex > catalog.Count;
            int savedCount = savedAnchors?.Count ?? 0;
            int discardedLegacyLatent = 0;
            for (int i = 0; i < savedCount && !failed; i++)
            {
                StrategyStoryPointOfInterestSaveData saved = savedAnchors[i];
                if (saved == null)
                {
                    failed = true;
                    break;
                }

                Vector2Int cell = new(saved.cellX, saved.cellY);
                string stableId = string.IsNullOrWhiteSpace(saved.stableId)
                    ? StrategyStoryPointOfInterestAnchor.BuildStableId(cell)
                    : saved.stableId;
                StrategyStoryPointOfInterestState state =
                    (StrategyStoryPointOfInterestState)saved.state;
                if (state == StrategyStoryPointOfInterestState.Latent)
                {
                    discardedLegacyLatent++;
                    continue;
                }

                if (catalog == null
                    || !catalog.TryGet(
                        saved.definitionId,
                        out StrategyStoryPointOfInterestDefinition definition))
                {
                    failed = true;
                    break;
                }

                if (!ids.Add(stableId)
                    || !cells.Add(cell)
                    || !map.IsCellWalkable(cell)
                    || resourcePoints?.HasPointAt(cell) == true
                    || !TryCreateAnchor(
                        stableId,
                        cell,
                        state,
                        saved.definitionId,
                        saved.sequenceIndex,
                        saved.committedResidentId,
                        definition.DistanceTier,
                        out _))
                {
                    failed = true;
                }
            }

            if (failed)
            {
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "RestoreRolledBack",
                    StrategyDebugLogger.F("saved", savedCount),
                    StrategyDebugLogger.F("restored", anchors.Count));
                GenerateDefaultAnchors();
                return;
            }

            nextSequenceIndex = savedNextSequenceIndex;
            if (rebuildCandidates)
            {
                RebuildLatentCandidates();
            }

            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "Restored",
                StrategyDebugLogger.F("anchors", anchors.Count),
                StrategyDebugLogger.F("candidates", latentCandidates.Count),
                StrategyDebugLogger.F("discardedLegacyLatent", discardedLegacyLatent),
                StrategyDebugLogger.F("nextSequenceIndex", nextSequenceIndex));
        }
    }
}
