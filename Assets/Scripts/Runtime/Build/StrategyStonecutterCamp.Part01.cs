using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStonecutterCamp
    {

        public bool TryTakeReservedConstructionResource(
            object owner,
            StrategyResidentAgent builder,
            StrategyConstructionResourceKind kind,
            int maxAmount,
            out int amount)
        {
            amount = 0;
            if (owner == null
                || builder == null
                || kind != StrategyConstructionResourceKind.Stone
                || maxAmount <= 0
                || !constructionPickupReservations.TryGetValue(builder, out ConstructionPickupReservation pickup)
                || pickup == null
                || !ReferenceEquals(pickup.Owner, owner)
                || pickup.Amount <= 0)
            {
                return false;
            }

            if (!constructionStoneReservations.TryGetValue(owner, out int reserved)
                || reserved <= 0
                || stoneStored <= 0)
            {
                constructionPickupReservations.Remove(builder);
                return false;
            }

            amount = Mathf.Min(maxAmount, pickup.Amount, reserved, stoneStored);
            stoneStored -= amount;
            reserved -= amount;
            pickup.Amount -= amount;
            if (reserved <= 0)
            {
                constructionStoneReservations.Remove(owner);
            }
            else
            {
                constructionStoneReservations[owner] = reserved;
            }

            if (pickup.Amount <= 0)
            {
                constructionPickupReservations.Remove(builder);
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneTaken",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored));
            return amount > 0;
        }

        public void AddStone(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stoneStored = StrategyProductionStorage.AddCapped(stoneStored, stoneStored, amount, out int accepted);
            if (accepted <= 0)
            {
                return;
            }

            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "StoneStored",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("added", accepted),
                StrategyDebugLogger.F("rejected", amount - accepted),
                StrategyDebugLogger.F("stock", stoneStored));
        }

        public bool HasStorageSpaceFor(int amount)
        {
            return StrategyProductionStorage.CanAccept(stoneStored, amount);
        }

        public string GetHudStatusText()
        {
            int deposits = stone != null ? stone.CountAvailableDeposits(Origin, WorkRadius) : 0;
            return "Workers: "
                + workers.Count
                + "/"
                + MaxWorkers
                + "\n"
                + "Stone: "
                + StrategyProductionStorage.Format(stoneStored)
                + (reservedStone > 0 ? " (" + reservedStone + " reserved)" : string.Empty)
                + "\n"
                + "Deposits: "
                + deposits;
        }

        private int ReserveConstructionStone(object owner, int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableStone);
            if (owner == null || amount <= 0)
            {
                return 0;
            }

            AddReservation(constructionStoneReservations, owner, amount);
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneReserved",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("owner", owner),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored),
                StrategyDebugLogger.F("available", AvailableStone));
            return amount;
        }

        private int SpendAvailableStone(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableStone);
            if (amount <= 0)
            {
                return 0;
            }

            stoneStored -= amount;
            UpdateStockVisual();
            StrategyDebugLogger.Info(
                "StonecutterCamp",
                "ConstructionStoneSpent",
                StrategyDebugLogger.F("campOrigin", Origin),
                StrategyDebugLogger.F("amount", amount),
                StrategyDebugLogger.F("stock", stoneStored));
            return amount;
        }

        private bool HasAvailableConstructionReservation(object owner)
        {
            return GetAvailableConstructionReservationAmount(owner) > 0;
        }

        private int GetAvailableConstructionReservationAmount(object owner)
        {
            if (owner == null
                || !constructionStoneReservations.TryGetValue(owner, out int amount)
                || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner));
        }

        private void ReleaseConstructionReservation(object owner)
        {
            if (owner == null)
            {
                return;
            }

            constructionStoneReservations.Remove(owner);
            if (constructionPickupReservations.Count <= 0)
            {
                return;
            }

            List<StrategyResidentAgent> buildersToRelease = new();
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                if (pair.Value != null && ReferenceEquals(pair.Value.Owner, owner))
                {
                    buildersToRelease.Add(pair.Key);
                }
            }

            for (int i = 0; i < buildersToRelease.Count; i++)
            {
                constructionPickupReservations.Remove(buildersToRelease[i]);
            }
        }

        private int CountPickupReservations(object owner)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
                if (pair.Key != null
                    && reservation != null
                    && ReferenceEquals(reservation.Owner, owner)
                    && reservation.Amount > 0)
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        private static void AddReservation(Dictionary<object, int> reservations, object owner, int amount)
        {
            if (reservations.TryGetValue(owner, out int current))
            {
                reservations[owner] = current + amount;
            }
            else
            {
                reservations.Add(owner, amount);
            }
        }

        private static int CountReservations(Dictionary<object, int> reservations)
        {
            int total = 0;
            foreach (KeyValuePair<object, int> pair in reservations)
            {
                if (pair.Key != null && pair.Value > 0)
                {
                    total += pair.Value;
                }
            }

            return total;
        }

        private static StrategyStonecutterCamp[] GetCampsSortedByDistance(Vector3 nearWorld)
        {
            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            System.Array.Sort(
                camps,
                (left, right) =>
                {
                    if (left == null && right == null)
                    {
                        return 0;
                    }

                    if (left == null)
                    {
                        return 1;
                    }

                    if (right == null)
                    {
                        return -1;
                    }

                    float leftDistance = (left.FootprintBounds.center - nearWorld).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - nearWorld).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });
            return camps;
        }

        private void EnsureStockRenderer()
        {
            if (stockRenderer != null)
            {
                return;
            }

            GameObject stockObject = new GameObject("Stone Stock");
            stockObject.transform.SetParent(transform, false);
            stockRenderer = stockObject.AddComponent<SpriteRenderer>();
            stockRenderer.color = Color.white;
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (stockRenderer == null)
            {
                return;
            }

            stockRenderer.sprite = StrategyBuildingSpriteFactory.GetStonecutterCampStockSprite(stoneStored);
            stockRenderer.gameObject.SetActive(stoneStored > 0 && stockRenderer.sprite != null);
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (stockRenderer == null || building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            Vector3 world = new Vector3(bounds.max.x - 0.30f, bounds.min.y + 0.34f, -0.13f);
            stockRenderer.transform.localPosition = transform.InverseTransformPoint(world);
            stockRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stockRenderer, world, 1);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearStoneWorkplace(this);
                }
            }

            workers.Clear();
            stoneReservationOwner = null;
            reservedStone = 0;
            constructionStoneReservations.Clear();
            constructionPickupReservations.Clear();
        }

        private static string GetOwnerName(object owner)
        {
            return owner is StrategyResidentAgent resident ? resident.FullName : owner?.ToString() ?? "none";
        }
    }
}
