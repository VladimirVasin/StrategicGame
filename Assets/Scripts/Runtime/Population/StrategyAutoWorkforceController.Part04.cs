using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private const int MaxDemandRebalanceReleasesPerTick = 3;
        private const float RebalanceScoreMargin = 20f;
        private const float EmergencyDemandRebalanceMargin = 140f;
        private const float ZeroCoverageRescueStealMargin = 10f;
        private const float DemandRebalanceLockSeconds = 18f;
        private const float NoDonorRetrySeconds = 6f;

        private readonly Dictionary<StrategyProfessionType, float> demandRebalanceLocks = new();
        private float nextDonorFailureLogTime;
        private float nextNoDonorSearchTime;
        private StrategyProfessionType lastNoDonorProfession;
        private string lastDonorFailureKey = string.Empty;
        private string lastNoDonorReason = string.Empty;

        private int AssignDemandsWithRebalance(ref int released, out int demandReleased, ref int assignmentBudget)
        {
            demandReleased = 0;
            int assigned = AssignAvailableDemands(ref assignmentBudget);
            for (int i = 0; assignmentBudget > 0 && i < MaxDemandRebalanceReleasesPerTick && HasUnfilledDemand(); i++)
            {
                if (candidates.Count <= 0)
                {
                    StrategyAutoWorkforceDemand demand = GetTopUnfilledDemand();
                    if (!TryReleaseDemandDonor(demand, out StrategyProfessionType source, out StrategyResidentAgent worker))
                    {
                        break;
                    }

                    released++;
                    demandReleased++;
                    RegisterDemandRebalanceLock(source, demand.Profession);
                    TryAddReleasedCandidate(worker);
                    StrategyDebugLogger.Info(
                        "AutoWorkforce",
                        "AutoWorkforceReleasedForDemand",
                        StrategyDebugLogger.F("sourceProfession", source),
                        StrategyDebugLogger.F("targetProfession", demand.Profession),
                        StrategyDebugLogger.F("resident", worker != null ? worker.FullName : string.Empty),
                        StrategyDebugLogger.F("targetScore", demand.Score),
                        StrategyDebugLogger.F("lockSeconds", DemandRebalanceLockSeconds));
                }

                int assignedNow = AssignAvailableDemands(ref assignmentBudget);
                assigned += assignedNow;
                if (assignedNow <= 0 && candidates.Count <= 0)
                {
                    break;
                }
            }

            return assigned;
        }

        private int AssignAvailableDemands(ref int assignmentBudget)
        {
            int assigned = 0;
            for (int i = 0; assignmentBudget > 0 && i < demands.Count && candidates.Count > 0; i++)
            {
                assigned += AssignDemand(demands[i], ref assignmentBudget);
            }

            return assigned;
        }

        private bool HasUnfilledDemand()
        {
            return GetTopUnfilledDemand() != null;
        }

        private int CountUnfilledDemand()
        {
            int total = 0;
            for (int i = 0; i < demands.Count; i++)
            {
                StrategyAutoWorkforceDemand demand = demands[i];
                if (demand != null && demand.Needed > 0)
                {
                    total += demand.Needed;
                }
            }

            return total;
        }

        private StrategyAutoWorkforceDemand GetTopUnfilledDemand()
        {
            for (int i = 0; i < demands.Count; i++)
            {
                StrategyAutoWorkforceDemand demand = demands[i];
                if (demand != null && demand.Needed > 0)
                {
                    return demand;
                }
            }

            return null;
        }

        private bool TryReleaseDemandDonor(
            StrategyAutoWorkforceDemand demand,
            out StrategyProfessionType source,
            out StrategyResidentAgent worker)
        {
            source = default;
            worker = null;
            if (demand == null)
            {
                return false;
            }

            if (IsNoDonorSearchOnCooldown(demand))
            {
                return false;
            }

            bool found = false;
            float bestHoldScore = float.MaxValue;
            for (int i = 0; i < AutoManagedProfessions.Length; i++)
            {
                StrategyProfessionType profession = AutoManagedProfessions[i];
                if (!CanDonateWorker(profession, demand, out float holdScore))
                {
                    continue;
                }

                if (holdScore < bestHoldScore)
                {
                    found = true;
                    bestHoldScore = holdScore;
                    source = profession;
                }
            }

            if (!found)
            {
                RegisterNoDonorSearchCooldown(demand);
                LogDemandDonorSearchFailed(demand);
                return false;
            }

            nextNoDonorSearchTime = 0f;
            return TryReleaseProfessionWorker(source, out worker);
        }

        private bool IsNoDonorSearchOnCooldown(StrategyAutoWorkforceDemand demand)
        {
            return demand != null
                && Time.realtimeSinceStartup < nextNoDonorSearchTime
                && demand.Profession == lastNoDonorProfession
                && demand.Reason == lastNoDonorReason;
        }

        private bool IsNoDonorSearchCooldownActive()
        {
            return Time.realtimeSinceStartup < nextNoDonorSearchTime;
        }

        private void ResetNoDonorSearchCooldown()
        {
            nextNoDonorSearchTime = 0f;
            lastNoDonorProfession = default;
            lastNoDonorReason = string.Empty;
        }

        private void RegisterNoDonorSearchCooldown(StrategyAutoWorkforceDemand demand)
        {
            if (demand == null)
            {
                return;
            }

            lastNoDonorProfession = demand.Profession;
            lastNoDonorReason = demand.Reason;
            nextNoDonorSearchTime = Time.realtimeSinceStartup + NoDonorRetrySeconds;
        }

        private bool CanDonateWorker(
            StrategyProfessionType profession,
            StrategyAutoWorkforceDemand demand,
            out float holdScore)
        {
            return GetDemandDonorBlockReason(profession, demand, out holdScore) == null;
        }

        private string GetDemandDonorBlockReason(
            StrategyProfessionType profession,
            StrategyAutoWorkforceDemand demand,
            out float holdScore)
        {
            holdScore = float.MaxValue;
            bool zeroCoverageRescue = IsZeroCoverageRescueDemand(demand);
            if (profession == demand.Profession)
            {
                return "target_profession";
            }

            if (IsProfessionManualLocked(profession))
            {
                return "manual_locked";
            }

            if (IsDemandRebalanceLocked(profession) && !zeroCoverageRescue)
            {
                return "rebalance_locked";
            }

            int current = CountAssignedProfession(profession);
            if (current <= 0)
            {
                return "no_workers";
            }

            if (CountReleasableProfessionWorkers(profession) <= 0)
            {
                return "busy_workers";
            }

            int target = desiredProfessionTargets.TryGetValue(profession, out int value) ? value : 0;
            holdScore = GetProfessionHoldScore(profession, current, target);
            float demandHoldScore = GetDemandScoreForProfession(profession);
            if (demandHoldScore > holdScore)
            {
                holdScore = demandHoldScore;
            }

            if (ShouldProtectFoodDonor(profession, demand))
            {
                return "food_emergency";
            }

            bool canUseZeroRescueDonor = zeroCoverageRescue
                && CanUseZeroCoverageRescueDonor(profession, demand, current, holdScore);
            int coverageFloor = GetCoverageFloorTarget(profession);
            if (coverageFloor > 0 && current <= coverageFloor && !canUseZeroRescueDonor)
            {
                return "coverage_floor";
            }

            if (IsCoverageDemand(demand))
            {
                return null;
            }

            if (current <= target && !canUseZeroRescueDonor && !CanUseEmergencyDemandDonor(demand, holdScore))
            {
                return "at_or_below_target";
            }

            return canUseZeroRescueDonor || demand.Score >= holdScore + RebalanceScoreMargin ? null : "score_too_low";
        }

        private bool IsZeroCoverageRescueDemand(StrategyAutoWorkforceDemand demand)
        {
            return demand != null
                && settings.GetPriority(demand.Category) > 0
                && CountAssignedProfession(demand.Profession) <= 0
                && GetDesiredOrCoverageTarget(demand.Profession) > 0;
        }

        private static bool CanUseZeroCoverageRescueDonor(
            StrategyProfessionType profession,
            StrategyAutoWorkforceDemand demand,
            int current,
            float holdScore)
        {
            if (demand == null || profession == demand.Profession || current <= 0)
            {
                return false;
            }

            return current > 1 || demand.Score >= holdScore + ZeroCoverageRescueStealMargin;
        }

        private bool CanUseEmergencyDemandDonor(StrategyAutoWorkforceDemand demand, float holdScore)
        {
            return IsEmergencyDemand(demand)
                && demand.Score >= holdScore + EmergencyDemandRebalanceMargin;
        }

        private static bool IsEmergencyDemand(StrategyAutoWorkforceDemand demand)
        {
            return demand != null
                && (demand.Reason == "low_food"
                    || demand.Reason != null && demand.Reason.EndsWith("_shortage", System.StringComparison.Ordinal));
        }

        private bool ShouldProtectFoodDonor(StrategyProfessionType profession, StrategyAutoWorkforceDemand demand)
        {
            if (!IsFoodProfession(profession)
                || demand == null
                || demand.Category == StrategyAutoWorkforceCategory.Food)
            {
                return false;
            }

            return HasHouseholdFoodEmergency() || GetDemandScoreForProfession(profession) > 0f;
        }

        private static bool IsFoodProfession(StrategyProfessionType profession)
        {
            return profession == StrategyProfessionType.Hunter
                || profession == StrategyProfessionType.Fisher
                || profession == StrategyProfessionType.Forager;
        }

        private static bool IsCoverageDemand(StrategyAutoWorkforceDemand demand)
        {
            return demand != null && demand.Reason == "coverage_floor";
        }

        private void LogDemandDonorSearchFailed(StrategyAutoWorkforceDemand demand)
        {
            string key = demand.Profession + ":" + demand.Reason;
            if (lastDonorFailureKey == key && Time.realtimeSinceStartup < nextDonorFailureLogTime)
            {
                return;
            }

            lastDonorFailureKey = key;
            nextDonorFailureLogTime = Time.realtimeSinceStartup + NoDonorRetrySeconds;
            StrategyDebugLogger.Info(
                "AutoWorkforce",
                "AutoWorkforceRebalanceSkipped",
                StrategyDebugLogger.F("targetProfession", demand.Profession),
                StrategyDebugLogger.F("targetScore", demand.Score),
                StrategyDebugLogger.F("targetReason", demand.Reason),
                StrategyDebugLogger.F("reason", "no_donor"),
                StrategyDebugLogger.F("zeroCoverageRescue", IsZeroCoverageRescueDemand(demand)),
                StrategyDebugLogger.F("emergencyDemand", IsEmergencyDemand(demand)),
                StrategyDebugLogger.F("cooldownSeconds", NoDonorRetrySeconds));
        }

        private void RegisterDemandRebalanceLock(StrategyProfessionType source, StrategyProfessionType target)
        {
            float until = Time.realtimeSinceStartup + DemandRebalanceLockSeconds;
            demandRebalanceLocks[source] = until;
            demandRebalanceLocks[target] = until;
        }

        private bool IsDemandRebalanceLocked(StrategyProfessionType profession)
        {
            if (!demandRebalanceLocks.TryGetValue(profession, out float until))
            {
                return false;
            }

            if (until > Time.realtimeSinceStartup)
            {
                return true;
            }

            demandRebalanceLocks.Remove(profession);
            return false;
        }

        private float GetDemandScoreForProfession(StrategyProfessionType profession)
        {
            float score = 0f;
            for (int i = 0; i < demands.Count; i++)
            {
                StrategyAutoWorkforceDemand demand = demands[i];
                if (demand != null && demand.Profession == profession && demand.Score > score)
                {
                    score = demand.Score;
                }
            }

            return score;
        }

        private float GetProfessionHoldScore(StrategyProfessionType profession, int current, int target)
        {
            StrategyAutoWorkforceCategory category = GetProfessionCategory(profession);
            float priorityScore = settings.GetPriority(category) * BasePriorityScore;
            float staffedScore = Mathf.Max(0, target - current) * 25f;
            return priorityScore + staffedScore;
        }

        private static StrategyAutoWorkforceCategory GetProfessionCategory(StrategyProfessionType profession)
        {
            return profession switch
            {
                StrategyProfessionType.Builder => StrategyAutoWorkforceCategory.Construction,
                StrategyProfessionType.Hunter => StrategyAutoWorkforceCategory.Food,
                StrategyProfessionType.Fisher => StrategyAutoWorkforceCategory.Food,
                StrategyProfessionType.Forager => StrategyAutoWorkforceCategory.Food,
                StrategyProfessionType.StorageWorker => StrategyAutoWorkforceCategory.Logistics,
                StrategyProfessionType.Lumberjack => StrategyAutoWorkforceCategory.Wood,
                StrategyProfessionType.Stonecutter => StrategyAutoWorkforceCategory.Stone,
                StrategyProfessionType.Sawyer => StrategyAutoWorkforceCategory.Planks,
                StrategyProfessionType.Miner => StrategyAutoWorkforceCategory.Iron,
                StrategyProfessionType.CoalMiner => StrategyAutoWorkforceCategory.Coal,
                StrategyProfessionType.ClayDigger => StrategyAutoWorkforceCategory.Clay,
                StrategyProfessionType.Potter => StrategyAutoWorkforceCategory.Pottery,
                StrategyProfessionType.Blacksmith => StrategyAutoWorkforceCategory.Tools,
                _ => StrategyAutoWorkforceCategory.Construction
            };
        }
    }
}
