using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private void RefreshWorksiteCacheFromActiveObjects()
        {
            cachedConstructionSites = CopyActiveConstructionSites(GetActiveConstructionSiteSnapshot());
            cachedStorageYards = CopyActiveBuildingComponents<StrategyStorageYard>();
            cachedLumberjackCamps = CopyActiveBuildingComponents<StrategyLumberjackCamp>();
            cachedStonecutterCamps = CopyActiveBuildingComponents<StrategyStonecutterCamp>();
            cachedMines = CopyActiveBuildingComponents<StrategyMine>();
            cachedCoalPits = CopyActiveBuildingComponents<StrategyCoalPit>();
            cachedClayPits = CopyActiveBuildingComponents<StrategyClayPit>();
            cachedSawmills = CopyActiveBuildingComponents<StrategySawmill>();
            cachedKilns = CopyActiveBuildingComponents<StrategyKiln>();
            cachedForges = CopyActiveBuildingComponents<StrategyForge>();
            cachedHunterCamps = CopyActiveBuildingComponents<StrategyHunterCamp>();
            cachedFisherHuts = CopyActiveBuildingComponents<StrategyFisherHut>();
            cachedForagerCamps = CopyActiveBuildingComponents<StrategyForagerCamp>();
            cachedChickenCoops = CopyActiveBuildingComponents<StrategyChickenCoop>();
            cachedGranaries = CopyActiveBuildingComponents<StrategyGranary>();
            cachedPlacedBuildings = CopyActiveBuildings(GetActiveBuildingSnapshot());
        }

        private IReadOnlyList<StrategyPlacedBuilding> GetActiveBuildingSnapshot()
        {
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                return chunks.ActiveBuildingsView;
            }

            return StrategyPlacedBuilding.ActiveBuildings;
        }

        private IReadOnlyList<StrategyConstructionSite> GetActiveConstructionSiteSnapshot()
        {
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                return chunks.ActiveConstructionSitesView;
            }

            return StrategyConstructionSite.ActiveSites;
        }

        private static T[] CopyActiveBuildingComponents<T>()
            where T : Component
        {
            List<T> found = new();
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (chunks != null && chunks.IsConfigured)
            {
                chunks.CopyActiveBuildingComponents(found);
            }
            else
            {
                StrategyPlacedBuilding.CopyActiveComponents(found);
            }

            return found.Count > 0 ? found.ToArray() : System.Array.Empty<T>();
        }

        private static StrategyConstructionSite[] CopyActiveConstructionSites(
            IReadOnlyList<StrategyConstructionSite> sites)
        {
            if (sites == null || sites.Count <= 0)
            {
                return System.Array.Empty<StrategyConstructionSite>();
            }

            List<StrategyConstructionSite> found = new();
            for (int i = 0; i < sites.Count; i++)
            {
                StrategyConstructionSite site = sites[i];
                if (site != null && !site.IsCompleted)
                {
                    found.Add(site);
                }
            }

            return found.Count > 0 ? found.ToArray() : System.Array.Empty<StrategyConstructionSite>();
        }

        private static StrategyPlacedBuilding[] CopyActiveBuildings(IReadOnlyList<StrategyPlacedBuilding> buildings)
        {
            if (buildings == null || buildings.Count <= 0)
            {
                return System.Array.Empty<StrategyPlacedBuilding>();
            }

            List<StrategyPlacedBuilding> found = new();
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null)
                {
                    found.Add(building);
                }
            }

            return found.Count > 0 ? found.ToArray() : System.Array.Empty<StrategyPlacedBuilding>();
        }
    }
}
