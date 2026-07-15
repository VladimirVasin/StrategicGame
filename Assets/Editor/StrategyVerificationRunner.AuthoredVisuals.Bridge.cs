using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        internal static void VerifyAuthoredBridgeVisuals()
        {
            StrategyVisualCatalog catalog =
                Resources.Load<StrategyVisualCatalog>("Visual/StrategyVisualCatalog");
            Require(catalog != null, "Visual catalog resource is missing");
            StrategyVisualCatalogProvider.ResetCache();
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            StrategyBridgeVisualProfile.ResetRuntimeState();
            try
            {
                AuthoredBridgeManifest manifest =
                    LoadAuthoredManifest<AuthoredBridgeManifest>(AuthoredBridgeManifestPath);
                VerifyAuthoredBridgeManifestRoot(manifest);

                HashSet<string> moduleKeys = new(StringComparer.Ordinal);
                HashSet<string> outputPaths = new(StringComparer.OrdinalIgnoreCase);
                HashSet<string> sequenceIds = new(StringComparer.Ordinal);
                for (int i = 0; i < manifest.modules.Length; i++)
                {
                    VerifyAuthoredBridgeModule(
                        catalog,
                        manifest,
                        manifest.modules[i],
                        moduleKeys,
                        outputPaths,
                        sequenceIds);
                }

                Require(moduleKeys.Count == 6,
                    "Authored Bridge manifest must cover six orientation/module pairs");
                Require(outputPaths.Count == 48,
                    "Authored Bridge manifest must own six final and 42 construction PNGs");
                Require(sequenceIds.Count == 12,
                    "Authored Bridge manifest must own 12 catalog sequences");
                VerifyBridgeCatalogSequenceCoverage(catalog, sequenceIds);
                VerifyAuthoredBridgeComposedSprites();
            }
            finally
            {
                StrategyBridgeVisualProfile.ResetRuntimeState();
                StrategyConstructionSpriteFactory.ResetCaches();
                StrategyBuildingSpriteFactory.ResetCaches();
                StrategyVisualCatalogProvider.ResetCache();
            }
        }

        private static void VerifyAuthoredBridgeManifestRoot(AuthoredBridgeManifest manifest)
        {
            Require(manifest.schemaVersion == 1, "Unsupported authored Bridge manifest schema");
            RequirePair(manifest.sourceSize, "Bridge sourceSize");
            Require(manifest.pixelsPerUnit > 0f && float.IsFinite(manifest.pixelsPerUnit),
                "Bridge pixelsPerUnit is invalid");
            RequirePair(manifest.pivotNormalized, "Bridge pivotNormalized", true);
            Require(Approximately(manifest.pixelsPerUnit,
                    StrategyBridgeVisualProfile.AuthoredPixelsPerUnit),
                "Bridge manifest PPU differs from the runtime profile");
            Require(Approximately(manifest.pivotNormalized[0], 0.5f)
                    && Approximately(manifest.pivotNormalized[1], 0.5f),
                "Bridge modules must keep their centered runtime pivot");
            Require(manifest.constructionStageCount == StrategyConstructionSpriteFactory.StageCount,
                "Bridge construction stage count differs from the runtime factory");
            Require(manifest.modules != null && manifest.modules.Length == 6,
                "Authored Bridge manifest must contain exactly six modules");

            VerifyExactSha256(manifest.source, manifest.sourceSha256, "Bridge source");
            RawSpriteImage source = LoadRawSpriteImage(manifest.source, "Bridge source");
            Require(source.Size.Width == manifest.sourceSize[0]
                    && source.Size.Height == manifest.sourceSize[1],
                "Bridge source dimensions changed");
        }

        private static void VerifyAuthoredBridgeModule(
            StrategyVisualCatalog catalog,
            AuthoredBridgeManifest manifest,
            AuthoredBridgeModule authored,
            HashSet<string> moduleKeys,
            HashSet<string> outputPaths,
            HashSet<string> sequenceIds)
        {
            Require(authored != null, "Authored Bridge module entry is null");
            GetBridgeModuleIdentity(authored, out bool horizontal,
                out StrategyBridgeVisualProfile.Module module);
            string label = $"Bridge {authored.orientation} {authored.module}";
            Require(moduleKeys.Add(authored.orientation + "/" + authored.module),
                "Duplicate authored " + label);
            VerifyBridgeModuleGeometry(manifest, authored, horizontal, module, label);

            string expectedFinalPath = GetBridgeModuleAssetPath(
                horizontal,
                module,
                construction: false,
                stage: 0);
            Require(NormalizeAssetPath(authored.output) == expectedFinalPath,
                label + " final output path differs from the runtime Resources contract");
            Require(outputPaths.Add(NormalizeAssetPath(authored.output)),
                "Duplicate authored Bridge output: " + authored.output);
            VerifyExactSha256(authored.output, authored.expectedSha256, label + " final");
            RawSpriteImage finalImage = LoadRawSpriteImage(authored.output, label + " final");
            VerifyBridgeModuleImage(finalImage, authored, exactTargetBounds: true, label + " final");
            VerifyBridgeSeamPixels(finalImage, authored.seamEdge, label + " final");
            VerifyBridgeModuleResource(
                authored.output,
                authored.outputCanvas,
                manifest.pixelsPerUnit,
                manifest.pivotNormalized,
                label + " final");

            Require(authored.construction != null
                    && authored.construction.Length == manifest.constructionStageCount,
                label + " construction frame count differs from the manifest contract");
            RawSpriteImage[] constructionFrames =
                new RawSpriteImage[manifest.constructionStageCount];
            for (int frame = 0; frame < constructionFrames.Length; frame++)
            {
                AuthoredBridgeConstructionFrame construction = authored.construction[frame];
                string frameLabel = label + " construction stage " + (frame + 1);
                Require(construction != null && construction.stage == frame + 1,
                    frameLabel + " is missing or out of order");
                string expectedPath = GetBridgeModuleAssetPath(
                    horizontal,
                    module,
                    construction: true,
                    stage: frame);
                Require(NormalizeAssetPath(construction.output) == expectedPath,
                    frameLabel + " output path differs from the runtime Resources contract");
                Require(outputPaths.Add(NormalizeAssetPath(construction.output)),
                    "Duplicate authored Bridge output: " + construction.output);
                VerifyExactSha256(construction.output, construction.expectedSha256, frameLabel);
                constructionFrames[frame] = LoadRawSpriteImage(construction.output, frameLabel);
                VerifyBridgeModuleImage(
                    constructionFrames[frame],
                    authored,
                    exactTargetBounds: false,
                    frameLabel);
                VerifyBridgeSeamPixels(
                    constructionFrames[frame],
                    authored.seamEdge,
                    frameLabel);
                bool isFinalStage = frame == constructionFrames.Length - 1;
                bool matchesFinal = BridgeImagesEquivalent(
                    constructionFrames[frame],
                    finalImage);
                Require(matchesFinal == isFinalStage,
                    isFinalStage
                        ? frameLabel + " must reproduce the completed module exactly"
                        : frameLabel + " must remain visually distinct from the completed module");
                Require(string.Equals(
                            construction.expectedSha256,
                            authored.expectedSha256,
                            StringComparison.OrdinalIgnoreCase) == isFinalStage,
                    frameLabel + " final-module hash relationship changed");
                VerifyBridgeModuleResource(
                    construction.output,
                    authored.outputCanvas,
                    manifest.pixelsPerUnit,
                    manifest.pivotNormalized,
                    frameLabel);
            }

            string finalId = StrategyBridgeVisualProfile.GetCatalogSequenceId(
                horizontal,
                module,
                construction: false);
            string constructionId = StrategyBridgeVisualProfile.GetCatalogSequenceId(
                horizontal,
                module,
                construction: true);
            Require(sequenceIds.Add(finalId) && sequenceIds.Add(constructionId),
                label + " produced duplicate catalog sequence IDs");
            VerifyBridgeCatalogSequence(
                catalog,
                finalId,
                GetBridgeAtlasAssetPath(horizontal, module, construction: false),
                new[] { finalImage },
                authored.outputCanvas,
                manifest,
                label + " final sequence");
            VerifyBridgeCatalogSequence(
                catalog,
                constructionId,
                GetBridgeAtlasAssetPath(horizontal, module, construction: true),
                constructionFrames,
                authored.outputCanvas,
                manifest,
                label + " construction sequence");
        }

        private static void GetBridgeModuleIdentity(
            AuthoredBridgeModule authored,
            out bool horizontal,
            out StrategyBridgeVisualProfile.Module module)
        {
            Require(authored.orientation == "Horizontal" || authored.orientation == "Vertical",
                "Authored Bridge module has an unknown orientation: " + authored.orientation);
            horizontal = authored.orientation == "Horizontal";
            Require(Enum.TryParse(authored.module, false, out module)
                    && Enum.IsDefined(typeof(StrategyBridgeVisualProfile.Module), module),
                "Authored Bridge module has an unknown role: " + authored.module);
        }

        private static void VerifyBridgeModuleGeometry(
            AuthoredBridgeManifest manifest,
            AuthoredBridgeModule authored,
            bool horizontal,
            StrategyBridgeVisualProfile.Module module,
            string label)
        {
            RequireRect(authored.sourceBounds, label + " sourceBounds");
            Require(authored.sourceBounds[0] + authored.sourceBounds[2] <= manifest.sourceSize[0]
                    && authored.sourceBounds[1] + authored.sourceBounds[3] <= manifest.sourceSize[1],
                label + " source crop leaves the declared source image");
            RequirePair(authored.outputCanvas, label + " outputCanvas");
            Vector2Int expected = StrategyBridgeVisualProfile.GetModulePixelSize(horizontal, module);
            Require(authored.outputCanvas[0] == expected.x && authored.outputCanvas[1] == expected.y,
                label + " output canvas differs from the runtime module profile");
            RequireRect(authored.targetBoundsTopLeft, label + " targetBoundsTopLeft");
            Require(authored.targetBoundsTopLeft[0] + authored.targetBoundsTopLeft[2]
                        <= authored.outputCanvas[0]
                    && authored.targetBoundsTopLeft[1] + authored.targetBoundsTopLeft[3]
                        <= authored.outputCanvas[1],
                label + " target alpha bounds leave the output canvas");
            string expectedSeam = GetExpectedBridgeSeam(horizontal, module);
            Require(authored.seamEdge == expectedSeam,
                label + " seam edge must be " + expectedSeam);
        }

        private static string GetExpectedBridgeSeam(
            bool horizontal,
            StrategyBridgeVisualProfile.Module module)
        {
            if (horizontal)
            {
                return module == StrategyBridgeVisualProfile.Module.Start
                    ? "Right"
                    : module == StrategyBridgeVisualProfile.Module.Middle
                        ? "BothHorizontal"
                        : "Left";
            }

            return module == StrategyBridgeVisualProfile.Module.Start
                ? "Top"
                : module == StrategyBridgeVisualProfile.Module.Middle
                    ? "BothVertical"
                    : "Bottom";
        }

        private static void VerifyBridgeModuleImage(
            RawSpriteImage image,
            AuthoredBridgeModule authored,
            bool exactTargetBounds,
            string label)
        {
            Require(image.Size.Width == authored.outputCanvas[0]
                    && image.Size.Height == authored.outputCanvas[1],
                label + " dimensions changed");
            VerifyVisiblePixelQuality(image, label, false, false);
            int chromaPixels = 0;
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                Color32 pixel = image.Pixels[i];
                if (pixel.a >= 16 && pixel.r >= 238 && pixel.g <= 48 && pixel.b >= 238)
                {
                    chromaPixels++;
                }
            }

            Require(chromaPixels == 0, label + " contains visible magenta chroma-key pixels");
            if (!exactTargetBounds)
            {
                return;
            }

            RectInt expected = new(
                authored.targetBoundsTopLeft[0],
                authored.targetBoundsTopLeft[1],
                authored.targetBoundsTopLeft[2],
                authored.targetBoundsTopLeft[3]);
            Require(GetAlphaBoundsTopLeft(image, label) == expected,
                label + " alpha bounds changed from the manifest contract");
        }

        private static void VerifyBridgeSeamPixels(
            RawSpriteImage image,
            string seamEdge,
            string label)
        {
            bool left = BridgeEdgeHasAlpha(image, "Left");
            bool right = BridgeEdgeHasAlpha(image, "Right");
            bool top = BridgeEdgeHasAlpha(image, "Top");
            bool bottom = BridgeEdgeHasAlpha(image, "Bottom");
            bool valid = seamEdge switch
            {
                "Left" => left,
                "Right" => right,
                "Top" => top,
                "Bottom" => bottom,
                "BothHorizontal" => left && right,
                "BothVertical" => top && bottom,
                _ => false
            };
            Require(valid, label + " has transparent pixels across a declared seam edge");
        }

        private static bool BridgeImagesEquivalent(RawSpriteImage left, RawSpriteImage right)
        {
            if (left.Size.Width != right.Size.Width || left.Size.Height != right.Size.Height)
            {
                return false;
            }

            for (int i = 0; i < left.Pixels.Length; i++)
            {
                if (!AuthoredPixelsEquivalent(left.Pixels[i], right.Pixels[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BridgeEdgeHasAlpha(RawSpriteImage image, string edge)
        {
            int count = edge == "Left" || edge == "Right"
                ? image.Size.Height
                : image.Size.Width;
            for (int offset = 0; offset < count; offset++)
            {
                int x = edge == "Left" ? 0
                    : edge == "Right" ? image.Size.Width - 1 : offset;
                int y = edge == "Bottom" ? 0
                    : edge == "Top" ? image.Size.Height - 1 : offset;
                if (image.Pixels[y * image.Size.Width + x].a >= 16)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetBridgeModuleAssetPath(
            bool horizontal,
            StrategyBridgeVisualProfile.Module module,
            bool construction,
            int stage)
        {
            string resourcePath = StrategyBridgeVisualProfile.GetResourcePath(
                horizontal,
                module,
                construction,
                stage);
            return "Assets/Resources/" + resourcePath + ".png";
        }

        private static string GetBridgeAtlasAssetPath(
            bool horizontal,
            StrategyBridgeVisualProfile.Module module,
            bool construction)
        {
            string orientation = horizontal ? "Horizontal" : "Vertical";
            string kind = construction ? "Construction" : "Final";
            return $"Assets/Resources/Visual/Baked/Bridge/{kind}/{orientation}/{module}.png";
        }

        private static void VerifyBridgeModuleResource(
            string assetPath,
            int[] size,
            float pixelsPerUnit,
            float[] pivotNormalized,
            string label)
        {
            Sprite sprite = Resources.Load<Sprite>(GetResourcePath(assetPath));
            Require(sprite != null && sprite.texture != null,
                label + " authored Sprite resource is missing");
            Require(NormalizeAssetPath(AssetDatabase.GetAssetPath(sprite))
                    == NormalizeAssetPath(assetPath),
                label + " Sprite resolves to a fallback asset");
            Require(Approximately(sprite.rect.width, size[0])
                    && Approximately(sprite.rect.height, size[1]),
                label + " imported Sprite dimensions changed");
            Require(Approximately(sprite.pixelsPerUnit, pixelsPerUnit),
                label + " imported Sprite PPU changed");
            Vector2 expectedPivot = new(
                size[0] * pivotNormalized[0],
                size[1] * pivotNormalized[1]);
            Require(Vector2.Distance(sprite.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " imported Sprite pivot changed");
            VerifyBridgeReadableImporter(
                sprite.texture,
                assetPath,
                pixelsPerUnit,
                new Vector2(pivotNormalized[0], pivotNormalized[1]),
                label);
        }

        private static void VerifyBridgeReadableImporter(
            Texture2D texture,
            string expectedPath,
            float expectedPpu,
            Vector2 expectedPivot,
            string label)
        {
            Require(texture != null, label + " texture is missing");
            string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(texture));
            Require(assetPath == NormalizeAssetPath(expectedPath),
                label + " texture resolves to " + assetPath);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, label + " texture importer is missing");
            Require(importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single,
                label + " must stay Sprite/Single");
            Require(importer.filterMode == FilterMode.Point, label + " must use Point filtering");
            Require(Approximately(importer.spritePixelsPerUnit, expectedPpu),
                label + " importer PPU changed");
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            Require(settings.spriteAlignment == (int)SpriteAlignment.Custom,
                label + " importer must keep a custom pivot");
            Require(Vector2.Distance(importer.spritePivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " importer pivot changed");
            Require(!importer.mipmapEnabled, label + " mipmaps must stay disabled");
            Require(importer.isReadable,
                label + " must stay CPU-readable for deterministic Bridge composition");
            Require(importer.alphaIsTransparency, label + " must import alpha as transparency");
            Require(importer.wrapMode == TextureWrapMode.Clamp, label + " must use Clamp wrapping");
            Require(importer.npotScale == TextureImporterNPOTScale.None,
                label + " must preserve NPOT dimensions");
            Require(importer.textureCompression == TextureImporterCompression.Uncompressed,
                label + " must stay uncompressed");
            Require(importer.maxTextureSize >= Mathf.Max(texture.width, texture.height),
                label + " importer max size would downscale the texture");
        }
    }
}
