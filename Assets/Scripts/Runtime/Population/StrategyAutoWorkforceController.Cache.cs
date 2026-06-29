using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyAutoWorkforceController
    {
        private void RefreshWorksiteCacheFromActiveObjects()
        {
            IReadOnlyList<StrategyPlacedBuilding> buildings = StrategyPlacedBuilding.ActiveBuildings;
            cachedConstructionSites = CopyActiveConstructionSites(StrategyConstructionSite.ActiveSites);
            cachedStorageYards = FindBuildingComponents<StrategyStorageYard>(buildings);
            cachedLumberjackCamps = FindBuildingComponents<StrategyLumberjackCamp>(buildings);
            cachedStonecutterCamps = FindBuildingComponents<StrategyStonecutterCamp>(buildings);
            cachedMines = FindBuildingComponents<StrategyMine>(buildings);
            cachedCoalPits = FindBuildingComponents<StrategyCoalPit>(buildings);
            cachedClayPits = FindBuildingComponents<StrategyClayPit>(buildings);
            cachedSawmills = FindBuildingComponents<StrategySawmill>(buildings);
            cachedKilns = FindBuildingComponents<StrategyKiln>(buildings);
            cachedForges = FindBuildingComponents<StrategyForge>(buildings);
            cachedHunterCamps = FindBuildingComponents<StrategyHunterCamp>(buildings);
            cachedFisherHuts = FindBuildingComponents<StrategyFisherHut>(buildings);
            cachedForagerCamps = FindBuildingComponents<StrategyForagerCamp>(buildings);
            cachedChickenCoops = FindBuildingComponents<StrategyChickenCoop>(buildings);
            cachedGranaries = FindBuildingComponents<StrategyGranary>(buildings);
            cachedPlacedBuildings = CopyActiveBuildings(buildings);
        }

        private static T[] FindBuildingComponents<T>(IReadOnlyList<StrategyPlacedBuilding> buildings)
            where T : Component
        {
            if (buildings == null || buildings.Count <= 0)
            {
                return System.Array.Empty<T>();
            }

            List<T> found = new();
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyPlacedBuilding building = buildings[i];
                if (building != null && building.TryGetComponent(out T component))
                {
                    found.Add(component);
                }
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
