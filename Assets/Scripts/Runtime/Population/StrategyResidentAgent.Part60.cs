using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private static readonly List<StrategyConstructionSite> constructionDeliverySiteQuery = new();
        private StrategyConstructionSite activeConstructionDeliverySite;

        private bool IsHaulerConstructionDeliveryActive =>
            constructionSite == null && activeConstructionDeliverySite != null;

        private StrategyConstructionSite GetActiveConstructionDeliverySite()
        {
            return constructionSite != null ? constructionSite : activeConstructionDeliverySite;
        }

        private bool TryStartHaulerConstructionDeliveryTask()
        {
            if (!CanStartHaulerConstructionDelivery())
            {
                return false;
            }

            constructionDeliverySiteQuery.Clear();
            IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null)
                {
                    constructionDeliverySiteQuery.Add(sites[i]);
                }
            }

            constructionDeliverySiteQuery.Sort(CompareHaulerConstructionSites);
            for (int i = 0; i < constructionDeliverySiteQuery.Count; i++)
            {
                StrategyConstructionSite site = constructionDeliverySiteQuery[i];
                if (site == null || site.IsCompleted || site.ResourcesComplete)
                {
                    continue;
                }

                if (TryStartHaulerConstructionDeliveryForSite(site))
                {
                    return true;
                }
            }

            return false;
        }

        private int CompareHaulerConstructionSites(StrategyConstructionSite left, StrategyConstructionSite right)
        {
            float leftScore = GetHaulerConstructionSiteScore(left);
            float rightScore = GetHaulerConstructionSiteScore(right);
            return leftScore.CompareTo(rightScore);
        }

        private float GetHaulerConstructionSiteScore(StrategyConstructionSite site)
        {
            if (site == null || site.IsCompleted || site.ResourcesComplete)
            {
                return float.MaxValue;
            }

            return Vector3.SqrMagnitude(site.FootprintBounds.center - transform.position);
        }

        private bool TryStartHaulerConstructionDeliveryForSite(StrategyConstructionSite site)
        {
            if (site == null
                || !site.TryFindResourcePickup(
                    this,
                    out IStrategyConstructionResourceSource source,
                    out StrategyConstructionResourceKind kind,
                    out Vector2Int pickupCell,
                    out int pickupAmount))
            {
                return false;
            }

            if (source == null || pickupAmount <= 0 || !TryBuildPathTo(pickupCell))
            {
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                return false;
            }

            if (!source.TryReserveConstructionPickup(site, this, kind, pickupAmount))
            {
                hasTarget = false;
                path.Clear();
                pathIndex = 0;
                return false;
            }

            activeConstructionDeliverySite = site;
            activeConstructionSource = source;
            activeConstructionResource = kind;
            constructionPickupPathFailures = 0;
            activity = ResidentActivity.MovingToConstructionStorage;
            hasTarget = true;
            waitTimer = Random.Range(0.02f, 0.14f);
            StrategyDebugLogger.Info(
                "Logistics",
                "HaulerConstructionDeliveryStarted",
                StrategyDebugLogger.F("resident", FullName),
                StrategyDebugLogger.F("siteOrigin", site.Origin),
                StrategyDebugLogger.F("sourceOrigin", source.Origin),
                StrategyDebugLogger.F("resource", kind),
                StrategyDebugLogger.F("amount", pickupAmount),
                StrategyDebugLogger.F("pickupCell", pickupCell));
            return true;
        }
    }
}
