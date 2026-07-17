using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStoryPointOfInterestController
    {
        internal bool TryGetActivationCandidate(
            StrategyResidentAgent resident,
            out StrategyStoryPointOfInterestAnchor anchor)
        {
            anchor = null;
            if (!configured
                || resident == null
                || nextSequenceIndex < 0
                || catalog == null
                || nextSequenceIndex >= catalog.Count
                || !TryFindDeterministicActivationWinner(out StrategyResidentAgent winner, out anchor)
                || winner != resident)
            {
                anchor = null;
                return false;
            }

            return true;
        }

        internal bool TryCommitActivation(
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident)
        {
            if (anchor == null
                || resident == null
                || !anchor.IsLatent
                || nextSequenceIndex < 0
                || catalog == null
                || nextSequenceIndex >= catalog.Count)
            {
                return false;
            }

            StrategyStoryPointOfInterestDefinition definition =
                catalog.Definitions[nextSequenceIndex];
            int sequenceIndex = nextSequenceIndex;
            if (!anchor.TryCommit(definition, sequenceIndex, resident))
            {
                return false;
            }

            nextSequenceIndex++;
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "Committed",
                StrategyDebugLogger.F("anchorId", anchor.StableId),
                StrategyDebugLogger.F("definitionId", definition.Id),
                StrategyDebugLogger.F("sequenceIndex", sequenceIndex),
                StrategyDebugLogger.F("residentId", resident.ResidentId));
            return true;
        }

        internal bool TryClaimAssignedTarget(
            StrategyResidentAgent resident,
            out StrategyStoryPointOfInterestAnchor anchor)
        {
            anchor = null;
            if (!configured || resident == null)
            {
                return false;
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                StrategyStoryPointOfInterestAnchor candidate = anchors[i];
                if (candidate != null
                    && candidate.State == StrategyStoryPointOfInterestState.Committed
                    && candidate.CommittedResidentId == resident.ResidentId
                    && candidate.TryBindCommittedResident(resident))
                {
                    anchor = candidate;
                    return true;
                }
            }

            if (!IsLowestEligibleScout(resident))
            {
                return false;
            }

            StrategyStoryPointOfInterestAnchor best = null;
            for (int i = 0; i < anchors.Count; i++)
            {
                StrategyStoryPointOfInterestAnchor candidate = anchors[i];
                if (candidate == null
                    || candidate.State != StrategyStoryPointOfInterestState.Materialized
                    || best != null && candidate.SequenceIndex >= best.SequenceIndex)
                {
                    continue;
                }

                best = candidate;
            }

            if (best == null || !best.TryClaimMaterialized(resident))
            {
                return false;
            }

            anchor = best;
            return true;
        }

        internal void ReleaseCommitment(
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident)
        {
            if (anchor == null || resident == null || !anchors.Contains(anchor))
            {
                return;
            }

            anchor.ReleaseCommitment(resident);
        }

        internal bool BeginInvestigationEncounter(
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident,
            System.Action<bool> onCompleted)
        {
            if (anchor == null
                || resident == null
                || !anchors.Contains(anchor)
                || !TryGetDefinition(anchor, out StrategyStoryPointOfInterestDefinition definition))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definition.EncounterId)
                && encounters.TryGetValue(
                    definition.EncounterId,
                    out IStrategyStoryPointOfInterestEncounter encounter))
            {
                return encounter.TryBegin(
                    definition,
                    anchor,
                    resident,
                    outcome => ResolveEncounter(
                        anchor,
                        resident,
                        definition,
                        outcome,
                        onCompleted));
            }

            if (!anchor.MarkResolved(resident))
            {
                return false;
            }

            pendingNotices.Enqueue(new StoryNotice(
                definition.Id,
                definition.Title,
                definition.Body));
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                "Resolved",
                StrategyDebugLogger.F("anchorId", anchor.StableId),
                StrategyDebugLogger.F("definitionId", definition.Id),
                StrategyDebugLogger.F("encounterId", definition.EncounterId),
                StrategyDebugLogger.F("residentId", resident.ResidentId));
            TryShowNextNotice();
            onCompleted?.Invoke(true);
            return true;
        }

        private void ResolveEncounter(
            StrategyStoryPointOfInterestAnchor anchor,
            StrategyResidentAgent resident,
            StrategyStoryPointOfInterestDefinition definition,
            StrategyStoryPointOfInterestOutcome outcome,
            System.Action<bool> onCompleted)
        {
            bool resolved = anchor != null
                && resident != null
                && anchors.Contains(anchor)
                && anchor.MarkResolved(resident);
            StrategyDebugLogger.Info(
                "StoryPointOfInterest",
                resolved ? "Resolved" : "ResolutionRejected",
                StrategyDebugLogger.F("anchorId", anchor != null ? anchor.StableId : string.Empty),
                StrategyDebugLogger.F("definitionId", definition.Id),
                StrategyDebugLogger.F("encounterId", definition.EncounterId),
                StrategyDebugLogger.F("outcome", outcome),
                StrategyDebugLogger.F("residentId", resident != null ? resident.ResidentId : 0));
            onCompleted?.Invoke(resolved);
        }

        private bool TryFindDeterministicActivationWinner(
            out StrategyResidentAgent bestResident,
            out StrategyStoryPointOfInterestAnchor bestAnchor)
        {
            bestResident = null;
            bestAnchor = null;
            bool found = false;
            long bestDistanceSquared = long.MaxValue;
            int residentCount = population?.Residents?.Count ?? 0;
            for (int residentIndex = 0; residentIndex < residentCount; residentIndex++)
            {
                StrategyResidentAgent resident = population.Residents[residentIndex];
                if (resident == null
                    || !resident.IsEligibleStoryPointActivationScout
                    || !resident.TryGetStoryPointActivationCell(out Vector2Int residentCell))
                {
                    continue;
                }

                for (int anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
                {
                    StrategyStoryPointOfInterestAnchor anchor = anchors[anchorIndex];
                    if (!CanActivateAnchor(anchor, residentCell, out long distanceSquared)
                        || !StrategyStoryPointOfInterestActivationPolicy.IsBetterCandidate(
                            found,
                            distanceSquared,
                            anchor.StableId,
                            resident.ResidentId,
                            bestDistanceSquared,
                            bestAnchor != null ? bestAnchor.StableId : string.Empty,
                            bestResident != null ? bestResident.ResidentId : int.MaxValue))
                    {
                        continue;
                    }

                    found = true;
                    bestResident = resident;
                    bestAnchor = anchor;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return found;
        }

        private bool CanActivateAnchor(
            StrategyStoryPointOfInterestAnchor anchor,
            Vector2Int residentCell,
            out long distanceSquared)
        {
            distanceSquared = long.MaxValue;
            if (anchor == null
                || !anchor.IsLatent
                || fog.IsCellPersistentlyExplored(anchor.Cell)
                || fog.IsCellVisibleAtDaylightRange(anchor.Cell))
            {
                return false;
            }

            long deltaX = (long)anchor.Cell.x - residentCell.x;
            long deltaY = (long)anchor.Cell.y - residentCell.y;
            distanceSquared = deltaX * deltaX + deltaY * deltaY;
            return StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(
                distanceSquared,
                fog.ResidentDaylightVisibleOuterRadius);
        }

        private bool IsLowestEligibleScout(StrategyResidentAgent resident)
        {
            int residentCount = population?.Residents?.Count ?? 0;
            for (int i = 0; i < residentCount; i++)
            {
                StrategyResidentAgent other = population.Residents[i];
                if (other != null
                    && other != resident
                    && other.IsScoutExploring
                    && other.ResidentId > 0
                    && other.ResidentId < resident.ResidentId)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
