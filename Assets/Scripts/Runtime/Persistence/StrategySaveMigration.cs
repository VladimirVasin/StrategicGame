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
                    case 4:
                        MigrateVersion4To5(data);
                        break;
                    case 5:
                        MigrateVersion5To6(data);
                        break;
                    case 6:
                        MigrateVersion6To7(data);
                        break;
                    case 7:
                        MigrateVersion7To8(data);
                        break;
                    case 8:
                        MigrateVersion8To9(data);
                        break;
                    case 9:
                        MigrateVersion9To10(data);
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

        private static void MigrateVersion4To5(StrategySaveData data)
        {
            data.looseResources ??= new List<StrategyLooseResourceSaveData>();
            data.version = 5;
        }

        private static void MigrateVersion5To6(StrategySaveData data)
        {
            data.pointsOfInterest ??= new List<StrategyPointOfInterestSaveData>();
            data.pointsOfInterest.Clear();
            data.version = 6;
        }

        private static void MigrateVersion6To7(StrategySaveData data)
        {
            // Version 6 used the six-minute day: Dusk began at 19:00 (210s)
            // and the first Night began at 22:00 (255s).
            data.firstNightFaunaStage = data.elapsedSeconds < 210f
                ? (int)StrategyFirstNightFaunaStage.Dormant
                : data.elapsedSeconds < 255f
                    ? (int)StrategyFirstNightFaunaStage.MiceVisible
                    : (int)StrategyFirstNightFaunaStage.StoryCompleted;
            data.version = 7;
        }

        private static void MigrateVersion7To8(StrategySaveData data)
        {
            data.cityItems = new List<StrategyCityItemSaveData>();
            data.version = 8;
        }

        private static void MigrateVersion8To9(StrategySaveData data)
        {
            data.cityItems ??= new List<StrategyCityItemSaveData>();
            if (data.firstNightFaunaStage == (int)StrategyFirstNightFaunaStage.StoryCompleted
                && !ContainsCityItem(data.cityItems, StrategyCityItemIds.Cats))
            {
                data.cityItems.Add(new StrategyCityItemSaveData
                {
                    itemId = StrategyCityItemIds.Cats,
                    quantity = 1
                });
            }

            data.version = 9;
        }

        private static void MigrateVersion9To10(StrategySaveData data)
        {
            data.scoutLodges = new List<StrategyScoutLodgeSaveData>();
            data.version = 10;
        }

        private static bool ContainsCityItem(
            IReadOnlyList<StrategyCityItemSaveData> items,
            string itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void NormalizeCollections(StrategySaveData data)
        {
            data.buildings ??= new List<StrategyBuildingSaveData>();
            data.constructionSites ??= new List<StrategyConstructionSiteSaveData>();
            data.residents ??= new List<StrategyResidentSaveData>();
            data.looseResources ??= new List<StrategyLooseResourceSaveData>();
            data.pointsOfInterest ??= new List<StrategyPointOfInterestSaveData>();
            data.cityItems ??= new List<StrategyCityItemSaveData>();
            data.scoutLodges ??= new List<StrategyScoutLodgeSaveData>();
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

            for (int i = 0; i < data.looseResources.Count; i++)
            {
                StrategyLooseResourceSaveData resource = data.looseResources[i];
                if (resource != null)
                {
                    resource.preparedDishRecipeId ??= string.Empty;
                }
            }

            for (int i = 0; i < data.scoutLodges.Count; i++)
            {
                StrategyScoutLodgeSaveData lodge = data.scoutLodges[i];
                if (lodge != null)
                {
                    lodge.lodgeStableId ??= string.Empty;
                }
            }
        }
    }
}
