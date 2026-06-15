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

        private int SpendAvailablePlanks(int requested)
        {
            int amount = Mathf.Min(Mathf.Max(0, requested), AvailableConstructionPlanks);
            if (amount <= 0)
            {
                return 0;
            }

            planksStored -= amount;
            UpdateStockVisual();
            return amount;
        }

        private void ReleaseConstructionReservation(object owner)
        {
            constructionLogReservations.Remove(owner);
            constructionStoneReservations.Remove(owner);
            constructionPlankReservations.Remove(owner);
            ReleaseConstructionPickupReservations(owner);
        }

        private bool HasAvailableConstructionReservation(object owner, StrategyConstructionResourceKind kind)
        {
            return GetAvailableReservationAmount(owner, kind) > 0;
        }

        private int GetAvailableReservationAmount(object owner, StrategyConstructionResourceKind kind)
        {
            Dictionary<object, int> source = GetConstructionReservations(kind);
            if (source == null)
            {
                return 0;
            }

            if (!source.TryGetValue(owner, out int amount) || amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, amount - CountPickupReservations(owner, kind));
        }

        private Dictionary<object, int> GetConstructionReservations(StrategyConstructionResourceKind kind)
        {
            return kind switch
            {
                StrategyConstructionResourceKind.Logs => constructionLogReservations,
                StrategyConstructionResourceKind.Stone => constructionStoneReservations,
                StrategyConstructionResourceKind.Planks => constructionPlankReservations,
                _ => null
            };
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
            return "Haulers: "
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
                + "Planks: "
                + planksStored
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
            count += CountAvailablePlankSources();
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

                if (kind == StrategyConstructionResourceKind.Planks && site.NeededPlanks > 0)
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
            TryDispatchSingleBuilderBalanced(builder, FootprintBounds.center);
        }

    }
}
