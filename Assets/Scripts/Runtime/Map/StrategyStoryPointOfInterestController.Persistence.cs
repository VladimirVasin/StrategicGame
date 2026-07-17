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
            ClearForLoad();
            if (!configured || map == null)
            {
                return;
            }

            if (savedAnchors == null || savedAnchors.Count <= 0)
            {
                GenerateDefaultAnchors();
                return;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<Vector2Int> cells = new();
            bool failed = savedNextSequenceIndex < 0
                || catalog == null
                || savedNextSequenceIndex > catalog.Count;
            for (int i = 0; i < savedAnchors.Count && !failed; i++)
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
                        saved.committedResidentId))
                {
                    failed = true;
                }
            }

            if (failed)
            {
                StrategyDebugLogger.Warn(
                    "StoryPointOfInterest",
                    "RestoreRolledBack",
                    StrategyDebugLogger.F("saved", savedAnchors.Count),
                    StrategyDebugLogger.F("restored", anchors.Count));
                GenerateDefaultAnchors();
                return;
            }

            nextSequenceIndex = savedNextSequenceIndex;
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "Restored",
                StrategyDebugLogger.F("anchors", anchors.Count),
                StrategyDebugLogger.F("nextSequenceIndex", nextSequenceIndex));
        }
    }
}
