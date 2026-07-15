using System.Collections.Generic;

namespace ProjectUnknown.Strategy
{
    internal static class StrategySaveMigration
    {
        public static bool TryMigrate(StrategySaveData data, out string reason)
        {
            if (data == null)
            {
                reason = "empty_or_invalid_json";
                return false;
            }

            if (data.version < 1 || data.version > StrategySaveData.CurrentVersion)
            {
                reason = "unsupported_version_" + data.version;
                return false;
            }

            while (data.version < StrategySaveData.CurrentVersion)
            {
                switch (data.version)
                {
                    case 1:
                        MigrateVersion1To2(data);
                        break;
                    case 2:
                        MigrateVersion2To3(data);
                        break;
                    case 3:
                        MigrateVersion3To4(data);
                        break;
                    default:
                        reason = "missing_migration_from_version_" + data.version;
                        return false;
                }
            }

            NormalizeCollections(data);
            reason = string.Empty;
            return true;
        }

        private static void MigrateVersion1To2(StrategySaveData data)
        {
            NormalizeCollections(data);
            data.version = 2;
        }

        private static void MigrateVersion2To3(StrategySaveData data)
        {
            data.foundingStart = new StrategyFoundingStartSaveData();
            data.version = 3;
        }

        private static void MigrateVersion3To4(StrategySaveData data)
        {
            data.pointsOfInterest ??= new List<StrategyPointOfInterestSaveData>();
            data.version = 4;
        }

        private static void NormalizeCollections(StrategySaveData data)
        {
            data.buildings ??= new List<StrategyBuildingSaveData>();
            data.constructionSites ??= new List<StrategyConstructionSiteSaveData>();
            data.residents ??= new List<StrategyResidentSaveData>();
            data.looseResources ??= new List<StrategyLooseResourceSaveData>();
            data.pointsOfInterest ??= new List<StrategyPointOfInterestSaveData>();
            data.exploredCells ??= new List<int>();
            data.trailCells ??= new List<int>();
            data.foundingStart ??= new StrategyFoundingStartSaveData();
            data.foundingStart.profileId ??= string.Empty;
            data.foundingStart.answers ??= new List<StrategyFoundingAnswerSaveData>();

            for (int i = 0; i < data.buildings.Count; i++)
            {
                StrategyBuildingSaveData building = data.buildings[i];
                if (building == null)
                {
                    continue;
                }

                building.bridgeCells ??= new List<StrategyCellSaveData>();
                building.preparedDishIds ??= new List<string>();
                building.preparedDishAmounts ??= new List<int>();
            }

            for (int i = 0; i < data.constructionSites.Count; i++)
            {
                StrategyConstructionSiteSaveData site = data.constructionSites[i];
                if (site != null)
                {
                    site.bridgeCells ??= new List<StrategyCellSaveData>();
                }
            }

            for (int i = 0; i < data.residents.Count; i++)
            {
                StrategyResidentSaveData resident = data.residents[i];
                if (resident != null)
                {
                    resident.childIds ??= new List<int>();
                }
            }
        }
    }
}
