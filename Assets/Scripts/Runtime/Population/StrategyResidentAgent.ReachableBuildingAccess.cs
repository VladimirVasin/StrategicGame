using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyResidentAgent
    {
        private const float UnreachableBuildingAccessCooldownSeconds = 15f;

        private readonly Dictionary<EntityId, BuildingAccessFailure> buildingAccessFailures = new();

        public bool CanReachBuildingForReservation(Component source)
        {
            return TryFindReachableBuildingAccess(source, false, out _);
        }

        private bool TryBuildPathToBuildingAccess(Component source, out Vector2Int accessCell)
        {
            return TryFindReachableBuildingAccess(source, true, out accessCell);
        }

        private bool TryFindReachableBuildingAccess(
            Component source,
            bool buildPath,
            out Vector2Int accessCell)
        {
            accessCell = default;
            if (map == null || source == null)
            {
                return false;
            }

            StrategyPlacedBuilding building = source.GetComponent<StrategyPlacedBuilding>();
            if (building == null)
            {
                return false;
            }

            EntityId key = building.GetEntityId();
            if (buildingAccessFailures.TryGetValue(key, out BuildingAccessFailure failure))
            {
                if (failure.WalkabilityVersion == map.WalkabilityVersion && Time.time < failure.RetryTime)
                {
                    return false;
                }

                buildingAccessFailures.Remove(key);
            }

            Vector2Int origin = building.Origin;
            Vector2Int footprint = building.Footprint;
            for (int radius = 1; radius <= 3; radius++)
            {
                for (int y = -radius; y < footprint.y + radius; y++)
                {
                    for (int x = -radius; x < footprint.x + radius; x++)
                    {
                        bool edge = x == -radius
                            || y == -radius
                            || x == footprint.x + radius - 1
                            || y == footprint.y + radius - 1;
                        if (!edge)
                        {
                            continue;
                        }

                        Vector2Int candidate = origin + new Vector2Int(x, y);
                        if (!map.IsCellWalkable(candidate))
                        {
                            continue;
                        }

                        bool reachable = buildPath
                            ? TryBuildPathTo(candidate)
                            : CanReachCellForReservation(candidate);
                        if (!reachable)
                        {
                            if (WasLastPathBuildDeferred)
                            {
                                return false;
                            }

                            continue;
                        }

                        buildingAccessFailures.Remove(key);
                        accessCell = candidate;
                        return true;
                    }
                }
            }

            if (!WasLastPathBuildDeferred)
            {
                buildingAccessFailures[key] = new BuildingAccessFailure(
                    map.WalkabilityVersion,
                    Time.time + UnreachableBuildingAccessCooldownSeconds);
            }

            return false;
        }

        private readonly struct BuildingAccessFailure
        {
            public BuildingAccessFailure(int walkabilityVersion, float retryTime)
            {
                WalkabilityVersion = walkabilityVersion;
                RetryTime = retryTime;
            }

            public int WalkabilityVersion { get; }
            public float RetryTime { get; }
        }
    }
}
