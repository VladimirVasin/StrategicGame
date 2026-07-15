using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        internal static void VerifyAuthoredBuildingAnimations()
        {
            StrategyVisualCatalog catalog = Resources.Load<StrategyVisualCatalog>("Visual/StrategyVisualCatalog");
            Require(catalog != null, "Visual catalog resource is missing");
            StrategyVisualCatalogProvider.ResetCache();
            StrategyBuildingSpriteFactory.ResetCaches();
            try
            {
                AuthoredBuildingAnimationManifest manifest =
                    LoadAuthoredManifest<AuthoredBuildingAnimationManifest>(
                        AuthoredBuildingAnimationManifestPath);
                Require(manifest.schemaVersion == 1,
                    "Unsupported authored building-animation manifest schema");
                Require(manifest.sequences != null && manifest.sequences.Length > 0,
                    "Authored building-animation manifest has no sequences");
                HashSet<string> ids = new(StringComparer.Ordinal);
                HashSet<string> outputs = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < manifest.sequences.Length; i++)
                {
                    AuthoredBuildingAnimationSequence sequence = manifest.sequences[i];
                    Require(sequence != null && !string.IsNullOrWhiteSpace(sequence.id),
                        "Authored building-animation sequence has no ID");
                    Require(ids.Add(sequence.id),
                        "Duplicate authored building-animation sequence: " + sequence.id);
                    Require(outputs.Add(NormalizeAssetPath(sequence.output)),
                        "Duplicate authored building-animation output: " + sequence.output);
                    VerifyAuthoredBuildingAnimationSequence(catalog, sequence);
                }
            }
            finally
            {
                StrategyBuildingSpriteFactory.ResetCaches();
                StrategyVisualCatalogProvider.ResetCache();
            }
        }

        private static void VerifyAuthoredBuildingAnimationSequence(
            StrategyVisualCatalog catalog,
            AuthoredBuildingAnimationSequence sequence)
        {
            string label = "Building animation " + sequence.id;
            Require(
                Enum.TryParse(sequence.tool, false, out StrategyBuildTool tool)
                    && Enum.IsDefined(typeof(StrategyBuildTool), tool),
                label + " has an unknown building tool: " + sequence.tool);
            RequirePair(sequence.frameSize, label + " frameSize");
            Require(sequence.frameCount > 0, label + " frameCount must be positive");
            Require(sequence.ppu > 0f && float.IsFinite(sequence.ppu), label + " PPU is invalid");
            RequirePair(sequence.pivotNormalized, label + " pivotNormalized", true);
            Require(sequence.frames != null && sequence.frames.Length == sequence.frameCount,
                label + " source-frame count differs from frameCount");
            VerifyChickenCoopAnimationRuntimeContract(sequence, tool, label);

            VerifyExactSha256(sequence.output, sequence.expectedSha256, label + " atlas");
            RawSpriteImage atlasImage = LoadRawSpriteImage(sequence.output, label + " atlas");
            Require(
                atlasImage.Size.Width == sequence.frameSize[0] * sequence.frameCount
                    && atlasImage.Size.Height == sequence.frameSize[1],
                label + " atlas dimensions changed");
            VerifyVisiblePixelQuality(atlasImage, label + " atlas", false, false);

            HashSet<string> frameSources = new(StringComparer.OrdinalIgnoreCase);
            for (int frame = 0; frame < sequence.frameCount; frame++)
            {
                AuthoredBuildingAnimationFrame sourceFrame = sequence.frames[frame];
                string frameLabel = label + " frame " + frame;
                Require(sourceFrame != null && !string.IsNullOrWhiteSpace(sourceFrame.source),
                    frameLabel + " source is missing");
                Require(frameSources.Add(NormalizeAssetPath(sourceFrame.source)),
                    frameLabel + " reuses another source path");
                VerifyExactSha256(sourceFrame.source, sourceFrame.sha256, frameLabel + " source");
                RawSpriteImage sourceImage = LoadRawSpriteImage(sourceFrame.source, frameLabel + " source");
                Require(
                    sourceImage.Size.Width == sequence.frameSize[0]
                        && sourceImage.Size.Height == sequence.frameSize[1],
                    frameLabel + " source dimensions changed");
                VerifyVisiblePixelQuality(sourceImage, frameLabel + " source", true, false);
                Require(GetAlphaBoundsTopLeft(sourceImage, frameLabel).yMin > 0,
                    frameLabel + " source alpha is clipped by the top edge");
                VerifyAnimationAtlasFramePixels(atlasImage, sourceImage, sequence, frame, frameLabel);
            }

            Texture2D atlas = Resources.Load<Texture2D>(GetResourcePath(sequence.output));
            Require(atlas != null, label + " authored atlas resource is missing");
            Require(atlas.width == atlasImage.Size.Width && atlas.height == atlasImage.Size.Height,
                label + " imported atlas dimensions changed");
            VerifyAuthoredImporter(atlas, sequence.output, sequence.ppu, label + " atlas");
            string catalogId = GetBuildingAnimationCatalogId(sequence, tool, label);
            VerifySerializedAnimationSequence(catalog, catalogId, atlas, sequence, label);
            VerifyAnimationCatalogAndRuntimeFrames(catalog, catalogId, atlas, sequence, tool, label);
        }

        private static void VerifyChickenCoopAnimationRuntimeContract(
            AuthoredBuildingAnimationSequence sequence,
            StrategyBuildTool tool,
            string label)
        {
            Require(sequence.id == "ChickenCoopProduction" && tool == StrategyBuildTool.ChickenCoop,
                label + " is not a supported authored runtime sequence");
            Require(sequence.frameCount == 6
                    && sequence.frameCount == StrategyChickenCoopVisualProfile.AnimationFrameCount,
                label + " must keep all six runtime production frames");
            Require(sequence.frameSize[0] == StrategyChickenCoopVisualProfile.AuthoredFrameWidth
                    && sequence.frameSize[1] == StrategyChickenCoopVisualProfile.AuthoredFrameHeight,
                label + " frame size differs from the Chicken Coop runtime profile");
            Require(Approximately(sequence.ppu,
                    StrategyChickenCoopVisualProfile.StandalonePixelsPerUnit),
                label + " PPU differs from the Chicken Coop runtime profile");
            Require(Approximately(sequence.pivotNormalized[0], 0.5f)
                    && Approximately(sequence.pivotNormalized[1],
                        StrategyChickenCoopVisualProfile.StandalonePivotY),
                label + " pivot differs from the Chicken Coop runtime profile");
        }

        private static string GetBuildingAnimationCatalogId(
            AuthoredBuildingAnimationSequence sequence,
            StrategyBuildTool tool,
            string label)
        {
            string conventionId = $"BuildingAnimation/{tool}/V0";
            Require(conventionId == StrategyVisualSequenceIds.ChickenCoopProduction,
                label + " catalog sequence ID differs from the runtime constant");
            return conventionId;
        }

        private static void VerifyAnimationAtlasFramePixels(
            RawSpriteImage atlas,
            RawSpriteImage source,
            AuthoredBuildingAnimationSequence sequence,
            int frame,
            string label)
        {
            int frameWidth = sequence.frameSize[0];
            int frameHeight = sequence.frameSize[1];
            for (int y = 0; y < frameHeight; y++)
            {
                int atlasRow = y * atlas.Size.Width + frame * frameWidth;
                int sourceRow = y * frameWidth;
                for (int x = 0; x < frameWidth; x++)
                {
                    Require(
                        AuthoredCompositedPixelsEquivalent(
                            source.Pixels[sourceRow + x],
                            atlas.Pixels[atlasRow + x]),
                        label + $" differs from its source at {x},{y}");
                }
            }
        }

        private static void VerifySerializedAnimationSequence(
            StrategyVisualCatalog catalog,
            string catalogId,
            Texture2D atlas,
            AuthoredBuildingAnimationSequence sequence,
            string label)
        {
            SerializedProperty sequences = new SerializedObject(catalog).FindProperty("visualSequences");
            Require(sequences != null && sequences.isArray, "Visual catalog sequence data is missing");
            int matches = 0;
            for (int i = 0; i < sequences.arraySize; i++)
            {
                SerializedProperty candidate = sequences.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("id").stringValue != catalogId)
                {
                    continue;
                }

                matches++;
                Require(candidate.FindPropertyRelative("atlas").objectReferenceValue == atlas,
                    label + " serialized sequence owns a fallback atlas");
                Require(candidate.FindPropertyRelative("frameWidth").intValue == sequence.frameSize[0]
                        && candidate.FindPropertyRelative("frameHeight").intValue == sequence.frameSize[1]
                        && candidate.FindPropertyRelative("frameCount").intValue == sequence.frameCount,
                    label + " serialized frame layout changed");
                Require(Approximately(candidate.FindPropertyRelative("pixelsPerUnit").floatValue,
                        sequence.ppu),
                    label + " serialized PPU changed");
                Vector2 expectedPivot = new(sequence.pivotNormalized[0], sequence.pivotNormalized[1]);
                Require(Vector2.Distance(candidate.FindPropertyRelative("pivot").vector2Value, expectedPivot)
                        <= AuthoredVisualTolerance,
                    label + " serialized pivot changed");
            }

            Require(matches == 1, label + " must have exactly one serialized catalog sequence");
        }

        private static void VerifyAnimationCatalogAndRuntimeFrames(
            StrategyVisualCatalog catalog,
            string catalogId,
            Texture2D atlas,
            AuthoredBuildingAnimationSequence sequence,
            StrategyBuildTool tool,
            string label)
        {
            Require(tool == StrategyBuildTool.ChickenCoop,
                label + " has no authored runtime consumer verifier");
            for (int frame = 0; frame < sequence.frameCount; frame++)
            {
                Require(catalog.TryGetSequenceSprite(catalogId, frame, out Sprite catalogSprite)
                        && catalogSprite != null,
                    label + " catalog frame is missing: " + frame);
                VerifyOwnedAnimationFrame(catalogSprite, atlas, sequence, frame,
                    label + " catalog frame");

                Sprite runtimeSprite = StrategyBuildingSpriteFactory.GetStandaloneChickenCoopSprite(frame);
                Require(runtimeSprite != null, label + " runtime frame is missing: " + frame);
                VerifyOwnedAnimationFrame(runtimeSprite, atlas, sequence, frame,
                    label + " runtime frame");
            }
        }

        private static void VerifyOwnedAnimationFrame(
            Sprite sprite,
            Texture2D atlas,
            AuthoredBuildingAnimationSequence sequence,
            int frame,
            string label)
        {
            Require(sprite.texture == atlas, label + " does not own the exact authored atlas");
            Require(NormalizeAssetPath(AssetDatabase.GetAssetPath(sprite.texture))
                    == NormalizeAssetPath(sequence.output),
                label + " resolves to a fallback texture");
            Require(
                Approximately(sprite.rect.x, frame * sequence.frameSize[0])
                    && Approximately(sprite.rect.y, 0f)
                    && Approximately(sprite.rect.width, sequence.frameSize[0])
                    && Approximately(sprite.rect.height, sequence.frameSize[1]),
                label + " rect changed: " + frame);
            Require(Approximately(sprite.pixelsPerUnit, sequence.ppu),
                label + " PPU changed: " + frame);
            Vector2 expectedPivot = new(
                sequence.frameSize[0] * sequence.pivotNormalized[0],
                sequence.frameSize[1] * sequence.pivotNormalized[1]);
            Require(Vector2.Distance(sprite.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " pivot changed: " + frame);
        }
    }
}
