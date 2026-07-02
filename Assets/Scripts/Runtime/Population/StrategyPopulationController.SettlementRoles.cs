using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyPopulationController
    {
        private const int MaxStarterSettlementBuilders = 4;
        private const float SettlementBuilderCountScoreWeight = 1000f;
        private const float SettlementBuilderEmptySiteScoreBonus = 500f;
        private const float SettlementBuilderRequestedSiteScoreBonus = 4f;
        private const float SettlementBuilderBuildableSiteScoreBonus = 12f;
        private const float SettlementBuilderDistanceScoreWeight = 0.02f;

        private readonly List<StrategyResidentAgent> settlementRoleCandidates = new();
        private readonly List<StrategyConstructionSite> settlementBuilderSiteQuery = new();

        public static int CountActiveSettlementHaulers()
        {
            StrategyPopulationController population = Object.FindAnyObjectByType<StrategyPopulationController>();
            return population != null ? population.CountSettlementHaulers() : 0;
        }

        public static int CountActiveSettlementBuilders()
        {
            StrategyPopulationController population = Object.FindAnyObjectByType<StrategyPopulationController>();
            return population != null ? population.CountSettlementBuilders() : 0;
        }

        public int CountSettlementHaulers()
        {
            return CountSettlementRole(resident => resident.IsSettlementHauler);
        }

        public int CountSettlementBuilders()
        {
            return CountSettlementRole(resident => resident.IsSettlementBuilder);
        }

        public bool TryAssignSettlementHauler(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (!TryFindSettlementRoleCandidate(out StrategyResidentAgent candidate))
            {
                return false;
            }

            return TryAssignSettlementHauler(candidate) && SetAssigned(candidate, out assigned);
        }

        public bool TryAssignSettlementBuilder(out StrategyResidentAgent assigned)
        {
            assigned = null;
            if (!TryFindSettlementRoleCandidate(out StrategyResidentAgent candidate))
            {
                return false;
            }

            return TryAssignSettlementBuilder(candidate) && SetAssigned(candidate, out assigned);
        }

        public bool TryAssignSettlementHauler(StrategyResidentAgent resident)
        {
            return resident != null && resident.AssignSettlementHaulerRole();
        }

        public bool TryAssignSettlementBuilder(StrategyResidentAgent resident)
        {
            return resident != null && resident.AssignSettlementBuilderRole();
        }

        public bool TryRemoveSettlementHauler(out StrategyResidentAgent removed, bool allowActiveRelease = true)
        {
            return TryRemoveSettlementRole(resident => resident.IsSettlementHauler, true, allowActiveRelease, out removed);
        }

        public bool TryRemoveSettlementBuilder(out StrategyResidentAgent removed, bool allowActiveRelease = true)
        {
            return TryRemoveSettlementRole(resident => resident.IsSettlementBuilder, false, allowActiveRelease, out removed);
        }

        public int CountReleasableSettlementHaulers(bool allowActiveRelease)
        {
            return CountReleasableSettlementRole(resident => resident.IsSettlementHauler, allowActiveRelease);
        }

        public int CountReleasableSettlementBuilders(bool allowActiveRelease)
        {
            return CountReleasableSettlementRole(resident => resident.IsSettlementBuilder, allowActiveRelease);
        }

        public bool TryDispatchSettlementBuildersToSite(StrategyConstructionSite requestedSite, bool assignAvailableAdults)
        {
            List<StrategyConstructionSite> sites = GetSettlementBuilderSites();
            if (sites.Count <= 0)
            {
                return false;
            }

            int assigned = DispatchAvailableSettlementBuilders(sites, requestedSite);
            while (assignAvailableAdults
                && CountSettlementBuilders() < MaxStarterSettlementBuilders
                && TryAssignSettlementBuilder(out StrategyResidentAgent builder))
            {
                StrategyConstructionSite target = ChooseSettlementBuilderSite(builder, sites, requestedSite);
                if (target == null || !RegisterSettlementBuilderToTarget(builder, target))
                {
                    builder.ClearSettlementBuilderRole();
                    break;
                }

                assigned++;
            }

            if (assigned > 0)
            {
                StrategyDebugLogger.Info(
                    "Construction",
                    "SettlementBuildersDispatched",
                    StrategyDebugLogger.F("requestedTool", requestedSite != null ? requestedSite.Tool : StrategyBuildTool.None),
                    StrategyDebugLogger.F("requestedOrigin", requestedSite != null ? requestedSite.Origin : Vector2Int.zero),
                    StrategyDebugLogger.F("assigned", assigned),
                    StrategyDebugLogger.F("siteCount", sites.Count));
            }

            return assigned > 0;
        }

        private int DispatchAvailableSettlementBuilders(
            IReadOnlyList<StrategyConstructionSite> sites,
            StrategyConstructionSite requestedSite)
        {
            int assigned = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent builder = residents[i];
                if (!CanDispatchSettlementBuilder(builder))
                {
                    continue;
                }

                StrategyConstructionSite target = ChooseSettlementBuilderSite(builder, sites, requestedSite);
                if (target != null && RegisterSettlementBuilderToTarget(builder, target))
                {
                    assigned++;
                }
            }

            return assigned;
        }

        private List<StrategyConstructionSite> GetSettlementBuilderSites()
        {
            settlementBuilderSiteQuery.Clear();
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                chunks.CopyActiveConstructionSites(settlementBuilderSiteQuery);
                return settlementBuilderSiteQuery;
            }

            IReadOnlyList<StrategyConstructionSite> sites = StrategyConstructionSite.ActiveSites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null && !sites[i].IsCompleted)
                {
                    settlementBuilderSiteQuery.Add(sites[i]);
                }
            }

            return settlementBuilderSiteQuery;
        }

        private StrategyConstructionSite ChooseSettlementBuilderSite(
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
                    - site.BuilderCount * SettlementBuilderCountScoreWeight
                    - GetSettlementBuilderDistanceScore(builder, site);
                if (site.BuilderCount <= 0)
                {
                    score += SettlementBuilderEmptySiteScoreBonus;
                }

                if (site.CanBuildWithDeliveredResources)
                {
                    score += SettlementBuilderBuildableSiteScoreBonus;
                }

                if (site == requestedSite)
                {
                    score += SettlementBuilderRequestedSiteScoreBonus;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = site;
                }
            }

            return best;
        }

        private static float GetSettlementBuilderDistanceScore(StrategyResidentAgent builder, StrategyConstructionSite site)
        {
            return builder != null && site != null
                ? (builder.transform.position - site.FootprintBounds.center).sqrMagnitude * SettlementBuilderDistanceScoreWeight
                : 0f;
        }

        private static bool RegisterSettlementBuilderToTarget(StrategyResidentAgent builder, StrategyConstructionSite target)
        {
            if (builder == null || target == null || builder.HasConstructionAssignment || !target.RegisterBuilder(builder, false))
            {
                return false;
            }

            builder.AssignConstructionSite(target, false);
            if (builder.ConstructionSite == target)
            {
                return true;
            }

            target.UnregisterBuilder(builder);
            return false;
        }

        private bool TryFindSettlementRoleCandidate(out StrategyResidentAgent candidate)
        {
            settlementRoleCandidates.Clear();
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && resident.CanAcceptWorkAssignment
                    && !resident.HasWorkplace
                    && !resident.HasConstructionAssignment)
                {
                    settlementRoleCandidates.Add(resident);
                }
            }

            candidate = settlementRoleCandidates.Count > 0
                ? settlementRoleCandidates[Random.Range(0, settlementRoleCandidates.Count)]
                : null;
            return candidate != null;
        }

        private static bool SetAssigned(StrategyResidentAgent candidate, out StrategyResidentAgent assigned)
        {
            assigned = candidate;
            return true;
        }

        private int CountSettlementRole(System.Func<StrategyResidentAgent, bool> hasRole)
        {
            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                if (residents[i] != null && hasRole(residents[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountReleasableSettlementRole(System.Func<StrategyResidentAgent, bool> hasRole, bool allowActiveRelease)
        {
            int count = 0;
            for (int i = 0; i < residents.Count; i++)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident != null
                    && hasRole(resident)
                    && (allowActiveRelease || resident.Activity == StrategyResidentAgent.ResidentActivity.Idle))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryRemoveSettlementRole(
            System.Func<StrategyResidentAgent, bool> hasRole,
            bool hauler,
            bool allowActiveRelease,
            out StrategyResidentAgent removed)
        {
            removed = null;
            for (int i = residents.Count - 1; i >= 0; i--)
            {
                StrategyResidentAgent resident = residents[i];
                if (resident == null
                    || !hasRole(resident)
                    || !allowActiveRelease && resident.Activity != StrategyResidentAgent.ResidentActivity.Idle)
                {
                    continue;
                }

                removed = resident;
                if (hauler)
                {
                    resident.ClearSettlementHaulerRole();
                }
                else
                {
                    resident.ClearSettlementBuilderRole();
                }

                return true;
            }

            return false;
        }

        private static bool CanDispatchSettlementBuilder(StrategyResidentAgent builder)
        {
            return builder != null
                && builder.IsSettlementBuilder
                && !builder.HasConstructionAssignment
                && builder.CanAcceptWorkAssignment;
        }
    }
}
