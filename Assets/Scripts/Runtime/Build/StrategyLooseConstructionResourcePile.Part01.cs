using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyLooseConstructionResourcePile
    {

        private int TakeReservedConstruction(object owner, StrategyConstructionResourceKind kind, int maxAmount)
        {
            Dictionary<object, int> reservations = kind == StrategyConstructionResourceKind.Logs ? logReservations : stoneReservations;
            if (!reservations.TryGetValue(owner, out int reserved) || reserved <= 0)
            {
                return 0;
            }

            int amount = Mathf.Min(maxAmount, reserved, kind == StrategyConstructionResourceKind.Logs ? logs : stone);
            if (amount <= 0)
            {
                return 0;
            }

            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logs -= amount;
            }
            else
            {
                stone -= amount;
            }

            reserved -= amount;
            if (reserved <= 0)
            {
                reservations.Remove(owner);
            }
            else
            {
                reservations[owner] = reserved;
            }

            return amount;
        }

        private int TakeStorageReservation(StrategyConstructionResourceKind kind, int maxAmount)
        {
            int amount = kind == StrategyConstructionResourceKind.Logs
                ? Mathf.Min(maxAmount, logs)
                : Mathf.Min(maxAmount, stone);
            if (amount <= 0)
            {
                return 0;
            }

            if (kind == StrategyConstructionResourceKind.Logs)
            {
                logs -= amount;
            }
            else
            {
                stone -= amount;
            }

            return amount;
        }

        private void ReleaseConstructionReservation(object owner)
        {
            logReservations.Remove(owner);
            stoneReservations.Remove(owner);
            List<StrategyResidentAgent> builders = new();
            foreach (KeyValuePair<StrategyResidentAgent, PickupReservation> pair in pickupReservations)
            {
                if (pair.Value != null && ReferenceEquals(pair.Value.Owner, owner))
                {
                    builders.Add(pair.Key);
                }
            }

            for (int i = 0; i < builders.Count; i++)
            {
                pickupReservations.Remove(builders[i]);
            }
        }

        private int CountPickupReservations(StrategyConstructionResourceKind kind)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, PickupReservation> pair in pickupReservations)
            {
                PickupReservation reservation = pair.Value;
                if (pair.Key != null && reservation != null && reservation.Kind == kind && reservation.Amount > 0)
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        private int CountPickupReservations(object owner, StrategyConstructionResourceKind kind)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, PickupReservation> pair in pickupReservations)
            {
                PickupReservation reservation = pair.Value;
                if (pair.Key != null
                    && reservation != null
                    && ReferenceEquals(reservation.Owner, owner)
                    && reservation.Kind == kind
                    && reservation.Amount > 0)
                {
                    total += reservation.Amount;
                }
            }

            return total;
        }

        private void EnsureRenderers()
        {
            if (logsRenderer == null)
            {
                GameObject logsObject = new GameObject("Loose Logs");
                logsObject.transform.SetParent(transform, false);
                logsRenderer = logsObject.AddComponent<SpriteRenderer>();
            }

            if (stoneRenderer == null)
            {
                GameObject stoneObject = new GameObject("Loose Stone");
                stoneObject.transform.SetParent(transform, false);
                stoneRenderer = stoneObject.AddComponent<SpriteRenderer>();
            }
        }

        private void UpdateVisuals()
        {
            EnsureRenderers();
            Vector3 logsWorld = transform.position + new Vector3(-0.24f, 0.06f, 0f);
            logsRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionLogsSprite(logs);
            logsRenderer.gameObject.SetActive(logs > 0 && logsRenderer.sprite != null);
            logsRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
            logsRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(logsRenderer, logsWorld, 1);
            AttachPileShadow(logsRenderer, logs > 0 ? logs : stone);

            Vector3 stoneWorld = transform.position + new Vector3(0.26f, -0.02f, 0f);
            stoneRenderer.sprite = StrategyConstructionSpriteFactory.GetConstructionStoneSprite(stone);
            stoneRenderer.gameObject.SetActive(stone > 0 && stoneRenderer.sprite != null);
            stoneRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
            stoneRenderer.transform.localScale = Vector3.one;
            StrategyWorldSorting.Apply(stoneRenderer, stoneWorld, 2);
            AttachPileShadow(stoneRenderer, stone > 0 ? stone : logs);
        }

        private static void AttachPileShadow(SpriteRenderer renderer, int amount)
        {
            if (renderer == null)
            {
                return;
            }

            float size = Mathf.Lerp(0.20f, 0.38f, Mathf.Clamp01(amount / 8f));
            StrategyShadowCaster2D.Attach(
                renderer,
                StrategyShadowShape.SoftEllipse,
                new Vector2(0.02f, -0.02f),
                new Vector2(size, size * 0.36f),
                0.13f,
                -4,
                0f,
                false);
        }

        private void UpdateOrDestroy()
        {
            if (logs <= 0 && stone <= 0)
            {
                StrategyDebugLogger.Info(
                    "Build",
                    "LooseConstructionResourcesConsumed",
                    StrategyDebugLogger.F("origin", origin));
                Destroy(gameObject);
                return;
            }

            UpdateVisuals();
        }

        private static void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            GameObject rootObject = new GameObject("Loose Construction Resources");
            root = rootObject.transform;
        }

        private static StrategyLooseConstructionResourcePile[] GetPilesSortedByDistance(Vector3 nearWorld)
        {
            StrategyLooseConstructionResourcePile[] piles = Object.FindObjectsByType<StrategyLooseConstructionResourcePile>();
            System.Array.Sort(
                piles,
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
            return piles;
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
    }
}
