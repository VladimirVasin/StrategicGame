using System.Collections.Generic;
using UnityEngine;
namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const int ChildPairPlaySearchRadius = 8;
        private const int ChildPairMeetingSearchRadius = 3;
        private const int ChildTagMoveRadius = 3;
        private const float ChildPlayCooldownMin = 5.0f;
        private const float ChildPlayCooldownMax = 12.0f;
        private const float ChildSoloPlaySecondsMin = 3.5f;
        private const float ChildSoloPlaySecondsMax = 7.0f;
        private const float ChildPairPlaySecondsMin = 5.0f;
        private const float ChildPairPlaySecondsMax = 10.0f;
        private ChildPlayKind childPlayKind;
        private StrategyResidentAgent childPlayPartner;
        private Vector2Int childPlayCenterCell;
        private float childPlayCooldown;
        private float childPlayTimer;
        private float childTagStepCooldown;
        private int childTagMovesRemaining;
        private enum ChildPlayKind
        {
            None,
            Solo,
            Pair,
            Tag
        }
        private bool TryStartChildIdleActivity()
        {
            if (!CanStartChildPlayFromIdle() || childPlayCooldown > 0f)
            {
                return false;
            }
            if (Random.value < GetChildPairPlayChance() && TryStartChildPairPlay())
            {
                return true;
            }
            return TryStartChildSoloPlay();
        }
        private bool TryStartChildSoloPlay()
        {
            for (int attempt = 0; attempt < 14; attempt++)
            {
                Vector2Int cell = GetRandomChildPlayCell();
                if (!map.IsCellWalkable(cell) || !TryBuildPathTo(cell))
                {
                    continue;
                }
                childPlayKind = ChildPlayKind.Solo;
                childPlayPartner = null;
                childPlayCenterCell = cell;
                childPlayTimer = Random.Range(ChildSoloPlaySecondsMin, ChildSoloPlaySecondsMax);
                childTagMovesRemaining = 0;
                childTagStepCooldown = 0f;
                activity = ResidentActivity.MovingToChildPlay;
                hasTarget = path.Count > 0;
                waitTimer = 0f;
                return true;
            }
            childPlayCooldown = Random.Range(1.5f, 3.5f);
            return false;
        }
        private bool TryStartChildPairPlay()
        {
            if (population == null || population.Residents == null)
            {
                return false;
            }
            List<ChildPlayCandidate> candidates = new();
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent candidate = residents[i];
                if (!candidate.CanAcceptChildPlayInvitation(this)
                    || !TryGetPathStartCell(out Vector2Int selfCell)
                    || !candidate.TryGetPathStartCell(out Vector2Int candidateCell))
                {
                    continue;
                }
                int distance = GetChebyshevDistance(selfCell, candidateCell);
                if (distance > ChildPairPlaySearchRadius)
                {
                    continue;
                }
                int score = distance * 10 + Random.Range(0, 4);
                if (home != null && candidate.home == home)
                {
                    score -= 18;
                }
                if (IsSiblingOf(candidate))
                {
                    score -= 12;
                }
                candidates.Add(new ChildPlayCandidate(candidate, selfCell, candidateCell, score));
            }
            candidates.Sort((a, b) => a.Score.CompareTo(b.Score));
            int checks = Mathf.Min(candidates.Count, 6);
            for (int i = 0; i < checks; i++)
            {
                ChildPlayCandidate candidate = candidates[i];
                if (TryPrepareChildPairPaths(
                    candidate.Resident,
                    candidate.SelfCell,
                    candidate.PartnerCell,
                    out Vector2Int selfTarget,
                    out Vector2Int partnerTarget))
                {
                    StartChildPairPlay(candidate.Resident, selfTarget, partnerTarget);
                    return true;
                }
            }
            return false;
        }
        private bool TryPrepareChildPairPaths(
            StrategyResidentAgent partner,
            Vector2Int selfCell,
            Vector2Int partnerCell,
            out Vector2Int selfTarget,
            out Vector2Int partnerTarget)
        {
            selfTarget = default;
            partnerTarget = default;
            Vector2Int midpoint = new Vector2Int(
                Mathf.RoundToInt((selfCell.x + partnerCell.x) * 0.5f),
                Mathf.RoundToInt((selfCell.y + partnerCell.y) * 0.5f));
            for (int radius = 0; radius <= ChildPairMeetingSearchRadius; radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                        {
                            continue;
                        }
                        Vector2Int center = midpoint + new Vector2Int(x, y);
                        if (!IsCellInChildPlayRange(center) || !map.IsCellWalkable(center))
                        {
                            continue;
                        }

                        for (int i = 0; i < CardinalDirections.Length; i++)
                        {
                            Vector2Int other = center + CardinalDirections[i];
                            if (!partner.IsCellInChildPlayRange(other)
                                || !map.IsCellWalkable(other)
                                || !TryBuildPathTo(center)
                                || !partner.TryBuildPathTo(other))
                            {
                                continue;
                            }

                            selfTarget = center;
                            partnerTarget = other;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void StartChildPairPlay(
            StrategyResidentAgent partner,
            Vector2Int selfTarget,
            Vector2Int partnerTarget)
        {
            bool canPlayTag = ageYears >= 7f && partner.ageYears >= 7f && Random.value < 0.45f;
            ChildPlayKind kind = canPlayTag ? ChildPlayKind.Tag : ChildPlayKind.Pair;
            float duration = Random.Range(ChildPairPlaySecondsMin, ChildPairPlaySecondsMax);
            ConfigureChildPairPlay(partner, kind, selfTarget, duration);
            partner.ConfigureChildPairPlay(this, kind, partnerTarget, duration);
        }

        private void ConfigureChildPairPlay(
            StrategyResidentAgent partner,
            ChildPlayKind kind,
            Vector2Int targetCell,
            float duration)
        {
            childPlayKind = kind;
            childPlayPartner = partner;
            childPlayCenterCell = targetCell;
            childPlayTimer = duration;
            childTagMovesRemaining = kind == ChildPlayKind.Tag ? Random.Range(2, 5) : 0;
            childTagStepCooldown = Random.Range(1.0f, 2.0f);
            activity = ResidentActivity.MovingToChildPartner;
            hasTarget = path.Count > 0;
            waitTimer = 0f;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            UseIdleSprite();
        }

        private void StartReachedChildPlay()
        {
            hasTarget = false;
            path.Clear();
            pathIndex = 0;
            waitTimer = 0f;
            if (childPlayKind == ChildPlayKind.None)
            {
                childPlayKind = ChildPlayKind.Solo;
                childPlayTimer = Random.Range(ChildSoloPlaySecondsMin, ChildSoloPlaySecondsMax);
            }

            activity = childPlayKind switch
            {
                ChildPlayKind.Tag => ResidentActivity.PlayingTag,
                ChildPlayKind.Pair => ResidentActivity.PlayingWithChild,
                _ => ResidentActivity.PlayingAlone
            };

            if (childPlayPartner != null)
            {
                FaceWorldPoint(childPlayPartner.transform.position);
            }

            UseIdleSprite();
        }

        private void UpdateChildPlayActivity()
        {
            if (!CanContinueChildPlay())
            {
                CancelChildPlay(true);
                return;
            }

            bool waitingForPartner = childPlayPartner != null
                && IsMovingChildPlayActivity(childPlayPartner.activity);
            if (!waitingForPartner)
            {
                childPlayTimer -= Time.deltaTime;
            }

            childTagStepCooldown -= Time.deltaTime;
            if (childPlayTimer <= 0f)
            {
                CancelChildPlay(true);
                return;
            }

            if (activity == ResidentActivity.PlayingTag
                && childTagMovesRemaining > 0
                && childTagStepCooldown <= 0f
                && TryStartNextChildTagMove())
            {
                return;
            }

            if (childPlayPartner != null)
            {
                FaceWorldPoint(childPlayPartner.transform.position);
            }

            AnimateIdle();
        }

        private bool TryStartNextChildTagMove()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2Int cell = childPlayCenterCell + new Vector2Int(
                    Random.Range(-ChildTagMoveRadius, ChildTagMoveRadius + 1),
                    Random.Range(-ChildTagMoveRadius, ChildTagMoveRadius + 1));
                if (!IsCellInChildPlayRange(cell) || !map.IsCellWalkable(cell) || !TryBuildPathTo(cell))
                {
                    continue;
                }

                childTagMovesRemaining--;
                childTagStepCooldown = Random.Range(1.0f, 2.25f);
                activity = ResidentActivity.MovingToChildPartner;
                hasTarget = path.Count > 0;
                waitTimer = 0f;
                return true;
            }

            childTagStepCooldown = Random.Range(1.0f, 2.0f);
            return false;
        }

        private bool TryCancelChildPlayForNight()
        {
            if (StrategyDayNightCycleController.IsSettlementWorkTime || !IsChildPlayActivity(activity))
            {
                return false;
            }

            CancelChildPlay(true);
            return true;
        }

        private void CancelChildPlay(bool scheduleCooldown)
        {
            StrategyResidentAgent partner = childPlayPartner;
            ClearChildPlayState(scheduleCooldown);
            if (partner != null && partner.childPlayPartner == this)
            {
                partner.ClearChildPlayState(scheduleCooldown);
            }
        }

        private void ClearChildPlayState(bool scheduleCooldown)
        {
            bool wasPlaying = IsChildPlayActivity(activity) || childPlayKind != ChildPlayKind.None;
            childPlayKind = ChildPlayKind.None;
            childPlayPartner = null;
            childPlayTimer = 0f;
            childTagStepCooldown = 0f;
            childTagMovesRemaining = 0;
            if (scheduleCooldown && wasPlaying)
            {
                childPlayCooldown = Random.Range(ChildPlayCooldownMin, ChildPlayCooldownMax);
            }

            if (!wasPlaying)
            {
                return;
            }

            if (IsChildPlayActivity(activity))
            {
                activity = GetRestingActivity();
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                waitTimer = Random.Range(0.25f, 0.8f);
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                UseIdleSprite();
            }
        }

        private bool CanStartChildPlayFromIdle()
        {
            return map != null
                && !IsAdult
                && !IsHomeboundYoungChild
                && !deathRequested
                && !IsPendingRefugee
                && !hiddenInsideHome
                && !hiddenUnderground
                && !sleepingInsideHome
                && !returningHomeToSleep
                && !IsFuneralDutyActive
                && StrategyDayNightCycleController.IsSettlementWorkTime
                && !ShouldPrioritizeHouseholdFoodHelpOverChildPlay()
                && activity == ResidentActivity.Idle
                && !hasTarget
                && waitTimer <= 0f;
        }

        private bool CanAcceptChildPlayInvitation(StrategyResidentAgent requester)
        {
            return requester != null
                && requester != this
                && map != null
                && !IsAdult
                && !IsHomeboundYoungChild
                && !deathRequested
                && !IsPendingRefugee
                && !hiddenInsideHome
                && !hiddenUnderground
                && !sleepingInsideHome
                && !returningHomeToSleep
                && !IsFuneralDutyActive
                && StrategyDayNightCycleController.IsSettlementWorkTime
                && !ShouldPrioritizeHouseholdFoodHelpOverChildPlay()
                && activity == ResidentActivity.Idle
                && !hasTarget
                && childPlayKind == ChildPlayKind.None
                && childPlayPartner == null
                && childPlayCooldown <= 0f;
        }

        private bool CanContinueChildPlay()
        {
            if (IsAdult
                || IsHomeboundYoungChild
                || deathRequested
                || hiddenInsideHome
                || hiddenUnderground
                || sleepingInsideHome
                || IsPendingRefugee
                || IsFuneralDutyActive
                || !StrategyDayNightCycleController.IsSettlementWorkTime)
            {
                return false;
            }

            return childPlayPartner == null
                || (!childPlayPartner.deathRequested
                    && childPlayPartner.childPlayPartner == this
                    && !childPlayPartner.IsAdult
                    && !childPlayPartner.IsHomeboundYoungChild);
        }

        private Vector2Int GetRandomChildPlayCell()
        {
            int radius = GetChildPlayRoamRadius();
            int minX = idleOrigin.x - radius;
            int maxX = idleOrigin.x + idleFootprint.x + radius - 1;
            int minY = idleOrigin.y - radius;
            int maxY = idleOrigin.y + idleFootprint.y + radius - 1;
            return new Vector2Int(Random.Range(minX, maxX + 1), Random.Range(minY, maxY + 1));
        }

        private bool IsCellInChildPlayRange(Vector2Int cell)
        {
            int radius = GetChildPlayRoamRadius();
            return cell.x >= idleOrigin.x - radius
                && cell.x <= idleOrigin.x + idleFootprint.x + radius - 1
                && cell.y >= idleOrigin.y - radius
                && cell.y <= idleOrigin.y + idleFootprint.y + radius - 1;
        }

        private int GetChildPlayRoamRadius()
        {
            if (ageYears >= 13f)
            {
                return 8;
            }

            return ageYears >= 7f ? 6 : IdleRadius;
        }

        private float GetChildPairPlayChance()
        {
            if (ageYears >= 7f)
            {
                return 0.68f;
            }

            return 0.42f;
        }

        private bool IsSiblingOf(StrategyResidentAgent other)
        {
            return other != null
                && ((fatherId != 0 && fatherId == other.fatherId)
                    || (motherId != 0 && motherId == other.motherId));
        }

        private static int GetChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static bool IsChildPlayActivity(ResidentActivity residentActivity)
        {
            return IsMovingChildPlayActivity(residentActivity)
                || IsStationaryChildPlayActivity(residentActivity);
        }

        private static bool IsMovingChildPlayActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.MovingToChildPlay
                || residentActivity == ResidentActivity.MovingToChildPartner;
        }

        private static bool IsStationaryChildPlayActivity(ResidentActivity residentActivity)
        {
            return residentActivity == ResidentActivity.PlayingAlone
                || residentActivity == ResidentActivity.PlayingWithChild
                || residentActivity == ResidentActivity.PlayingTag;
        }

        private readonly struct ChildPlayCandidate
        {
            public ChildPlayCandidate(
                StrategyResidentAgent resident,
                Vector2Int selfCell,
                Vector2Int partnerCell,
                int score)
            {
                Resident = resident;
                SelfCell = selfCell;
                PartnerCell = partnerCell;
                Score = score;
            }

            public StrategyResidentAgent Resident { get; }
            public Vector2Int SelfCell { get; }
            public Vector2Int PartnerCell { get; }
            public int Score { get; }
        }
    }
}
