using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static void VerifyBridgeCatalogSequence(
            StrategyVisualCatalog catalog,
            string sequenceId,
            string atlasPath,
            RawSpriteImage[] sourceFrames,
            int[] frameSize,
            AuthoredBridgeManifest manifest,
            string label)
        {
            Require(sourceFrames != null && sourceFrames.Length > 0,
                label + " has no source frames");
            RawSpriteImage atlasImage = LoadRawSpriteImage(atlasPath, label + " atlas");
            Require(atlasImage.Size.Width == frameSize[0] * sourceFrames.Length
                    && atlasImage.Size.Height == frameSize[1],
                label + " atlas dimensions changed");
            VerifyVisiblePixelQuality(atlasImage, label + " atlas", false, false);
            for (int frame = 0; frame < sourceFrames.Length; frame++)
            {
                VerifyBridgeAtlasFramePixels(
                    atlasImage,
                    sourceFrames[frame],
                    frameSize,
                    frame,
                    label + " frame " + frame);
            }

            Texture2D atlas = Resources.Load<Texture2D>(GetResourcePath(atlasPath));
            Require(atlas != null, label + " catalog atlas resource is missing");
            Require(atlas.width == atlasImage.Size.Width && atlas.height == atlasImage.Size.Height,
                label + " imported atlas dimensions changed");
            Vector2 pivot = new(manifest.pivotNormalized[0], manifest.pivotNormalized[1]);
            VerifyBridgeReadableImporter(
                atlas,
                atlasPath,
                manifest.pixelsPerUnit,
                pivot,
                label + " atlas");
            VerifySerializedBridgeSequence(
                catalog,
                sequenceId,
                atlas,
                frameSize,
                sourceFrames.Length,
                manifest,
                label);
            for (int frame = 0; frame < sourceFrames.Length; frame++)
            {
                Require(catalog.TryGetSequenceSprite(sequenceId, frame, out Sprite sprite)
                        && sprite != null,
                    label + " catalog frame is missing: " + frame);
                VerifyOwnedBridgeCatalogFrame(
                    sprite,
                    atlas,
                    atlasPath,
                    frameSize,
                    sourceFrames.Length,
                    frame,
                    manifest,
                    label + " catalog frame " + frame);
            }
        }

        private static void VerifyBridgeAtlasFramePixels(
            RawSpriteImage atlas,
            RawSpriteImage source,
            int[] frameSize,
            int frame,
            string label)
        {
            Require(source.Size.Width == frameSize[0] && source.Size.Height == frameSize[1],
                label + " source dimensions changed");
            for (int y = 0; y < frameSize[1]; y++)
            {
                int atlasRow = y * atlas.Size.Width + frame * frameSize[0];
                int sourceRow = y * frameSize[0];
                for (int x = 0; x < frameSize[0]; x++)
                {
                    Require(AuthoredCompositedPixelsEquivalent(
                            source.Pixels[sourceRow + x],
                            atlas.Pixels[atlasRow + x]),
                        label + $" differs from its authored module at {x},{y}");
                }
            }
        }

        private static void VerifySerializedBridgeSequence(
            StrategyVisualCatalog catalog,
            string sequenceId,
            Texture2D atlas,
            int[] frameSize,
            int frameCount,
            AuthoredBridgeManifest manifest,
            string label)
        {
            SerializedProperty sequences = new SerializedObject(catalog).FindProperty("visualSequences");
            Require(sequences != null && sequences.isArray, "Visual catalog sequence data is missing");
            int matches = 0;
            for (int i = 0; i < sequences.arraySize; i++)
            {
                SerializedProperty sequence = sequences.GetArrayElementAtIndex(i);
                if (sequence.FindPropertyRelative("id").stringValue != sequenceId)
                {
                    continue;
                }

                matches++;
                Require(sequence.FindPropertyRelative("atlas").objectReferenceValue == atlas,
                    label + " serialized sequence owns a fallback atlas");
                Require(sequence.FindPropertyRelative("frameWidth").intValue == frameSize[0]
                        && sequence.FindPropertyRelative("frameHeight").intValue == frameSize[1]
                        && sequence.FindPropertyRelative("frameCount").intValue == frameCount,
                    label + " serialized frame layout changed");
                Require(Approximately(sequence.FindPropertyRelative("pixelsPerUnit").floatValue,
                        manifest.pixelsPerUnit),
                    label + " serialized PPU changed");
                Vector2 expectedPivot = new(
                    manifest.pivotNormalized[0],
                    manifest.pivotNormalized[1]);
                Require(Vector2.Distance(sequence.FindPropertyRelative("pivot").vector2Value,
                        expectedPivot) <= AuthoredVisualTolerance,
                    label + " serialized pivot changed");
            }

            Require(matches == 1, label + " must have exactly one serialized catalog sequence");
        }

        private static void VerifyOwnedBridgeCatalogFrame(
            Sprite sprite,
            Texture2D atlas,
            string atlasPath,
            int[] frameSize,
            int frameCount,
            int frame,
            AuthoredBridgeManifest manifest,
            string label)
        {
            Require(sprite.texture == atlas, label + " does not own the exact Bridge atlas");
            Require(NormalizeAssetPath(AssetDatabase.GetAssetPath(sprite.texture))
                    == NormalizeAssetPath(atlasPath),
                label + " resolves to a fallback texture");
            int normalizedFrame = frame % frameCount;
            Require(Approximately(sprite.rect.x, normalizedFrame * frameSize[0])
                    && Approximately(sprite.rect.y, 0f)
                    && Approximately(sprite.rect.width, frameSize[0])
                    && Approximately(sprite.rect.height, frameSize[1]),
                label + " rect changed");
            Require(Approximately(sprite.pixelsPerUnit, manifest.pixelsPerUnit),
                label + " PPU changed");
            Vector2 expectedPivot = new(
                frameSize[0] * manifest.pivotNormalized[0],
                frameSize[1] * manifest.pivotNormalized[1]);
            Require(Vector2.Distance(sprite.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " pivot changed");
        }

        private static void VerifyBridgeCatalogSequenceCoverage(
            StrategyVisualCatalog catalog,
            HashSet<string> expectedIds)
        {
            SerializedProperty sequences = new SerializedObject(catalog).FindProperty("visualSequences");
            Require(sequences != null && sequences.isArray, "Visual catalog sequence data is missing");
            int bridgeSequences = 0;
            for (int i = 0; i < sequences.arraySize; i++)
            {
                string id = sequences.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("id").stringValue;
                if (!id.StartsWith("Bridge/Final/", StringComparison.Ordinal)
                    && !id.StartsWith("Bridge/Construction/", StringComparison.Ordinal))
                {
                    continue;
                }

                bridgeSequences++;
                Require(expectedIds.Contains(id),
                    "Visual catalog contains a stale Bridge sequence: " + id);
            }

            Require(bridgeSequences == expectedIds.Count,
                "Visual catalog Bridge sequence coverage differs from the authored manifest");
        }

        private static void VerifyAuthoredBridgeComposedSprites()
        {
            HashSet<Sprite> sprites = new();
            HashSet<Texture2D> textures = new();
            StrategyVisualCatalogProvider.ResetCache();
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBridgeVisualProfile.ResetRuntimeState();
            try
            {
                VerifyReadableBridgeBakeSources();
                for (int span = StrategyBridgeVisualProfile.MinimumSpanCells;
                     span <= StrategyBridgeVisualProfile.MaximumSpanCells;
                     span++)
                {
                    VerifyAuthoredBridgeOrientation(
                        new Vector2Int(span, 1),
                        sprites,
                        textures);
                    VerifyAuthoredBridgeOrientation(
                        new Vector2Int(1, span),
                        sprites,
                        textures);
                }
            }
            finally
            {
                StrategyBridgeVisualProfile.ResetRuntimeState();
                StrategyConstructionSpriteFactory.ResetCaches();
                StrategyBuildingSpriteFactory.ResetCaches();
                StrategyVisualCatalogProvider.ResetCache();
                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null)
                    {
                        UnityEngine.Object.DestroyImmediate(sprite);
                    }
                }

                foreach (Texture2D texture in textures)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
        }

        private static void VerifyReadableBridgeBakeSources()
        {
            List<Sprite> sources = new();
            try
            {
                sources.Add(StrategyVisualBakeSource.GetBuildingSprite(
                    StrategyBuildTool.Bridge,
                    0));
                for (int stage = 0; stage < StrategyConstructionSpriteFactory.StageCount; stage++)
                {
                    sources.Add(StrategyVisualBakeSource.GetConstructionSprite(
                        StrategyBuildTool.Bridge,
                        0,
                        stage));
                }

                Vector2Int expected = StrategyBridgeVisualProfile.GetOutputPixelSize(
                    new Vector2Int(3, 1));
                for (int i = 0; i < sources.Count; i++)
                {
                    Sprite source = sources[i];
                    Require(source != null && source.texture != null,
                        "Bridge catalog bake source is missing: " + i);
                    Require(source.name.Contains("Authored Bridge", StringComparison.Ordinal),
                        "Bridge catalog bake source used the procedural fallback: " + i);
                    Require(source.texture.isReadable,
                        "Bridge catalog bake source must keep its CPU pixels: " + i);
                    Require(Approximately(source.rect.width, expected.x)
                            && Approximately(source.rect.height, expected.y)
                            && Approximately(source.pixelsPerUnit,
                                StrategyBridgeVisualProfile.AuthoredPixelsPerUnit),
                        "Bridge catalog bake source geometry changed: " + i);
                    Require(source.texture.GetPixels32().Length == expected.x * expected.y,
                        "Bridge catalog bake source pixels are unavailable: " + i);
                }
            }
            finally
            {
                for (int i = 0; i < sources.Count; i++)
                {
                    Sprite source = sources[i];
                    if (source == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(source)))
                    {
                        continue;
                    }

                    Texture2D texture = source.texture;
                    UnityEngine.Object.DestroyImmediate(source);
                    if (texture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
            }
        }

        private static void VerifyAuthoredBridgeOrientation(
            Vector2Int footprint,
            HashSet<Sprite> sprites,
            HashSet<Texture2D> textures)
        {
            Sprite completed = StrategyBuildingSpriteFactory.GetBridgeSprite(footprint);
            VerifyComposedBridgeSprite(completed, footprint, "Completed",
                sprites, textures);
            for (int stage = 0; stage < StrategyConstructionSpriteFactory.StageCount; stage++)
            {
                Sprite construction = StrategyConstructionSpriteFactory.GetBridgeConstructionSprite(
                    footprint,
                    stage);
                VerifyComposedBridgeSprite(
                    construction,
                    footprint,
                    "Construction Stage " + (stage + 1),
                    sprites,
                    textures);
            }
        }

        private static void VerifyComposedBridgeSprite(
            Sprite sprite,
            Vector2Int footprint,
            string expectedKind,
            HashSet<Sprite> sprites,
            HashSet<Texture2D> textures)
        {
            string label = $"Bridge {footprint.x}x{footprint.y} {expectedKind}";
            Require(sprite != null && sprite.texture != null, label + " runtime sprite is missing");
            Require(sprites.Add(sprite), label + " reused another composed Sprite instance");
            Require(textures.Add(sprite.texture), label + " reused another composed texture instance");
            Require(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite))
                    && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite.texture)),
                label + " must be a runtime-composed sprite");
            Require(sprite.name.Contains("Authored Bridge " + expectedKind, StringComparison.Ordinal),
                label + " used the procedural fallback");

            Vector2Int expectedSize = StrategyBridgeVisualProfile.GetOutputPixelSize(footprint);
            Require(Approximately(sprite.rect.x, 0f) && Approximately(sprite.rect.y, 0f)
                    && Approximately(sprite.rect.width, expectedSize.x)
                    && Approximately(sprite.rect.height, expectedSize.y)
                    && sprite.texture.width == expectedSize.x
                    && sprite.texture.height == expectedSize.y,
                label + " composed dimensions changed");
            Require(Approximately(sprite.pixelsPerUnit,
                    StrategyBridgeVisualProfile.AuthoredPixelsPerUnit),
                label + " PPU changed");
            Vector2 expectedPivot = new(expectedSize.x * 0.5f, expectedSize.y * 0.5f);
            Require(Vector2.Distance(sprite.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " pivot changed");
            Require(sprite.texture.filterMode == FilterMode.Bilinear,
                label + " must use Bilinear filtering after high-resolution composition");
            Require(sprite.texture.wrapMode == TextureWrapMode.Clamp,
                label + " must use Clamp wrapping");
            Require(!sprite.texture.isReadable,
                label + " composed texture must release its CPU copy");
            VerifyBridgeLegacyWorldSize(sprite, footprint, label);
        }

        private static void VerifyBridgeLegacyWorldSize(
            Sprite sprite,
            Vector2Int footprint,
            string label)
        {
            bool horizontal = footprint.x >= footprint.y;
            int span = horizontal ? footprint.x : footprint.y;
            Vector2 legacyPixels = horizontal
                ? new Vector2(Mathf.Max(72, span * 24 + 20), 56)
                : new Vector2(62, Mathf.Max(72, span * 24 + 20));
            Require(Approximately(sprite.rect.width / sprite.pixelsPerUnit,
                        legacyPixels.x / 24f)
                    && Approximately(sprite.rect.height / sprite.pixelsPerUnit,
                        legacyPixels.y / 24f),
                label + " no longer preserves the procedural world dimensions");
        }
    }
}
