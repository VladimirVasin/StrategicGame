using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private static readonly string[] BuildingLayerIds =
        {
            StrategyVisualSequenceIds.LumberjackLogs,
            StrategyVisualSequenceIds.StonecutterStone,
            StrategyVisualSequenceIds.HunterGame,
            StrategyVisualSequenceIds.FisherFish,
            StrategyVisualSequenceIds.StorageLogs,
            StrategyVisualSequenceIds.StorageStone,
            StrategyVisualSequenceIds.GranaryGame,
            StrategyVisualSequenceIds.GranaryFish,
            StrategyVisualSequenceIds.MineIron,
            StrategyVisualSequenceIds.StorageIron,
            StrategyVisualSequenceIds.CoalPitCoal,
            StrategyVisualSequenceIds.StorageCoal,
            StrategyVisualSequenceIds.SawmillLogs,
            StrategyVisualSequenceIds.SawmillPlanks,
            StrategyVisualSequenceIds.StoragePlanks,
            StrategyVisualSequenceIds.ClayPitClay,
            StrategyVisualSequenceIds.StorageClay,
            StrategyVisualSequenceIds.KilnClay,
            StrategyVisualSequenceIds.KilnCoal,
            StrategyVisualSequenceIds.KilnPottery,
            StrategyVisualSequenceIds.StoragePottery,
            StrategyVisualSequenceIds.ForgeIron,
            StrategyVisualSequenceIds.ForgeCoal,
            StrategyVisualSequenceIds.ForgeLogs,
            StrategyVisualSequenceIds.ForgeTools,
            StrategyVisualSequenceIds.StorageTools,
            StrategyVisualSequenceIds.SawmillWork + "/W1",
            StrategyVisualSequenceIds.KilnWork + "/W1",
            StrategyVisualSequenceIds.ForgeWork
        };

        private static void BakeBuildingLayers(List<StrategyVisualCatalog.VisualSequenceSet> sequences)
        {
            for (int idIndex = 0; idIndex < BuildingLayerIds.Length; idIndex++)
            {
                string id = BuildingLayerIds[idIndex];
                int count = StrategyVisualBakeSource.GetBuildingLayerFrameCount(id);
                Sprite[] frames = new Sprite[count];
                for (int frame = 0; frame < count; frame++)
                {
                    frames[frame] = StrategyVisualBakeSource.GetBuildingLayerSprite(id, frame);
                    if (frames[frame] == null)
                    {
                        throw new InvalidOperationException($"Building layer bake source missing: {id}/{frame}");
                    }
                }

                string safeName = id.Replace('/', '_');
                string path = $"{BakedRoot}/BuildingLayers/{safeName}.png";
                sequences.Add(BakeSequenceAsset(id, frames, path));
            }
        }

        private static void BakeBuildingAnimations(
            List<StrategyVisualCatalog.VisualSequenceSet> sequences)
        {
            Sprite[] frames = new Sprite[StrategyVisualBakeSource.ChickenCoopAnimationFrameCount];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                frames[frame] = StrategyVisualBakeSource.GetChickenCoopProductionSprite(frame);
            }

            const string relativePath = "BuildingAnimations/ChickenCoop/V01.png";
            sequences.Add(BakeSequenceAsset(
                StrategyVisualSequenceIds.ChickenCoopProduction,
                frames,
                $"{BakedRoot}/{relativePath}",
                relativePath));
        }
    }
}
