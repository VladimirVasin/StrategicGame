using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStorageYard
    {
        private const float BuilderCountScoreWeight = 1000f;
        private const float BuilderEmptySiteScoreBonus = 500f;
        private const float BuilderRequestedSiteScoreBonus = 4f;
        private const float BuilderBuildableSiteScoreBonus = 12f;
        private const float BuilderDistanceScoreWeight = 0.02f;
        private static readonly List<StrategyConstructionSite> constructionSiteQuery = new();

        private static bool TryAssignBuildersAcrossSites(StrategyConstructionSite requestedSite)
        {
            if (requestedSite == null || requestedSite.IsCompleted)
            {
                return false;
            }

            List<StrategyConstructionSite> sites = GetActiveConstructionSites();
            if (sites.Count <= 0)
            {
                return false;
            }

            int assignedCount = 0;
            List<StrategyStorageYard> yards = GetYardsSortedByDistance(requestedSite.FootprintBounds.center);
            for (int i = 0; i < yards.Count; i++)
            {
                StrategyStorageYard yard = yards[i];
                if (yard == null)
                {
                    continue;
                }

                while (yard.TryGetAvailableBuilder(out StrategyResidentAgent builder))
                {
                    StrategyConstructionSite target = ChooseBuilderDispatchSite(builder, sites, requestedSite);
                    if (target == null || !RegisterBuilderToTarget(builder, target))
                    {
                        break;
                    }

                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                StrategyDebugLogger.Info(
                    "Construction",
                    "BuildersBalancedDispatch",
                    StrategyDebugLogger.F("requestedTool", requestedSite.Tool),
                    StrategyDebugLogger.F("requestedOrigin", requestedSite.Origin),
                    StrategyDebugLogger.F("assigned", assignedCount),
                    StrategyDebugLogger.F("siteCount", sites.Count));
            }

            return assignedCount > 0;
        }

        private static void TryDispatchSingleBuilderBalanced(StrategyResidentAgent builder, Vector3 yardWorld)
        {
            if (builder == null || builder.HasConstructionAssignment || !builder.CanAcceptWorkAssignment)
            {
                return;
            }

            StrategyConstructionSite target = ChooseBuilderDispatchSite(builder, GetActiveConstructionSites(), null);
            if (target == null || !RegisterBuilderToTarget(builder, target))
            {
                return;
            }

            StrategyDebugLogger.Info(
                "Construction",
                "BuilderBalancedDispatch",
                StrategyDebugLogger.F("tool", target.Tool),
                StrategyDebugLogger.F("origin", target.Origin),
                StrategyDebugLogger.F("builder", builder.FullName),
                StrategyDebugLogger.F("yardWorld", yardWorld),
                StrategyDebugLogger.F("builderCount", target.BuilderCount));
        }

        private static List<StrategyConstructionSite> GetActiveConstructionSites()
        {
            constructionSiteQuery.Clear();
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                chunks.CopyActiveConstructionSites(constructionSiteQuery);
            }
            else
            {
                IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
                for (int i = 0; i < sites.Count; i++)
                {
                    if (sites[i] != null)
                    {
                        constructionSiteQuery.Add(sites[i]);
                    }
                }
            }

            constructionSiteQuery.Sort(
                (left, right) =>
                {
                    int leftCount = left != null && !left.IsCompleted ? left.BuilderCount : int.MaxValue;
                    int rightCount = right != null && !right.IsCompleted ? right.BuilderCount : int.MaxValue;
                    int countCompare = leftCount.CompareTo(rightCount);
                    return countCompare != 0
                        ? countCompare
                        : GetSitePriority(right).CompareTo(GetSitePriority(left));
                });
            return constructionSiteQuery;
        }

        private static StrategyConstructionSite ChooseBuilderDispatchSite(
            StrategyResidentAgent builder,
            IReadOnlyList<StrategyConstructionSite> sites,
            StrategyConstructionSite requestedSite)
        {
            StrategyConstructionSite best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < sites.Count; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site == null || site.IsCompleted)
                {
                    continue;
                }

                float score = GetSitePriority(site)
                    - site.BuilderCount * BuilderCountScoreWeight
                    - GetBuilderDistanceScore(builder, site);
                if (site.BuilderCount <= 0)
                {
                    score += BuilderEmptySiteScoreBonus;
                }

                if (site.CanBuildWithDeliveredResources)
                {
                    score += BuilderBuildableSiteScoreBonus;
                }

                if (site == requestedSite)
                {
                    score += BuilderRequestedSiteScoreBonus;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = site;
                }
            }

            return best;
        }

        private static float GetSitePriority(StrategyConstructionSite site)
        {
            if (site == null)
            {
                return float.MinValue;
            }

            return site.NeededLogs + site.NeededStone + site.NeededPlanks + site.Cost.Total * 0.1f;
        }

        private static float GetBuilderDistanceScore(StrategyResidentAgent builder, StrategyConstructionSite site)
        {
            if (builder == null || site == null)
            {
                return 0f;
            }

            return (builder.transform.position - site.FootprintBounds.center).sqrMagnitude * BuilderDistanceScoreWeight;
        }

        private static bool RegisterBuilderToTarget(StrategyResidentAgent builder, StrategyConstructionSite target)
        {
            if (builder == null
                || target == null
                || builder.HasConstructionAssignment
                || !target.RegisterBuilder(builder, false))
            {
                return false;
            }

            builder.AssignConstructionSite(target, false);
            return true;
        }
    }
}
