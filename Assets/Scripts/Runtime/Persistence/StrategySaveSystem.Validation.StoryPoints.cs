using System;
using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategySaveSystem
    {
        private static bool ValidateStoryPointsOfInterest(
            StrategySaveData data,
            out string reason)
        {
            if (data.nextStoryPointOfInterestSequenceIndex < 0
                || data.nextStoryPointOfInterestSequenceIndex > MaxSaveStoryPointsOfInterest)
            {
                reason = "invalid_story_point_sequence_index";
                return false;
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<long> cells = new();
            HashSet<int> sequenceIndices = new();
            HashSet<int> residentIds = new();
            HashSet<int> activeScoutIds = new();
            for (int i = 0; i < data.residents.Count; i++)
            {
                if (data.residents[i] != null && data.residents[i].residentId > 0)
                {
                    residentIds.Add(data.residents[i].residentId);
                }
            }

            for (int i = 0; i < data.scoutLodges.Count; i++)
            {
                StrategyScoutLodgeSaveData lodge = data.scoutLodges[i];
                if (lodge != null
                    && lodge.residentId > 0
                    && lodge.expeditionState == (int)StrategyScoutExpeditionState.Exploring)
                {
                    activeScoutIds.Add(lodge.residentId);
                }
            }

            HashSet<int> committedResidentIds = new();
            for (int i = 0; i < data.storyPointsOfInterest.Count; i++)
            {
                StrategyStoryPointOfInterestSaveData point = data.storyPointsOfInterest[i];
                if (point == null
                    || string.IsNullOrWhiteSpace(point.stableId)
                    || point.stableId.Length > 128
                    || !ids.Add(point.stableId))
                {
                    reason = "invalid_or_duplicate_story_point_id_" + i;
                    return false;
                }

                long cellKey = (long)point.cellY * data.mapWidth + point.cellX;
                if (!IsCellInside(point.cellX, point.cellY, data.mapWidth, data.mapHeight)
                    || !cells.Add(cellKey)
                    || OverlapsSavedWorld(point.cellX, point.cellY, data)
                    || HasResourcePointAt(data.pointsOfInterest, point.cellX, point.cellY))
                {
                    reason = "invalid_or_occupied_story_point_cell_" + i;
                    return false;
                }

                if (!Enum.IsDefined(typeof(StrategyStoryPointOfInterestState), point.state))
                {
                    reason = "invalid_story_point_state_" + i;
                    return false;
                }

                StrategyStoryPointOfInterestState state =
                    (StrategyStoryPointOfInterestState)point.state;
                if (state == StrategyStoryPointOfInterestState.Latent)
                {
                    if (!string.IsNullOrEmpty(point.definitionId)
                        || point.sequenceIndex != -1
                        || point.committedResidentId != 0)
                    {
                        reason = "invalid_latent_story_point_" + i;
                        return false;
                    }

                    continue;
                }

                if (!StrategyStoryPointOfInterestDefinition.IsValidId(point.definitionId)
                    || point.sequenceIndex < 0
                    || point.sequenceIndex >= data.nextStoryPointOfInterestSequenceIndex
                    || !sequenceIndices.Add(point.sequenceIndex))
                {
                    reason = "invalid_story_point_definition_or_sequence_" + i;
                    return false;
                }

                bool committed = state == StrategyStoryPointOfInterestState.Committed;
                if (committed
                    && (point.committedResidentId <= 0
                        || !residentIds.Contains(point.committedResidentId)
                        || !activeScoutIds.Contains(point.committedResidentId)
                        || !committedResidentIds.Add(point.committedResidentId))
                    || !committed && point.committedResidentId != 0)
                {
                    reason = "invalid_story_point_commitment_" + i;
                    return false;
                }
            }

            if (sequenceIndices.Count != data.nextStoryPointOfInterestSequenceIndex)
            {
                reason = "story_point_sequence_has_gaps";
                return false;
            }

            for (int i = 0; i < data.nextStoryPointOfInterestSequenceIndex; i++)
            {
                if (!sequenceIndices.Contains(i))
                {
                    reason = "story_point_sequence_has_gaps";
                    return false;
                }
            }

            for (int i = 0; i < data.scoutLodges.Count; i++)
            {
                StrategyScoutLodgeSaveData lodge = data.scoutLodges[i];
                if (lodge != null
                    && lodge.returnAfterStoryPoint
                    && (lodge.expeditionState != (int)StrategyScoutExpeditionState.Exploring
                        || !committedResidentIds.Contains(lodge.residentId)))
                {
                    reason = "invalid_scout_story_return_commitment_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        internal static bool ValidateStoryPointsAgainstCatalog(
            IReadOnlyList<StrategyStoryPointOfInterestSaveData> points,
            int nextSequenceIndex,
            StrategyStoryPointOfInterestCatalog catalog,
            out string reason)
        {
            if (catalog == null)
            {
                reason = "missing_story_point_catalog";
                return false;
            }

            if (nextSequenceIndex < 0 || nextSequenceIndex > catalog.Count)
            {
                reason = "story_point_sequence_exceeds_catalog";
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                StrategyStoryPointOfInterestSaveData point = points[i];
                if (point.state == (int)StrategyStoryPointOfInterestState.Latent)
                {
                    continue;
                }

                if (point.sequenceIndex < 0
                    || point.sequenceIndex >= catalog.Count
                    || !catalog.TryGet(
                        point.definitionId,
                        out StrategyStoryPointOfInterestDefinition definition)
                    || catalog.Definitions[point.sequenceIndex] != definition)
                {
                    reason = "unknown_or_reordered_story_point_" + i;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private static bool HasResourcePointAt(
            IReadOnlyList<StrategyPointOfInterestSaveData> points,
            int x,
            int y)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != null && points[i].cellX == x && points[i].cellY == y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
