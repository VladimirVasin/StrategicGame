using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {

        private int SpendAvailableLogs(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionLogs);
            if (amount <= 0)
            {
                return 0;
            }

            logsStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private int SpendAvailableStone(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionStone);
            if (amount <= 0)
            {
                return 0;
            }

            stoneStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private void ReleaseConstructionReservation(object owner)
        {
            constructionLogReservations.Remove(owner);
            constructionStoneReservations.Remove(owner);
            ReleaseConstructionPickupReservations(owner);
        }

        private bool HasAvailableConstructionReservation(object owner, StrategyConstructionResourceKind kind)
        {
            return GetAvailableReservationAmount(owner, kind) > 0;
        }

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> source = kind == StrategyConstructionResourceKind.Logs
                ? constructionLogReservations
                : constructionStoneReservations;
            if (!source.TryGetValue(owner, out int amount) || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner, kind));
        }

        private int CountPickupReservations(object owner, StrategyConstructionResourceKind kind)
        {
            int total = 0;
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
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

        private void ReleaseConstructionPickupReservations(object owner)
        {
            if (owner == null || constructionPickupReservations.Count <= 0)
            {
                return;
            }

            List<StrategyResidentAgent> buildersToRelease = new();
            foreach (KeyValuePair<StrategyResidentAgent, ConstructionPickupReservation> pair in constructionPickupReservations)
            {
                ConstructionPickupReservation reservation = pair.Value;
                if (reservation != null && ReferenceEquals(reservation.Owner, owner))
                {
                    buildersToRelease.Add(pair.Key);
                }
            }

            for (int i = 0; i < buildersToRelease.Count; i++)
            {
                constructionPickupReservations.Remove(buildersToRelease[i]);
            }
        }

        private static int TakeReservedConstruction(
            object owner,
            Dictionary<object, int> reservations,
            ref int stored,
            int maxAmount)
        {
            if (!reservations.TryGetValue(owner, out int reserved) || reserved <= 0 || stored <= 0)
            {
                return 0;
            }

            int amount = Mathf.Min(maxAmount, reserved, stored);
            stored -= amount;
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

        private static StrategyStorageYard[] GetYardsSortedByDistance(Vector3 nearWorld)
        {
            StrategyStorageYard[] yards = Object.FindObjectsByType<StrategyStorageYard>();
            System.Array.Sort(
                yards,
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
            return yards;
        }

        public string GetHudStatusText()
        {
            int sourceCount = CountAvailableSources();
            return "Storekeepers: "
                + workers.Count
                + "/\u221e"
                + "\n"
                + "Builders: "
                + builders.Count
                + "/\u221e"
                + "\n"
                + "Logs: "
                + logsStored
                + "\n"
                + "Stone: "
                + stoneStored
                + "\n"
                + "Iron: "
                + ironStored
                + "\n"
                + "Coal: "
                + coalStored
                + "\n"
                + "Sources: "
                + sourceCount;
        }

        private int CountAvailableSources()
        {
            int count = 0;
            StrategyLumberjackCamp[] camps = Object.FindObjectsByType<StrategyLumberjackCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyLumberjackCamp camp = camps[i];
                if (camp != null && camp.AvailableLogs > 0)
                {
                    count++;
                }
            }

            StrategyStonecutterCamp[] stoneCamps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            for (int i = 0; i < stoneCamps.Length; i++)
            {
                StrategyStonecutterCamp camp = stoneCamps[i];
                if (camp != null && camp.AvailableStone > 0)
                {
                    count++;
                }
            }

            count += CountAvailableIronSources();
            count += CountAvailableCoalSources();
            return count;
        }

        private bool HasAvailableLogLogisticsSource()
        {
            StrategyConstructionResourceCost loose = StrategyLooseConstructionResourcePile.GetTotalAvailableResources();
            if (loose.Logs > 0)
            {
                return true;
            }

            StrategyLumberjackCamp[] camps = Object.FindObjectsByType<StrategyLumberjackCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyLumberjackCamp camp = camps[i];
                if (camp != null && camp.AvailableLogs > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasAvailableStoneLogisticsSource()
        {
            StrategyConstructionResourceCost loose = StrategyLooseConstructionResourcePile.GetTotalAvailableResources();
            if (loose.Stone > 0)
            {
                return true;
            }

            StrategyStonecutterCamp[] camps = Object.FindObjectsByType<StrategyStonecutterCamp>();
            for (int i = 0; i < camps.Length; i++)
            {
                StrategyStonecutterCamp camp = camps[i];
                if (camp != null && camp.AvailableStone > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasWaitingConstructionNeeding(StrategyConstructionResourceKind kind)
        {
            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted || site.ResourcesComplete)
                {
                    continue;
                }

                if (kind == StrategyConstructionResourceKind.Logs && site.NeededLogs > 0)
                {
                    return true;
                }

                if (kind == StrategyConstructionResourceKind.Stone && site.NeededStone > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetAvailableBuilder(out StrategyResidentAgent builder)
        {
            builder = null;
            for (int i = 0; i < builders.Count; i++)
            {
                StrategyResidentAgent candidate = builders[i];
                if (candidate != null
                    && candidate.CanAcceptWorkAssignment
                    && candidate.BuilderWorkplace == this
                    && !candidate.HasConstructionAssignment)
                {
                    builder = candidate;
                    return true;
                }
            }

            return false;
        }

        private void TryDispatchBuilder(StrategyResidentAgent builder)
        {
            if (builder == null || builder.HasConstructionAssignment || !builder.CanAcceptWorkAssignment)
            {
                return;
            }

            StrategyConstructionSite[] sites = Object.FindObjectsByType<StrategyConstructionSite>();
            System.Array.Sort(
                sites,
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

                    float leftDistance = (left.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                    float rightDistance = (right.FootprintBounds.center - FootprintBounds.center).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });

            for (int i = 0; i < sites.Length; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted || site.BuilderCount >= StrategyConstructionSite.MaxBuilders)
                {
                    continue;
                }

                if (site.RegisterBuilder(builder, false))
                {
                    builder.AssignConstructionSite(site, false);
                    StrategyDebugLogger.Info(
                        "Construction",
                        "BuilderDispatched",
                        StrategyDebugLogger.F("tool", site.Tool),
                        StrategyDebugLogger.F("origin", site.Origin),
                        StrategyDebugLogger.F("builder", builder.FullName));
                    return;
                }
            }
        }

        private void EnsureStockRenderer()
        {
            if (logsStockRenderer != null && stoneStockRenderer != null && ironStockRenderer != null && coalStockRenderer != null)
            {
                return;
            }

            if (logsStockRenderer == null)
            {
                GameObject stockObject = new GameObject("Storage Logs Stock");
                stockObject.transform.SetParent(transform, false);
                logsStockRenderer = stockObject.AddComponent<SpriteRenderer>();
                logsStockRenderer.color = Color.white;
            }

            if (stoneStockRenderer == null)
            {
                GameObject stoneObject = new GameObject("Storage Stone Stock");
                stoneObject.transform.SetParent(transform, false);
                stoneStockRenderer = stoneObject.AddComponent<SpriteRenderer>();
                stoneStockRenderer.color = Color.white;
            }

            EnsureIronStockRenderer();
            EnsureCoalStockRenderer();
            UpdateStockPosition();
        }

        private void UpdateStockVisual()
        {
            EnsureStockRenderer();
            if (logsStockRenderer != null)
            {
                logsStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStockSprite(logsStored);
                logsStockRenderer.gameObject.SetActive(logsStored > 0 && logsStockRenderer.sprite != null);
            }

            if (stoneStockRenderer != null)
            {
                stoneStockRenderer.sprite = StrategyBuildingSpriteFactory.GetStorageYardStoneStockSprite(stoneStored);
                stoneStockRenderer.gameObject.SetActive(stoneStored > 0 && stoneStockRenderer.sprite != null);
            }

            UpdateIronStockVisual();
            UpdateCoalStockVisual();
            UpdateStockPosition();
        }

        private void UpdateStockPosition()
        {
            if (building == null)
            {
                return;
            }

            Bounds bounds = building.FootprintBounds;
            if (logsStockRenderer != null)
            {
                Vector3 logsWorld = new Vector3(bounds.center.x + 0.28f, bounds.min.y + 0.45f, -0.16f);
                logsStockRenderer.transform.localPosition = transform.InverseTransformPoint(logsWorld);
                logsStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(logsStockRenderer, logsWorld, 1);
            }

            if (stoneStockRenderer != null)
            {
                Vector3 stoneWorld = new Vector3(bounds.center.x - 0.86f, bounds.min.y + 0.37f, -0.155f);
                stoneStockRenderer.transform.localPosition = transform.InverseTransformPoint(stoneWorld);
                stoneStockRenderer.transform.localScale = Vector3.one;
                StrategyWorldSorting.Apply(stoneStockRenderer, stoneWorld, 1);
            }

            UpdateIronStockPosition(bounds);
            UpdateCoalStockPosition(bounds);
        }

        private void OnDestroy()
        {
            for (int i = workers.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent worker = workers[i];
                if (worker != null)
                {
                    worker.ClearStorageWorkplace(this);
                }
            }

            workers.Clear();

            for (int i = builders.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent builder = builders[i];
                if (builder != null)
                {
                    builder.ClearBuilderWorkplace(this);
                }
            }

            builders.Clear();
        }
    }
}
