namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyNaturePropController
    {
        private float GetVegetationClusterScore(CityMapCell cell)
        {
            int salt = cell.Kind switch
            {
                CityMapCellKind.Forest => 901,
                CityMapCellKind.Meadow => 907,
                CityMapCellKind.Grass => 911,
                CityMapCellKind.Dirt => 919,
                CityMapCellKind.Shore => 929,
                _ => 937
            };
            return StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, cell.X, cell.Y, salt, 0.034f, 0.093f);
        }

        private float GetStoneClusterScore(CityMapCell cell)
        {
            return StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, cell.X, cell.Y, 953, 0.030f, 0.086f);
        }

        private float GetIronClusterScore(CityMapCell cell)
        {
            return StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, cell.X, cell.Y, 967, 0.024f, 0.071f);
        }

        private float GetCoalClusterScore(CityMapCell cell)
        {
            return StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, cell.X, cell.Y, 971, 0.026f, 0.076f);
        }

        private float GetClayClusterScore(CityMapCell cell)
        {
            return StrategyMapDistributionUtility.ClusterScore(map.ActiveSeed, cell.X, cell.Y, 977, 0.038f, 0.104f);
        }
    }
}
