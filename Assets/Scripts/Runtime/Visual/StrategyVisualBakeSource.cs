using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public static class StrategyVisualBakeSource
    {
        public const int ConstructionStageCount = 7;
        public const int ChickenCoopAnimationFrameCount =
            StrategyBuildingUpgradeSpriteFactory.AnimationFrameCount;

        public static int GetBuildingVariantCount(StrategyBuildTool tool)
        {
            return StrategyBuildingSpriteFactory.GetVariantCount(tool);
        }

        public static Sprite GetBuildingSprite(StrategyBuildTool tool, int variant)
        {
            if (tool == StrategyBuildTool.Bridge
                && StrategyBridgeVisualProfile.TryCreateReadableCompletedSpriteForBake(
                    new Vector2Int(3, 1),
                    out Sprite bridge))
            {
                return bridge;
            }

            return StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, variant, out Sprite sprite)
                ? sprite
                : null;
        }

        public static int GetNatureVariantCount(StrategyNaturePropKind kind)
        {
            return StrategyNatureSpriteFactory.GetVariantCount(kind);
        }

        public static Sprite GetNatureSprite(StrategyNaturePropKind kind, int variant)
        {
            return StrategyNatureSpriteFactory.GetSprite(kind, variant);
        }

        public static Sprite GetConstructionSprite(StrategyBuildTool tool, int variant, int stage)
        {
            if (tool == StrategyBuildTool.Bridge
                && StrategyBridgeVisualProfile.TryCreateReadableConstructionSpriteForBake(
                    new Vector2Int(3, 1),
                    stage,
                    out Sprite bridge))
            {
                return bridge;
            }

            return StrategyConstructionSpriteFactory.GetConstructionSprite(tool, variant, stage);
        }

        public static Sprite GetChickenCoopProductionSprite(int frame)
        {
            return StrategyBuildingSpriteFactory.GetProceduralStandaloneChickenCoopSprite(frame);
        }

        public static void ResetRuntimeVisualCaches()
        {
            StrategyVisualCatalogProvider.ResetCache();
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBuildingUpgradeSpriteFactory.ResetCaches();
            StrategyBuildingSnowSpriteFactory.ResetCache();
            StrategyBuildingGroundSpriteFactory.ResetCache();
            StrategyHouseAmbientSpriteFactory.ResetCaches();
        }

        public static Sprite GetTrailSprite(int mask, int level, int variant)
        {
            return StrategyTrailSpriteFactory.GetSprite(mask, level, variant);
        }

        public static Sprite GetTerrainSprite(CityMapCellKind kind, int variant)
        {
            return StrategyTerrainTexturePainter.CreateCatalogSwatchSprite(kind, variant);
        }

        public static int GetBuildingLayerFrameCount(string id)
        {
            if (id == StrategyVisualSequenceIds.SawmillWork + "/W1")
            {
                return StrategyBuildingSpriteFactory.SawmillWorkFrameCount;
            }

            if (id == StrategyVisualSequenceIds.KilnWork + "/W1")
            {
                return StrategyBuildingSpriteFactory.KilnWorkFrameCount;
            }

            if (id == StrategyVisualSequenceIds.ForgeWork)
            {
                return StrategyBuildingSpriteFactory.ForgeWorkFrameCount;
            }

            return id == StrategyVisualSequenceIds.LumberjackLogs
                || id == StrategyVisualSequenceIds.StonecutterStone
                || id == StrategyVisualSequenceIds.HunterGame
                || id == StrategyVisualSequenceIds.FisherFish
                || id == StrategyVisualSequenceIds.MineIron
                || id == StrategyVisualSequenceIds.CoalPitCoal
                ? 5
                : 6;
        }

        public static Sprite GetBuildingLayerSprite(string id, int frame)
        {
            int level = frame + 1;
            return id switch
            {
                StrategyVisualSequenceIds.LumberjackLogs => StrategyBuildingSpriteFactory.GetLumberjackCampStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.StonecutterStone => StrategyBuildingSpriteFactory.GetStonecutterCampStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.HunterGame => StrategyBuildingSpriteFactory.GetHunterCampStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.FisherFish => StrategyBuildingSpriteFactory.GetFisherHutStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.StorageLogs => StrategyBuildingSpriteFactory.GetStorageYardStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.StorageStone => StrategyBuildingSpriteFactory.GetStorageYardStoneStockSprite(level * 4 - 3),
                StrategyVisualSequenceIds.GranaryGame => StrategyBuildingSpriteFactory.GetGranaryGameStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.GranaryFish => StrategyBuildingSpriteFactory.GetGranaryFishStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.MineIron => StrategyBuildingSpriteFactory.GetMineIronStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.StorageIron => StrategyBuildingSpriteFactory.GetStorageYardIronStockSprite(level * 4 - 3),
                StrategyVisualSequenceIds.CoalPitCoal => StrategyBuildingSpriteFactory.GetCoalPitStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.StorageCoal => StrategyBuildingSpriteFactory.GetStorageYardCoalStockSprite(level * 4 - 3),
                StrategyVisualSequenceIds.SawmillLogs => StrategyBuildingSpriteFactory.GetSawmillLogStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.SawmillPlanks => StrategyBuildingSpriteFactory.GetSawmillPlankStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.StoragePlanks => StrategyBuildingSpriteFactory.GetStorageYardPlankStockSprite(level * 3 - 2),
                StrategyVisualSequenceIds.ClayPitClay => StrategyBuildingSpriteFactory.GetClayPitStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.StorageClay => StrategyBuildingSpriteFactory.GetStorageYardClayStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.KilnClay => StrategyBuildingSpriteFactory.GetKilnClayStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.KilnCoal => StrategyBuildingSpriteFactory.GetKilnCoalStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.KilnPottery => StrategyBuildingSpriteFactory.GetKilnPotteryStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.StoragePottery => StrategyBuildingSpriteFactory.GetStorageYardPotteryStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.ForgeIron => StrategyBuildingSpriteFactory.GetForgeIronStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.ForgeCoal => StrategyBuildingSpriteFactory.GetForgeCoalStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.ForgeLogs => StrategyBuildingSpriteFactory.GetForgeLogStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.ForgeTools => StrategyBuildingSpriteFactory.GetForgeToolsStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.StorageTools => StrategyBuildingSpriteFactory.GetStorageYardToolsStockSprite(level * 2 - 1),
                StrategyVisualSequenceIds.SawmillWork + "/W1" => StrategyBuildingSpriteFactory.GetSawmillWorkSprite(frame, 1),
                StrategyVisualSequenceIds.KilnWork + "/W1" => StrategyBuildingSpriteFactory.GetKilnWorkSprite(frame, 1),
                StrategyVisualSequenceIds.ForgeWork => StrategyBuildingSpriteFactory.GetForgeWorkSprite(frame),
                _ => null
            };
        }

        public static int GetResidentFrameCount(
            StrategyResidentVisualPose pose,
            StrategyResidentLifeStage lifeStage)
        {
            return pose switch
            {
                StrategyResidentVisualPose.Idle => 1,
                StrategyResidentVisualPose.Walk => StrategyResidentSpriteFactory.WalkFrameCount,
                StrategyResidentVisualPose.Portrait => 1,
                StrategyResidentVisualPose.Crying => StrategyResidentSpriteFactory.CryFrameCount,
                StrategyResidentVisualPose.ForageBerries => StrategyResidentSpriteFactory.ForageFrameCount,
                StrategyResidentVisualPose.ForageRoots => StrategyResidentSpriteFactory.ForageFrameCount,
                StrategyResidentVisualPose.ForageMushrooms => StrategyResidentSpriteFactory.ForageFrameCount,
                StrategyResidentVisualPose.NightTorchWalk => StrategyResidentSpriteFactory.NightTorchWalkFrameCount,
                StrategyResidentVisualPose.NightTorchLight => StrategyResidentSpriteFactory.NightTorchLightFrameCount,
                StrategyResidentVisualPose.CampfireKindle => StrategyResidentSpriteFactory.CampfireKindleFrameCount,
                StrategyResidentVisualPose.GroundSleep => StrategyResidentSpriteFactory.GroundSleepFrameCount,
                StrategyResidentVisualPose.Woodcut when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.WoodcutFrameCount,
                StrategyResidentVisualPose.Stonecut when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.StonecutFrameCount,
                StrategyResidentVisualPose.CoalMine when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.CoalMineFrameCount,
                StrategyResidentVisualPose.Construction when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.ConstructionFrameCount,
                StrategyResidentVisualPose.Bow when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.BowFrameCount,
                StrategyResidentVisualPose.Butcher when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.ButcherFrameCount,
                StrategyResidentVisualPose.Fishing when lifeStage == StrategyResidentLifeStage.Adult => StrategyResidentSpriteFactory.FishingFrameCount,
                _ => 0
            };
        }

        public static Sprite GetResidentSprite(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage,
            StrategyResidentVisualPose pose,
            int frame)
        {
            return pose switch
            {
                StrategyResidentVisualPose.Idle => StrategyResidentSpriteFactory.GetSprite(gender, variant, lifeStage),
                StrategyResidentVisualPose.Walk => StrategyResidentSpriteFactory.GetWalkSprite(gender, variant, lifeStage, frame),
                StrategyResidentVisualPose.Portrait => StrategyResidentSpriteFactory.GetPortraitSprite(gender, variant, lifeStage),
                StrategyResidentVisualPose.Woodcut => StrategyResidentSpriteFactory.GetWoodcutSprite(gender, variant, frame),
                StrategyResidentVisualPose.Stonecut => StrategyResidentSpriteFactory.GetStonecutSprite(gender, variant, frame),
                StrategyResidentVisualPose.CoalMine => StrategyResidentSpriteFactory.GetCoalMineSprite(gender, variant, frame),
                StrategyResidentVisualPose.Construction => StrategyResidentSpriteFactory.GetConstructionSprite(gender, variant, frame),
                StrategyResidentVisualPose.Bow => StrategyResidentSpriteFactory.GetBowSprite(gender, variant, frame),
                StrategyResidentVisualPose.Butcher => StrategyResidentSpriteFactory.GetButcherSprite(gender, variant, frame),
                StrategyResidentVisualPose.Fishing => StrategyResidentSpriteFactory.GetFishingSprite(gender, variant, frame),
                StrategyResidentVisualPose.Crying => StrategyResidentSpriteFactory.GetCryingSprite(gender, variant, lifeStage, frame),
                StrategyResidentVisualPose.ForageBerries => StrategyResidentSpriteFactory.GetForageSprite(gender, variant, lifeStage, StrategyResourceType.Berries, frame),
                StrategyResidentVisualPose.ForageRoots => StrategyResidentSpriteFactory.GetForageSprite(gender, variant, lifeStage, StrategyResourceType.Roots, frame),
                StrategyResidentVisualPose.ForageMushrooms => StrategyResidentSpriteFactory.GetForageSprite(gender, variant, lifeStage, StrategyResourceType.Mushrooms, frame),
                StrategyResidentVisualPose.NightTorchWalk => StrategyResidentSpriteFactory.GetNightTorchWalkSprite(gender, variant, lifeStage, frame),
                StrategyResidentVisualPose.NightTorchLight => StrategyResidentSpriteFactory.GetNightTorchLightSprite(gender, variant, lifeStage, frame),
                StrategyResidentVisualPose.CampfireKindle => StrategyResidentSpriteFactory.GetCampfireKindleSprite(gender, variant, lifeStage, frame),
                StrategyResidentVisualPose.GroundSleep => StrategyResidentSpriteFactory.GetGroundSleepSprite(gender, variant, lifeStage, frame),
                _ => null
            };
        }
    }
}
