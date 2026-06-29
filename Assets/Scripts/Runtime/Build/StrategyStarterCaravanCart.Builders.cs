using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyStarterCaravanCart
    {
        private const int MaxTemporaryStarterBuilders = 4;
        private const float TemporaryBuilderCountScoreWeight = 1000f;
        private const float TemporaryBuilderEmptySiteScoreBonus = 500f;
        private const float TemporaryBuilderRequestedSiteScoreBonus = 4f;
        private const float TemporaryBuilderBuildableSiteScoreBonus = 12f;
        private const float TemporaryBuilderDistanceScoreWeight = 0.02f;

        private static readonly List<StrategyConstructionSite> temporaryBuilderSiteQuery = new();

        public static bool TryAssignBuildersToSite(StrategyConstructionSite requestedSite)
        {
            if (requestedSite == null || requestedSite.IsCompleted)
            {
                return false;
            }

            StrategyPopulationController population = GetStarterBuilderPopulation();
            if (population == null)
            {
                return false;
            }

            List<StrategyConstructionSite> sites = GetTemporaryBuilderSites();
            if (sites.Count <= 0)
            {
                return false;
            }

            int assigned = 0;
            int activeTemporaryBuilders = CountActiveTemporaryBuilders(population);
            while (activeTemporaryBuilders + assigned < MaxTemporaryStarterBuilders
                && TryFindTemporaryBuilder(population, out StrategyResidentAgent builder))
            {
                StrategyConstructionSite target = ChooseTemporaryBuilderSite(builder, sites, requestedSite);
                if (target == null || !RegisterTemporaryBuilder(builder, target))
                {
                    break;
                }

                assigned++;
            }

            if (assigned > 0)
            {
                StrategyDebugLogger.Info(
                    "Construction",
                    "StarterTemporaryBuildersAssigned",
                    StrategyDebugLogger.F("requestedTool", requestedSite.Tool),
                    StrategyDebugLogger.F("requestedOrigin", requestedSite.Origin),
                    StrategyDebugLogger.F("assigned", assigned));
            }

            return assigned > 0;
        }

        private static StrategyPopulationController GetStarterBuilderPopulation()
        {
            List<StrategyStarterCaravanCart> carts = GetActiveCarts();
            for (int i = 0; i < carts.Count; i++)
            {
                if (carts[i] != null && carts[i].population != null)
                {
                    return carts[i].population;
                }
            }

            return null;
        }

        private static List<StrategyConstructionSite> GetTemporaryBuilderSites()
        {
            temporaryBuilderSiteQuery.Clear();
            IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null && !sites[i].IsCompleted)
                {
                    temporaryBuilderSiteQuery.Add(sites[i]);
                }
            }

            return temporaryBuilderSiteQuery;
        }

        private static int CountActiveTemporaryBuilders(StrategyPopulationController population)
        {
            int count = 0;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null && resident.ConstructionSite != null && resident.BuilderWorkplace == null)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryFindTemporaryBuilder(
            StrategyPopulationController population,
            out StrategyResidentAgent builder)
        {
            builder = null;
            IReadOnlyList<StrategyResidentAgent> residents = population.Residents;
            List<StrategyResidentAgent> candidates = new();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    candidates.Add(resident);
                }
            }

            if (candidates.Count <= 0)
            {
                return false;
            }

            builder = candidates[Random.Range(0, candidates.Count)];
            return true;
        }

        private static StrategyConstructionSite ChooseTemporaryBuilderSite(
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

                float score = site.NeededLogs + site.NeededStone + site.NeededPlanks + site.Cost.Total * 0.1f
                    - site.BuilderCount * TemporaryBuilderCountScoreWeight
                    - GetTemporaryBuilderDistanceScore(builder, site);
                if (site.BuilderCount <= 0)
                {
                    score += TemporaryBuilderEmptySiteScoreBonus;
                }

                if (site.CanBuildWithDeliveredResources)
                {
                    score += TemporaryBuilderBuildableSiteScoreBonus;
                }

                if (site == requestedSite)
                {
                    score += TemporaryBuilderRequestedSiteScoreBonus;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = site;
                }
            }

            return best;
        }

        private static float GetTemporaryBuilderDistanceScore(StrategyResidentAgent builder, StrategyConstructionSite site)
        {
            if (builder == null || site == null)
            {
                return 0f;
            }

            return (builder.transform.position - site.FootprintBounds.center).sqrMagnitude * TemporaryBuilderDistanceScoreWeight;
        }

        private static bool RegisterTemporaryBuilder(StrategyResidentAgent builder, StrategyConstructionSite target)
        {
            if (builder == null || target == null || builder.HasConstructionAssignment || !target.RegisterBuilder(builder, false))
            {
                return false;
            }

            if (builder.AssignTemporaryConstructionSite(target))
            {
                return true;
            }

            target.UnregisterBuilder(builder);
            return false;
        }
    }
}
