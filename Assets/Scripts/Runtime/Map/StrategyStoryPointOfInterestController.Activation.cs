using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStoryPointOfInterestController
    {
        internal bool TryGetActivationCandidate(
            StrategyResidentAgent resident,
            out StrategyStoryPointOfInterestCandidatePlan candidate)
        {
            candidate = default;
            if (!configured
                || resident == null
                || nextSequenceIndex < 0
                || catalog == null
                || nextSequenceIndex >= catalog.Count
                || !TryFindDeterministicActivationWinner(out StrategyResidentAgent winner, out candidate)
                || winner != resident)
            {
                candidate = default;
                return false;
            }

            return true;
        }

        internal bool TryCommitActivation(
            StrategyStoryPointOfInterestCandidatePlan candidate,
            StrategyResidentAgent resident,
            out StrategyStoryPointOfInterestAnchor anchor)
        {
            anchor = null;
            if (resident == null
                || nextSequenceIndex < 0
                || catalog == null
                || nextSequenceIndex >= catalog.Count
                || !ContainsCandidate(candidate))
            {
                return false;
            }

            StrategyStoryPointOfInterestDefinition definition =
                catalog.Definitions[nextSequenceIndex];
            int sequenceIndex = nextSequenceIndex;
            if (candidate.DistanceTier != definition.DistanceTier
                || !CanUseStoryCandidateCell(candidate.Cell)
                || !TryCreateAnchor(
                    StrategyStoryPointOfInterestAnchor.BuildStableId(candidate.Cell),
                    candidate.Cell,
                    StrategyStoryPointOfInterestState.Committed,
                    definition.Id,
                    sequenceIndex,
                    resident.ResidentId,
                    definition.DistanceTier,
                    out anchor)
                || !anchor.TryBindCommittedResident(resident))
            {
                return false;
            }

            RemoveCandidate(candidate);
            nextSequenceIndex++;
            PruneCandidatesWithoutRemainingDefinitions();
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
            out StrategyStoryPointOfInterestCandidatePlan bestCandidate)
        {
            bestResident = null;
            bestCandidate = default;
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

                for (int candidateIndex = 0; candidateIndex < latentCandidates.Count; candidateIndex++)
                {
                    StrategyStoryPointOfInterestCandidatePlan candidate = latentCandidates[candidateIndex];
                    string candidateId = StrategyStoryPointOfInterestAnchor.BuildStableId(candidate.Cell);
                    if (!CanActivateCandidate(candidate, residentCell, out long distanceSquared)
                        || !StrategyStoryPointOfInterestActivationPolicy.IsBetterCandidate(
                            found,
                            distanceSquared,
                            candidateId,
                            resident.ResidentId,
                            bestDistanceSquared,
                            found
                                ? StrategyStoryPointOfInterestAnchor.BuildStableId(bestCandidate.Cell)
                                : string.Empty,
                            bestResident != null ? bestResident.ResidentId : int.MaxValue))
                    {
                        continue;
                    }

                    found = true;
                    bestResident = resident;
                    bestCandidate = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return found;
        }

        private bool CanActivateCandidate(
            StrategyStoryPointOfInterestCandidatePlan candidate,
            Vector2Int residentCell,
            out long distanceSquared)
        {
            distanceSquared = long.MaxValue;
            if (catalog == null
                || nextSequenceIndex < 0
                || nextSequenceIndex >= catalog.Count
                || candidate.DistanceTier != catalog.Definitions[nextSequenceIndex].DistanceTier
                || !CanUseStoryCandidateCell(candidate.Cell))
            {
                return false;
            }

            long deltaX = (long)candidate.Cell.x - residentCell.x;
            long deltaY = (long)candidate.Cell.y - residentCell.y;
            distanceSquared = deltaX * deltaX + deltaY * deltaY;
            return StrategyStoryPointOfInterestActivationPolicy.IsInsideActivationBand(
                distanceSquared,
                fog.ResidentDaylightVisibleOuterRadius);
        }

        private bool ContainsCandidate(StrategyStoryPointOfInterestCandidatePlan candidate)
        {
            for (int i = 0; i < latentCandidates.Count; i++)
            {
                if (latentCandidates[i].Cell == candidate.Cell
                    && latentCandidates[i].DistanceTier == candidate.DistanceTier)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveCandidate(StrategyStoryPointOfInterestCandidatePlan candidate)
        {
            for (int i = latentCandidates.Count - 1; i >= 0; i--)
            {
                if (latentCandidates[i].Cell == candidate.Cell
                    && latentCandidates[i].DistanceTier == candidate.DistanceTier)
                {
                    latentCandidates.RemoveAt(i);
                    return;
                }
            }
        }

        private void PruneCandidatesWithoutRemainingDefinitions()
        {
            for (int i = latentCandidates.Count - 1; i >= 0; i--)
            {
                if (CountRemainingDefinitions(latentCandidates[i].DistanceTier) <= 0)
                {
                    latentCandidates.RemoveAt(i);
                }
            }
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
