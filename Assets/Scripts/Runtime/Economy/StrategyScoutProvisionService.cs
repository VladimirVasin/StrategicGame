using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyScoutProvisionService
    {
        private const int RationPointScale = 1000;
        private const float RationEpsilon = 0.0001f;
        private const StrategyResourceStoreScope SupplyScopes =
            StrategyResourceStoreScope.Settlement
            | StrategyResourceStoreScope.TemporarySettlement
            | StrategyResourceStoreScope.Production;
        private const StrategyResourceStoreScope ExcludedScopes =
            StrategyResourceStoreScope.Household
            | StrategyResourceStoreScope.Loose;

        public static float GetAvailableRations()
        {
            List<StrategyResourceStore> stores = new();
            StrategyResourceStore.CopyActiveStores(stores);
            List<SupplyCandidate> candidates = BuildCandidates(stores);
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                SupplyCandidate candidate = candidates[i];
                total += candidate.AvailableUnits * candidate.RationValue;
            }

            return total;
        }

        public static bool TryTakeRations(
            float requestedRations,
            object reservationOwner,
            out float suppliedRations)
        {
            suppliedRations = 0f;
            if (reservationOwner == null
                || float.IsNaN(requestedRations)
                || float.IsInfinity(requestedRations)
                || requestedRations <= 0f)
            {
                return false;
            }

            List<StrategyResourceStore> stores = new();
            StrategyResourceStore.CopyActiveStores(stores);
            ClearStaleExpeditionReservations(stores, reservationOwner);

            List<SupplyCandidate> candidates = BuildCandidates(stores);
            if (!TryBuildUnitPlan(
                    requestedRations,
                    candidates,
                    out List<PlanEntry> plan,
                    out float plannedRations))
            {
                return false;
            }

            if (!TryReservePlan(plan, reservationOwner))
            {
                return false;
            }

            if (!TryCommitPlan(plan, reservationOwner))
            {
                return false;
            }

            if (plannedRations + RationEpsilon < requestedRations)
            {
                RestoreCommittedPlan(plan);
                return false;
            }

            suppliedRations = plannedRations;
            NotifyChangedStores(plan);
            return true;
        }

        private static List<SupplyCandidate> BuildCandidates(
            List<StrategyResourceStore> stores)
        {
            List<SupplyCandidate> candidates = new();
            for (int storeIndex = 0; storeIndex < stores.Count; storeIndex++)
            {
                StrategyResourceStore store = stores[storeIndex];
                if (!CanSupplyExpedition(store))
                {
                    continue;
                }

                for (int value = 1; value <= (int)StrategyResourceType.Tools; value++)
                {
                    StrategyResourceType resource = (StrategyResourceType)value;
                    float rationValue = StrategyFoodNutrition.GetRationValue(resource);
                    int available = rationValue > 0f ? store.GetAvailable(resource) : 0;
                    if (available <= 0)
                    {
                        continue;
                    }

                    int rationPoints = ToRationPoints(rationValue);
                    if (rationPoints > 0)
                    {
                        candidates.Add(new SupplyCandidate(
                            store,
                            resource,
                            available,
                            rationValue,
                            rationPoints,
                            storeIndex));
                    }
                }
            }

            candidates.Sort(CompareCandidates);
            return candidates;
        }

        private static bool TryBuildUnitPlan(
            float requestedRations,
            List<SupplyCandidate> candidates,
            out List<PlanEntry> plan,
            out float suppliedRations)
        {
            plan = new List<PlanEntry>();
            suppliedRations = 0f;
            if (candidates.Count == 0)
            {
                return false;
            }

            int targetPoints = Mathf.CeilToInt(
                requestedRations * RationPointScale - RationEpsilon);
            int largestUnitPoints = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                largestUnitPoints = Math.Max(largestUnitPoints, candidates[i].RationPoints);
            }

            if (targetPoints <= 0 || largestUnitPoints <= 0)
            {
                return false;
            }

            int maximumPlanPoints;
            try
            {
                maximumPlanPoints = checked(targetPoints + largestUnitPoints - 1);
            }
            catch (OverflowException)
            {
                return false;
            }

            bool[] reachable = new bool[maximumPlanPoints + 1];
            int[] parentPoints = new int[maximumPlanPoints + 1];
            int[] parentCandidates = new int[maximumPlanPoints + 1];
            for (int i = 0; i <= maximumPlanPoints; i++)
            {
                parentPoints[i] = -1;
                parentCandidates[i] = -1;
            }

            reachable[0] = true;

            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                SupplyCandidate candidate = candidates[candidateIndex];
                int usefulUnits = Math.Min(
                    candidate.AvailableUnits,
                    maximumPlanPoints / candidate.RationPoints);
                for (int unit = 0; unit < usefulUnits; unit++)
                {
                    for (int points = maximumPlanPoints;
                        points >= candidate.RationPoints;
                        points--)
                    {
                        int previous = points - candidate.RationPoints;
                        if (reachable[points] || !reachable[previous])
                        {
                            continue;
                        }

                        reachable[points] = true;
                        parentPoints[points] = previous;
                        parentCandidates[points] = candidateIndex;
                    }
                }
            }

            int selectedPoints = -1;
            for (int points = targetPoints; points <= maximumPlanPoints; points++)
            {
                if (reachable[points])
                {
                    selectedPoints = points;
                    break;
                }
            }

            if (selectedPoints < 0)
            {
                return false;
            }

            int[] selectedUnits = new int[candidates.Count];
            int cursor = selectedPoints;
            while (cursor > 0)
            {
                int candidateIndex = parentCandidates[cursor];
                int previous = parentPoints[cursor];
                if (candidateIndex < 0 || previous < 0 || previous >= cursor)
                {
                    return false;
                }

                selectedUnits[candidateIndex]++;
                cursor = previous;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int units = selectedUnits[i];
                if (units <= 0)
                {
                    continue;
                }

                SupplyCandidate candidate = candidates[i];
                plan.Add(new PlanEntry(
                    candidate.Store,
                    candidate.Resource,
                    units,
                    candidate.RationValue));
                suppliedRations += units * candidate.RationValue;
            }

            return suppliedRations + RationEpsilon >= requestedRations;
        }

        private static bool TryReservePlan(List<PlanEntry> plan, object owner)
        {
            int reservedEntries = 0;
            for (int i = 0; i < plan.Count; i++)
            {
                PlanEntry entry = plan[i];
                bool reserved = entry.Store.TryReserve(
                    owner,
                    entry.Resource,
                    entry.Amount,
                    StrategyResourceReservationChannel.Expedition,
                    out int reservedAmount);
                if (!reserved || reservedAmount != entry.Amount)
                {
                    entry.Store.Release(
                        owner,
                        entry.Resource,
                        StrategyResourceReservationChannel.Expedition);
                    ReleaseReservations(plan, owner, reservedEntries);
                    return false;
                }

                reservedEntries++;
            }

            return true;
        }

        private static bool TryCommitPlan(List<PlanEntry> plan, object owner)
        {
            int committedEntries = 0;
            for (int i = 0; i < plan.Count; i++)
            {
                PlanEntry entry = plan[i];
                int taken = entry.Store.TakeReserved(
                    owner,
                    entry.Resource,
                    entry.Amount,
                    StrategyResourceReservationChannel.Expedition);
                if (taken != entry.Amount)
                {
                    if (taken > 0)
                    {
                        entry.Store.RestoreTakenForTransaction(entry.Resource, taken);
                    }

                    RestoreCommittedPlan(plan, committedEntries);
                    ReleaseReservations(plan, owner, plan.Count);
                    return false;
                }

                committedEntries++;
            }

            ReleaseReservations(plan, owner, plan.Count);
            return true;
        }

        private static void RestoreCommittedPlan(List<PlanEntry> plan)
        {
            RestoreCommittedPlan(plan, plan.Count);
        }

        private static void RestoreCommittedPlan(List<PlanEntry> plan, int count)
        {
            for (int i = 0; i < count; i++)
            {
                PlanEntry entry = plan[i];
                entry.Store.RestoreTakenForTransaction(entry.Resource, entry.Amount);
            }
        }

        private static void ReleaseReservations(
            List<PlanEntry> plan,
            object owner,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                PlanEntry entry = plan[i];
                entry.Store.Release(
                    owner,
                    entry.Resource,
                    StrategyResourceReservationChannel.Expedition);
            }
        }

        private static void ClearStaleExpeditionReservations(
            List<StrategyResourceStore> stores,
            object owner)
        {
            for (int i = 0; i < stores.Count; i++)
            {
                StrategyResourceStore store = stores[i];
                if (!CanSupplyExpedition(store))
                {
                    continue;
                }

                for (int value = 1; value <= (int)StrategyResourceType.Tools; value++)
                {
                    StrategyResourceType resource = (StrategyResourceType)value;
                    if (StrategyFoodNutrition.IsFood(resource))
                    {
                        store.Release(
                            owner,
                            resource,
                            StrategyResourceReservationChannel.Expedition);
                    }
                }
            }
        }

        private static void NotifyChangedStores(List<PlanEntry> plan)
        {
            HashSet<StrategyResourceStore> notified = new();
            for (int i = 0; i < plan.Count; i++)
            {
                StrategyResourceStore store = plan[i].Store;
                if (!notified.Add(store)
                    || store.Owner is not IStrategyExternalResourceTakeObserver observer)
                {
                    continue;
                }

                if (observer is UnityEngine.Object unityObserver && unityObserver == null)
                {
                    continue;
                }

                try
                {
                    observer.OnExternalResourceTaken();
                }
                catch (Exception exception)
                {
                    StrategyDebugLogger.Warn(
                        "ScoutExpedition",
                        "ResourceTakeObserverFailed",
                        StrategyDebugLogger.F("owner", store.Owner),
                        StrategyDebugLogger.F("exception", exception.GetType().Name));
                }
            }
        }

        private static bool CanSupplyExpedition(StrategyResourceStore store)
        {
            return store != null
                && (store.Scope & SupplyScopes) != 0
                && (store.Scope & ExcludedScopes) == 0;
        }

        private static int ToRationPoints(float rationValue)
        {
            return Mathf.RoundToInt(Mathf.Max(0f, rationValue) * RationPointScale);
        }

        private static int CompareCandidates(SupplyCandidate left, SupplyCandidate right)
        {
            int scope = GetScopePriority(left.Store.Scope).CompareTo(
                GetScopePriority(right.Store.Scope));
            if (scope != 0)
            {
                return scope;
            }

            int ownerType = string.Compare(
                left.Store.Owner?.GetType().FullName,
                right.Store.Owner?.GetType().FullName,
                StringComparison.Ordinal);
            if (ownerType != 0)
            {
                return ownerType;
            }

            int storeOrder = left.StoreOrder.CompareTo(right.StoreOrder);
            return storeOrder != 0
                ? storeOrder
                : left.Resource.CompareTo(right.Resource);
        }

        private static int GetScopePriority(StrategyResourceStoreScope scope)
        {
            if ((scope & StrategyResourceStoreScope.Settlement) != 0)
            {
                return 0;
            }

            return (scope & StrategyResourceStoreScope.TemporarySettlement) != 0 ? 1 : 2;
        }

        private readonly struct SupplyCandidate
        {
            public SupplyCandidate(
                StrategyResourceStore store,
                StrategyResourceType resource,
                int availableUnits,
                float rationValue,
                int rationPoints,
                int storeOrder)
            {
                Store = store;
                Resource = resource;
                AvailableUnits = availableUnits;
                RationValue = rationValue;
                RationPoints = rationPoints;
                StoreOrder = storeOrder;
            }

            public StrategyResourceStore Store { get; }
            public StrategyResourceType Resource { get; }
            public int AvailableUnits { get; }
            public float RationValue { get; }
            public int RationPoints { get; }
            public int StoreOrder { get; }
        }

        private readonly struct PlanEntry
        {
            public PlanEntry(
                StrategyResourceStore store,
                StrategyResourceType resource,
                int amount,
                float rationValue)
            {
                Store = store;
                Resource = resource;
                Amount = amount;
                RationValue = rationValue;
            }

            public StrategyResourceStore Store { get; }
            public StrategyResourceType Resource { get; }
            public int Amount { get; }
            public float RationValue { get; }
        }
    }
}
