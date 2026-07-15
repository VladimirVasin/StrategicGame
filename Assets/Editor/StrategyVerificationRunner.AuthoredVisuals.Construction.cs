using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static void VerifyAuthoredConstructionManifest(
            StrategyVisualCatalog catalog,
            AuthoredConstructionManifest manifest,
            Dictionary<string, AuthoredBuildingFamily> buildingFamilies,
            HashSet<string> outputPaths)
        {
            HashSet<string> constructionFamilies = new(StringComparer.Ordinal);
            for (int familyIndex = 0; familyIndex < manifest.families.Length; familyIndex++)
            {
                AuthoredConstructionFamily family = manifest.families[familyIndex];
                Require(family != null && !string.IsNullOrWhiteSpace(family.tool),
                    "Authored construction family has no tool");
                Require(constructionFamilies.Add(family.tool),
                    "Duplicate authored construction family: " + family.tool);
                Require(buildingFamilies.TryGetValue(family.tool, out AuthoredBuildingFamily buildingFamily),
                    family.tool + " construction family has no authored final family");
                VerifyAuthoredConstructionFamily(
                    catalog,
                    family,
                    buildingFamily,
                    outputPaths);
            }
        }

        private static void VerifyAuthoredConstructionFamily(
            StrategyVisualCatalog catalog,
            AuthoredConstructionFamily family,
            AuthoredBuildingFamily buildingFamily,
            HashSet<string> outputPaths)
        {
            Require(
                Enum.TryParse(family.tool, false, out StrategyBuildTool tool)
                    && Enum.IsDefined(typeof(StrategyBuildTool), tool),
                "Unknown authored construction tool: " + family.tool);
            Require(family.frameCount > 0, family.tool + " construction frameCount must be positive");
            VerifyFrameContracts(family);
            Require(family.variants != null && family.variants.Length == buildingFamily.variants.Length,
                family.tool + " construction variants do not match final variants");

            for (int variantIndex = 0; variantIndex < family.variants.Length; variantIndex++)
            {
                AuthoredConstructionVariant variant = family.variants[variantIndex];
                AuthoredBuildingVariant finalVariant = buildingFamily.variants[variantIndex];
                string expectedId = $"V{variantIndex + 1:00}";
                Require(variant != null && variant.id == expectedId,
                    family.tool + " construction variants must be ordered and named " + expectedId);
                Require(outputPaths.Add(NormalizeAssetPath(variant.output)),
                    "Duplicate authored output path: " + variant.output);
                VerifyConstructionFinalLink(family, variant, finalVariant);
                VerifyAuthoredConstructionVariant(
                    catalog,
                    tool,
                    variantIndex,
                    family,
                    variant);
            }
        }

        private static void VerifyFrameContracts(AuthoredConstructionFamily family)
        {
            string label = family.tool + " construction";
            Require(family.sourceFrame != null && family.outputFrame != null,
                label + " frame contracts are missing");
            RequirePair(family.sourceFrame.size, label + " sourceFrame.size");
            RequirePair(family.outputFrame.size, label + " outputFrame.size");
            RequirePair(family.sourceFrame.pivotPixelsBottomLeft,
                label + " sourceFrame.pivotPixelsBottomLeft", false);
            RequirePair(family.outputFrame.pivotPixelsBottomLeft,
                label + " outputFrame.pivotPixelsBottomLeft", false);
            Require(family.sourceFrame.ppu > 0f && float.IsFinite(family.sourceFrame.ppu),
                label + " source PPU is invalid");
            Require(family.outputFrame.ppu > 0f && float.IsFinite(family.outputFrame.ppu),
                label + " output PPU is invalid");
            RequirePivotInsideFrame(family.sourceFrame, label + " source frame");
            RequirePivotInsideFrame(family.outputFrame, label + " output frame");

            float sourceToOutputScale = family.outputFrame.ppu / family.sourceFrame.ppu;
            Require(sourceToOutputScale >= 1f && float.IsFinite(sourceToOutputScale),
                label + " output PPU cannot downscale the source contract");
            Require(
                family.outputFrame.size[0]
                    >= Mathf.CeilToInt(family.sourceFrame.size[0] * sourceToOutputScale)
                    && family.outputFrame.size[1]
                    >= Mathf.CeilToInt(family.sourceFrame.size[1] * sourceToOutputScale),
                label + " output canvas no longer contains the source frame at world scale");
        }

        private static void RequirePivotInsideFrame(AuthoredFrameContract frame, string label)
        {
            Require(
                frame.pivotPixelsBottomLeft[0] >= 0f
                    && frame.pivotPixelsBottomLeft[0] <= frame.size[0]
                    && frame.pivotPixelsBottomLeft[1] >= 0f
                    && frame.pivotPixelsBottomLeft[1] <= frame.size[1],
                label + " pivot leaves the frame");
        }

        private static void VerifyConstructionFinalLink(
            AuthoredConstructionFamily family,
            AuthoredConstructionVariant variant,
            AuthoredBuildingVariant finalVariant)
        {
            string label = family.tool + " construction " + variant.id;
            Require(NormalizeAssetPath(variant.finalSprite) == NormalizeAssetPath(finalVariant.output),
                label + " points at the wrong final sprite");
            Require(string.Equals(variant.finalSha256, finalVariant.expectedSha256,
                    StringComparison.OrdinalIgnoreCase),
                label + " final SHA does not match the final manifest");
            VerifyExactSha256(variant.finalSprite, variant.finalSha256, label + " final sprite");
            Require(Approximately(variant.finalPpu, finalVariant.ppu),
                label + " final PPU differs between manifests");
            RequirePair(variant.finalPivotNormalized, label + " finalPivotNormalized", true);
            Require(
                Approximately(variant.finalPivotNormalized[0], finalVariant.pivotNormalized[0])
                    && Approximately(variant.finalPivotNormalized[1], finalVariant.pivotNormalized[1]),
                label + " final pivot differs between manifests");

            float sourceToOutputScale = family.outputFrame.ppu / family.sourceFrame.ppu;
            float finalToOutputScale = family.outputFrame.ppu / variant.finalPpu;
            int expectedWidth = Mathf.Max(
                Mathf.CeilToInt(family.sourceFrame.size[0] * sourceToOutputScale),
                Mathf.CeilToInt(finalVariant.outputCanvas[0] * finalToOutputScale));
            int expectedHeight = Mathf.Max(
                Mathf.CeilToInt(family.sourceFrame.size[1] * sourceToOutputScale),
                Mathf.CeilToInt(finalVariant.outputCanvas[1] * finalToOutputScale));
            Require(
                family.outputFrame.size[0] == expectedWidth
                    && family.outputFrame.size[1] == expectedHeight,
                label + " construction canvas is not the minimum frame that contains source and final art");

            float constructionPivotX =
                family.outputFrame.pivotPixelsBottomLeft[0] / family.outputFrame.size[0];
            float constructionPivotWorldY =
                family.outputFrame.pivotPixelsBottomLeft[1] / family.outputFrame.ppu;
            float finalPivotWorldY =
                finalVariant.outputCanvas[1] * variant.finalPivotNormalized[1] / variant.finalPpu;
            Require(Approximately(constructionPivotX, variant.finalPivotNormalized[0]),
                label + " construction X pivot is not aligned with the final sprite");
            Require(Approximately(constructionPivotWorldY, finalPivotWorldY),
                label + " construction/final pivots do not align in world units");
        }

        private static void VerifyAuthoredConstructionVariant(
            StrategyVisualCatalog catalog,
            StrategyBuildTool tool,
            int variantIndex,
            AuthoredConstructionFamily family,
            AuthoredConstructionVariant variant)
        {
            string label = family.tool + " construction " + variant.id;
            VerifyExactSha256(variant.source, variant.sourceSha256, label + " source");
            VerifyExactSha256(variant.output, variant.expectedSha256, label + " output");
            RawSpriteImage source = LoadRawSpriteImage(variant.source, label + " source");
            RawSpriteImage output = LoadRawSpriteImage(variant.output, label + " output");
            Require(
                source.Size.Width == family.sourceFrame.size[0] * family.frameCount
                    && source.Size.Height == family.sourceFrame.size[1],
                label + " source atlas dimensions changed");
            Require(
                output.Size.Width == family.outputFrame.size[0] * family.frameCount
                    && output.Size.Height == family.outputFrame.size[1],
                label + " output atlas dimensions changed");
            VerifyConstructionFramePixels(output, family, label);
            Require(!IsExactNearestNeighbor2x(output, source),
                label + " is only an exact nearest-neighbor 2x copy of its source atlas");

            Texture2D atlas = Resources.Load<Texture2D>(GetResourcePath(variant.output));
            Require(atlas != null, label + " authored atlas resource is missing");
            Require(atlas.width == output.Size.Width && atlas.height == output.Size.Height,
                label + " imported atlas dimensions changed");
            VerifyAuthoredImporter(atlas, variant.output, family.outputFrame.ppu, label);

            string sequenceId = $"Construction/{tool}/V{variantIndex}";
            VerifySerializedConstructionSequence(catalog, sequenceId, atlas, family, label);
            VerifyConstructionSequenceFrames(
                catalog,
                tool,
                variantIndex,
                sequenceId,
                atlas,
                family,
                label);
        }

        private static void VerifyConstructionFramePixels(
            RawSpriteImage atlas,
            AuthoredConstructionFamily family,
            string label)
        {
            int frameWidth = family.outputFrame.size[0];
            int frameHeight = family.outputFrame.size[1];
            for (int frame = 0; frame < family.frameCount; frame++)
            {
                int visible = 0;
                int transparent = 0;
                int magenta = 0;
                int maxY = -1;
                for (int y = 0; y < frameHeight; y++)
                {
                    int row = y * atlas.Size.Width + frame * frameWidth;
                    for (int x = 0; x < frameWidth; x++)
                    {
                        Color32 pixel = atlas.Pixels[row + x];
                        if (pixel.a < AuthoredVisibleAlphaThreshold)
                        {
                            transparent++;
                            continue;
                        }

                        visible++;
                        maxY = Mathf.Max(maxY, y);
                        if (pixel.r >= 238 && pixel.g <= 48 && pixel.b >= 238)
                        {
                            magenta++;
                        }
                    }
                }

                string frameLabel = label + " frame " + frame;
                Require(visible > 0, frameLabel + " has no visible pixels");
                Require(transparent > 0, frameLabel + " has no transparent background");
                Require(magenta == 0, frameLabel + " contains visible magenta chroma-key pixels");
                Require(maxY < frameHeight - 1,
                    frameLabel + " visible alpha is clipped by the top canvas edge");
            }
        }

        private static void VerifySerializedConstructionSequence(
            StrategyVisualCatalog catalog,
            string sequenceId,
            Texture2D atlas,
            AuthoredConstructionFamily family,
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
                Require(sequence.FindPropertyRelative("frameWidth").intValue == family.outputFrame.size[0]
                        && sequence.FindPropertyRelative("frameHeight").intValue == family.outputFrame.size[1]
                        && sequence.FindPropertyRelative("frameCount").intValue == family.frameCount,
                    label + " serialized sequence frame layout changed");
                Require(Approximately(sequence.FindPropertyRelative("pixelsPerUnit").floatValue,
                        family.outputFrame.ppu),
                    label + " serialized sequence PPU changed");
                Vector2 expectedPivot = GetConstructionPivotNormalized(family);
                Require(Vector2.Distance(sequence.FindPropertyRelative("pivot").vector2Value, expectedPivot)
                        <= AuthoredVisualTolerance,
                    label + " serialized sequence pivot changed");
            }

            Require(matches == 1, label + " must have exactly one serialized catalog sequence");
        }

        private static void VerifyConstructionSequenceFrames(
            StrategyVisualCatalog catalog,
            StrategyBuildTool tool,
            int variantIndex,
            string sequenceId,
            Texture2D atlas,
            AuthoredConstructionFamily family,
            string label)
        {
            Vector2 expectedPivotPixels = new(
                family.outputFrame.pivotPixelsBottomLeft[0],
                family.outputFrame.pivotPixelsBottomLeft[1]);
            for (int frame = 0; frame < family.frameCount; frame++)
            {
                Require(catalog.TryGetSequenceSprite(sequenceId, frame, out Sprite catalogSprite)
                        && catalogSprite != null,
                    label + " catalog frame is missing: " + frame);
                VerifyOwnedConstructionFrame(
                    catalogSprite,
                    atlas,
                    family,
                    frame,
                    expectedPivotPixels,
                    label + " catalog frame");

                if (tool == StrategyBuildTool.Bridge
                    || frame >= StrategyConstructionSpriteFactory.StageCount)
                {
                    continue;
                }

                Sprite runtimeSprite = StrategyConstructionSpriteFactory.GetConstructionSprite(
                    tool,
                    variantIndex,
                    frame);
                Require(runtimeSprite != null, label + " runtime frame is missing: " + frame);
                VerifyOwnedConstructionFrame(
                    runtimeSprite,
                    atlas,
                    family,
                    frame,
                    expectedPivotPixels,
                    label + " runtime frame");
            }
        }

        private static void VerifyOwnedConstructionFrame(
            Sprite sprite,
            Texture2D atlas,
            AuthoredConstructionFamily family,
            int frame,
            Vector2 expectedPivotPixels,
            string label)
        {
            Require(sprite.texture == atlas, label + " does not own the exact authored atlas");
            Require(
                Approximately(sprite.rect.x, frame * family.outputFrame.size[0])
                    && Approximately(sprite.rect.y, 0f)
                    && Approximately(sprite.rect.width, family.outputFrame.size[0])
                    && Approximately(sprite.rect.height, family.outputFrame.size[1]),
                label + " rect changed: " + frame);
            Require(Approximately(sprite.pixelsPerUnit, family.outputFrame.ppu),
                label + " PPU changed: " + frame);
            Require(Vector2.Distance(sprite.pivot, expectedPivotPixels) <= AuthoredVisualTolerance,
                label + " pivot changed: " + frame);
        }

        private static Vector2 GetConstructionPivotNormalized(AuthoredConstructionFamily family)
        {
            return new Vector2(
                family.outputFrame.pivotPixelsBottomLeft[0] / family.outputFrame.size[0],
                family.outputFrame.pivotPixelsBottomLeft[1] / family.outputFrame.size[1]);
        }
    }
}
