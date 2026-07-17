using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private static List<StrategyVisualCatalog.ResidentAtlasSet> BakeResidents(
            List<StrategyVisualCatalog.ResidentSpriteSet> portraits)
        {
            List<StrategyVisualCatalog.ResidentAtlasSet> result = new();
            StrategyResidentLifeStage[] lifeStages =
            {
                StrategyResidentLifeStage.Adult,
                StrategyResidentLifeStage.Child
            };
            for (int genderValue = 0; genderValue < 2; genderValue++)
            {
                StrategyResidentGender gender = (StrategyResidentGender)genderValue;
                for (int variant = 0; variant < 5; variant++)
                {
                    for (int lifeIndex = 0; lifeIndex < lifeStages.Length; lifeIndex++)
                    {
                        StrategyResidentLifeStage lifeStage = lifeStages[lifeIndex];
                        BakeResidentPortrait(portraits, gender, variant, lifeStage);
                        result.Add(BakeResidentAtlas(gender, variant, lifeStage));
                    }
                }
            }

            return result;
        }

        private static void BakeResidentPortrait(
            List<StrategyVisualCatalog.ResidentSpriteSet> portraits,
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage)
        {
            Sprite source = StrategyVisualBakeSource.GetResidentSprite(
                gender,
                variant,
                lifeStage,
                StrategyResidentVisualPose.Portrait,
                0);
            string path = $"{BakedRoot}/Residents/{lifeStage}/{gender}/V{variant + 1:00}_Portrait.png";
            Sprite portrait = BakeSpriteAsset(source, path);
            portraits.Add(new StrategyVisualCatalog.ResidentSpriteSet(
                gender,
                lifeStage,
                StrategyResidentVisualPose.Portrait,
                variant,
                new[] { portrait }));
        }

        private static StrategyVisualCatalog.ResidentAtlasSet BakeResidentAtlas(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage)
        {
            List<PoseFrames> poses = CollectResidentPoses(gender, variant, lifeStage);
            List<Sprite> allFrames = new();
            int maxFrameCount = 0;
            for (int i = 0; i < poses.Count; i++)
            {
                allFrames.AddRange(poses[i].Frames);
                maxFrameCount = Mathf.Max(maxFrameCount, poses[i].Frames.Length);
            }

            CalculateFrameLayout(allFrames.ToArray(), out int width, out int height, out Vector2 pivotPixels);
            int atlasWidth = width * maxFrameCount;
            int atlasHeight = height * poses.Count;
            Color32[] pixels = new Color32[atlasWidth * atlasHeight];
            StrategyVisualCatalog.ResidentPoseRow[] rows =
                new StrategyVisualCatalog.ResidentPoseRow[poses.Count];
            for (int row = 0; row < poses.Count; row++)
            {
                PoseFrames pose = poses[row];
                rows[row] = new StrategyVisualCatalog.ResidentPoseRow(pose.Pose, row, pose.Frames.Length);
                for (int frame = 0; frame < pose.Frames.Length; frame++)
                {
                    BlitSprite(
                        pose.Frames[frame],
                        pixels,
                        atlasWidth,
                        frame * width,
                        row * height,
                        pivotPixels);
                }
            }

            string path = $"{BakedRoot}/Residents/{lifeStage}/{gender}/V{variant + 1:00}_WorldAtlas.png";
            WriteTexture(path, pixels, atlasWidth, atlasHeight);
            ConfigureAtlasImporter(path);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (atlas == null)
            {
                throw new InvalidOperationException("Resident atlas import failed: " + path);
            }

            Vector2 pivot = new(pivotPixels.x / width, pivotPixels.y / height);
            return new StrategyVisualCatalog.ResidentAtlasSet(
                gender,
                lifeStage,
                variant,
                atlas,
                width,
                height,
                allFrames[0].pixelsPerUnit,
                pivot,
                rows);
        }

        private static List<PoseFrames> CollectResidentPoses(
            StrategyResidentGender gender,
            int variant,
            StrategyResidentLifeStage lifeStage)
        {
            List<PoseFrames> result = new();
            StrategyResidentVisualPose[] poses =
            {
                StrategyResidentVisualPose.Idle,
                StrategyResidentVisualPose.Walk,
                StrategyResidentVisualPose.Woodcut,
                StrategyResidentVisualPose.Stonecut,
                StrategyResidentVisualPose.CoalMine,
                StrategyResidentVisualPose.Construction,
                StrategyResidentVisualPose.Bow,
                StrategyResidentVisualPose.Butcher,
                StrategyResidentVisualPose.Fishing,
                StrategyResidentVisualPose.Crying,
                StrategyResidentVisualPose.ForageBerries,
                StrategyResidentVisualPose.ForageRoots,
                StrategyResidentVisualPose.ForageMushrooms,
                StrategyResidentVisualPose.NightTorchWalk,
                StrategyResidentVisualPose.NightTorchLight,
                StrategyResidentVisualPose.CampfireKindle,
                StrategyResidentVisualPose.GroundSleep,
                StrategyResidentVisualPose.MouseStartle,
                StrategyResidentVisualPose.TrashSearch
            };

            for (int poseIndex = 0; poseIndex < poses.Length; poseIndex++)
            {
                StrategyResidentVisualPose pose = poses[poseIndex];
                int frameCount = StrategyVisualBakeSource.GetResidentFrameCount(pose, lifeStage);
                if (frameCount <= 0)
                {
                    continue;
                }

                Sprite[] frames = new Sprite[frameCount];
                for (int frame = 0; frame < frameCount; frame++)
                {
                    frames[frame] = StrategyVisualBakeSource.GetResidentSprite(
                        gender,
                        variant,
                        lifeStage,
                        pose,
                        frame);
                    if (frames[frame] == null)
                    {
                        throw new InvalidOperationException(
                            $"Resident bake source missing: {lifeStage}/{gender}/V{variant}/{pose}/{frame}");
                    }
                }

                result.Add(new PoseFrames(pose, frames));
            }

            return result;
        }

        private sealed class PoseFrames
        {
            public PoseFrames(StrategyResidentVisualPose pose, Sprite[] frames)
            {
                Pose = pose;
                Frames = frames;
            }

            public StrategyResidentVisualPose Pose { get; }
            public Sprite[] Frames { get; }
        }
    }
}
