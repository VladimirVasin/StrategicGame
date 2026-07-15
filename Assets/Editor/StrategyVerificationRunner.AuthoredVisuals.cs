using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        internal static void VerifyAuthoredBuildingManifests()
        {
            StrategyVisualCatalog catalog = Resources.Load<StrategyVisualCatalog>("Visual/StrategyVisualCatalog");
            Require(catalog != null, "Visual catalog resource is missing");
            VerifyAuthoredBuildingManifests(catalog);
        }

        private static void VerifyAuthoredBuildingManifests(StrategyVisualCatalog catalog)
        {
            AuthoredBuildingManifest buildingManifest =
                LoadAuthoredManifest<AuthoredBuildingManifest>(AuthoredBuildingManifestPath);
            AuthoredConstructionManifest constructionManifest =
                LoadAuthoredManifest<AuthoredConstructionManifest>(AuthoredConstructionManifestPath);
            Require(buildingManifest.schemaVersion == 1, "Unsupported authored building manifest schema");
            Require(constructionManifest.schemaVersion == 1, "Unsupported construction manifest schema");
            Require(buildingManifest.families != null && buildingManifest.families.Length > 0,
                "Authored building manifest has no families");
            Require(constructionManifest.families != null,
                "Authored construction manifest family list is missing");

            Dictionary<string, AuthoredBuildingFamily> families =
                new(StringComparer.Ordinal);
            HashSet<string> outputPaths = new(StringComparer.OrdinalIgnoreCase);
            StrategyVisualCatalogProvider.ResetCache();
            StrategyBuildingSpriteFactory.ResetCaches();
            StrategyConstructionSpriteFactory.ResetCaches();
            try
            {
                for (int i = 0; i < buildingManifest.families.Length; i++)
                {
                    AuthoredBuildingFamily family = buildingManifest.families[i];
                    Require(family != null && !string.IsNullOrWhiteSpace(family.tool),
                        "Authored building family has no tool");
                    Require(families.TryAdd(family.tool, family),
                        "Duplicate authored building family: " + family.tool);
                    VerifyAuthoredBuildingFamily(catalog, family, outputPaths);
                }

                VerifyAuthoredConstructionManifest(
                    catalog,
                    constructionManifest,
                    families,
                    outputPaths);
            }
            finally
            {
                StrategyConstructionSpriteFactory.ResetCaches();
                StrategyBuildingSpriteFactory.ResetCaches();
                StrategyVisualCatalogProvider.ResetCache();
            }
        }

        private static void VerifyAuthoredBuildingFamily(
            StrategyVisualCatalog catalog,
            AuthoredBuildingFamily family,
            HashSet<string> outputPaths)
        {
            Require(
                Enum.TryParse(family.tool, false, out StrategyBuildTool tool)
                    && Enum.IsDefined(typeof(StrategyBuildTool), tool),
                "Unknown authored building tool: " + family.tool);
            RequirePair(family.sourceSize, family.tool + " sourceSize");
            Require(family.variants != null && family.variants.Length > 0,
                family.tool + " has no authored variants");
            Require(
                family.variants.Length == StrategyBuildingSpriteFactory.GetVariantCount(tool),
                family.tool + " manifest variant count does not match the runtime variant profile");
            VerifyExactSha256(family.source, family.sourceSha256, family.tool + " source");
            RawSpriteImage source = LoadRawSpriteImage(family.source, family.tool + " source");
            Require(
                source.Size.Width == family.sourceSize[0]
                    && source.Size.Height == family.sourceSize[1],
                family.tool + " source dimensions changed");

            Sprite[] importedVariants = new Sprite[family.variants.Length];
            for (int variantIndex = 0; variantIndex < family.variants.Length; variantIndex++)
            {
                AuthoredBuildingVariant variant = family.variants[variantIndex];
                string expectedId = $"V{variantIndex + 1:00}";
                Require(variant != null && variant.id == expectedId,
                    family.tool + " variants must be ordered and named " + expectedId);
                Require(outputPaths.Add(NormalizeAssetPath(variant.output)),
                    "Duplicate authored output path: " + variant.output);
                VerifySourceCrop(family, variant);
                importedVariants[variantIndex] = VerifyAuthoredBuildingVariant(
                    catalog,
                    tool,
                    family.tool,
                    variantIndex,
                    variant);
            }

            VerifySerializedBuildingFamily(catalog, tool, family.tool, importedVariants);
        }

        private static void VerifySourceCrop(
            AuthoredBuildingFamily family,
            AuthoredBuildingVariant variant)
        {
            string label = family.tool + " " + variant.id;
            RequireRect(variant.sourceBounds, label + " sourceBounds");
            Require(
                variant.sourceBounds[0] + variant.sourceBounds[2] <= family.sourceSize[0]
                    && variant.sourceBounds[1] + variant.sourceBounds[3] <= family.sourceSize[1],
                label + " source crop leaves the declared source image");
        }

        private static Sprite VerifyAuthoredBuildingVariant(
            StrategyVisualCatalog catalog,
            StrategyBuildTool tool,
            string toolName,
            int variantIndex,
            AuthoredBuildingVariant variant)
        {
            string label = toolName + " " + variant.id;
            RequirePair(variant.outputCanvas, label + " outputCanvas");
            RequireRect(variant.targetBoundsTopLeft, label + " targetBoundsTopLeft");
            RequirePair(variant.pivotNormalized, label + " pivotNormalized", true);
            Require(variant.ppu > 0f && float.IsFinite(variant.ppu), label + " PPU is invalid");
            Require(
                variant.targetBoundsTopLeft[0] + variant.targetBoundsTopLeft[2] <= variant.outputCanvas[0]
                    && variant.targetBoundsTopLeft[1] + variant.targetBoundsTopLeft[3] <= variant.outputCanvas[1],
                label + " target alpha bounds leave the output canvas");
            VerifyExactSha256(variant.output, variant.expectedSha256, label + " output");

            RawSpriteImage output = LoadRawSpriteImage(variant.output, label + " output");
            Require(
                output.Size.Width == variant.outputCanvas[0]
                    && output.Size.Height == variant.outputCanvas[1],
                label + " output dimensions changed");
            VerifyVisiblePixelQuality(output, label, true, false);
            RectInt alphaBounds = GetAlphaBoundsTopLeft(output, label);
            RectInt expectedBounds = new(
                variant.targetBoundsTopLeft[0],
                variant.targetBoundsTopLeft[1],
                variant.targetBoundsTopLeft[2],
                variant.targetBoundsTopLeft[3]);
            Require(alphaBounds == expectedBounds,
                label + " alpha bounds changed from the manifest contract");
            Require(alphaBounds.yMin > 0,
                label + " visible alpha is clipped by the top canvas edge");

            RawSpriteImage legacy = LoadRawSpriteImage(variant.legacyReference, label + " legacy reference");
            Require(!IsExactNearestNeighbor2x(output, legacy),
                label + " is only an exact nearest-neighbor 2x copy of its legacy sprite");

            Sprite imported = Resources.Load<Sprite>(GetResourcePath(variant.output));
            Require(imported != null, label + " authored Sprite resource is missing");
            VerifyFinalSpriteContract(imported, variant, label);
            VerifyLegacyWorldContract(imported, tool, variant, label);

            Require(
                catalog.TryGetBuildingSprite(tool, variantIndex, out Sprite catalogSprite)
                    && catalogSprite != null,
                label + " is missing from the visual catalog");
            VerifyOwnedFinalSprite(catalogSprite, imported, variant, label + " catalog sprite");

            Require(
                StrategyBuildingSpriteFactory.TryGetBuildSprite(tool, variantIndex, out Sprite runtimeSprite)
                    && runtimeSprite != null,
                label + " is missing from the runtime building factory");
            VerifyOwnedFinalSprite(runtimeSprite, imported, variant, label + " runtime sprite");
            return imported;
        }

        private static void VerifyFinalSpriteContract(
            Sprite sprite,
            AuthoredBuildingVariant variant,
            string label)
        {
            Require(sprite.texture != null, label + " imported texture is missing");
            Require(
                sprite.texture.width == variant.outputCanvas[0]
                    && sprite.texture.height == variant.outputCanvas[1]
                    && Approximately(sprite.rect.width, variant.outputCanvas[0])
                    && Approximately(sprite.rect.height, variant.outputCanvas[1]),
                label + " imported sprite dimensions changed");
            Require(Approximately(sprite.pixelsPerUnit, variant.ppu), label + " sprite PPU changed");
            Vector2 expectedPivot = new(
                variant.outputCanvas[0] * variant.pivotNormalized[0],
                variant.outputCanvas[1] * variant.pivotNormalized[1]);
            Require(Vector2.Distance(sprite.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " sprite pivot changed");
            VerifyAuthoredImporter(sprite.texture, variant.output, variant.ppu, label);
            TextureImporter importer = AssetImporter.GetAtPath(variant.output) as TextureImporter;
            Require(importer != null, label + " importer is missing");
            Require(Vector2.Distance(
                    importer.spritePivot,
                    new Vector2(variant.pivotNormalized[0], variant.pivotNormalized[1]))
                    <= AuthoredVisualTolerance,
                label + " importer pivot changed");
        }

        private static void VerifyLegacyWorldContract(
            Sprite authored,
            StrategyBuildTool tool,
            AuthoredBuildingVariant variant,
            string label)
        {
            Sprite legacy = AssetDatabase.LoadAssetAtPath<Sprite>(variant.legacyReference);
            Require(legacy != null && legacy.pixelsPerUnit > 0f,
                label + " legacy Sprite reference is missing");
            Require(
                Approximately(authored.rect.width / authored.pixelsPerUnit,
                    legacy.rect.width / legacy.pixelsPerUnit)
                    && Approximately(authored.rect.height / authored.pixelsPerUnit,
                        legacy.rect.height / legacy.pixelsPerUnit),
                label + " no longer preserves legacy world dimensions");
            bool preservesWorldPivot =
                Approximately(authored.pivot.x / authored.pixelsPerUnit,
                    legacy.pivot.x / legacy.pixelsPerUnit)
                && Approximately(authored.pivot.y / authored.pixelsPerUnit,
                    legacy.pivot.y / legacy.pixelsPerUnit);
            if (preservesWorldPivot)
            {
                return;
            }

            Vector2 legacyPivotNormalized = new(
                legacy.pivot.x / legacy.rect.width,
                legacy.pivot.y / legacy.rect.height);
            Require(IsDeclaredAuthoredBuildingPivotMigration(
                    tool,
                    variant,
                    legacyPivotNormalized),
                label + " has an undeclared legacy-to-authored pivot migration");
        }

        private static bool IsDeclaredAuthoredBuildingPivotMigration(
            StrategyBuildTool tool,
            AuthoredBuildingVariant variant,
            Vector2 legacyPivotNormalized)
        {
            float expectedLegacyPivotY = tool switch
            {
                StrategyBuildTool.ChickenCoop => 0.12f,
                StrategyBuildTool.StarterCaravanCart => 0.16f,
                _ => -1f
            };
            return expectedLegacyPivotY > 0f
                && variant != null
                && variant.pivotNormalized != null
                && variant.pivotNormalized.Length == 2
                && Approximately(variant.pivotNormalized[0], 0.5f)
                && Approximately(variant.pivotNormalized[1], 0.10f)
                && Approximately(legacyPivotNormalized.x, 0.5f)
                && Approximately(legacyPivotNormalized.y, expectedLegacyPivotY);
        }

        internal static void VerifyAuthoredBuildingPivotMigrationPolicy()
        {
            AuthoredBuildingVariant declared = new()
            {
                pivotNormalized = new[] { 0.5f, 0.10f }
            };
            Require(IsDeclaredAuthoredBuildingPivotMigration(
                    StrategyBuildTool.ChickenCoop,
                    declared,
                    new Vector2(0.5f, 0.12f)),
                "Chicken Coop authored pivot migration policy was not recognized");
            Require(IsDeclaredAuthoredBuildingPivotMigration(
                    StrategyBuildTool.StarterCaravanCart,
                    declared,
                    new Vector2(0.5f, 0.16f)),
                "Starter Caravan Cart authored pivot migration policy was not recognized");
            Require(!IsDeclaredAuthoredBuildingPivotMigration(
                    StrategyBuildTool.House,
                    declared,
                    new Vector2(0.5f, 0.16f)),
                "Pivot migration policy accepted an undeclared building");
            declared.pivotNormalized = new[] { 0.5f, 0.20f };
            Require(!IsDeclaredAuthoredBuildingPivotMigration(
                    StrategyBuildTool.ChickenCoop,
                    declared,
                    new Vector2(0.5f, 0.12f)),
                "Pivot migration policy accepted a non-manifest runtime pivot");
        }

        private static void VerifyOwnedFinalSprite(
            Sprite candidate,
            Sprite imported,
            AuthoredBuildingVariant variant,
            string label)
        {
            Require(candidate.texture == imported.texture,
                label + " does not own the exact authored texture");
            Require(NormalizeAssetPath(AssetDatabase.GetAssetPath(candidate.texture))
                    == NormalizeAssetPath(variant.output),
                label + " resolves to a fallback texture");
            Require(
                Approximately(candidate.rect.width, variant.outputCanvas[0])
                    && Approximately(candidate.rect.height, variant.outputCanvas[1])
                    && Approximately(candidate.pixelsPerUnit, variant.ppu),
                label + " changed authored dimensions or PPU");
            Vector2 expectedPivot = new(
                variant.outputCanvas[0] * variant.pivotNormalized[0],
                variant.outputCanvas[1] * variant.pivotNormalized[1]);
            Require(Vector2.Distance(candidate.pivot, expectedPivot) <= AuthoredVisualTolerance,
                label + " changed the authored pivot");
        }

        private static void VerifySerializedBuildingFamily(
            StrategyVisualCatalog catalog,
            StrategyBuildTool tool,
            string toolName,
            Sprite[] importedVariants)
        {
            SerializedProperty sets = new SerializedObject(catalog).FindProperty("buildingSprites");
            Require(sets != null && sets.isArray, "Visual catalog buildingSprites data is missing");
            int matches = 0;
            for (int i = 0; i < sets.arraySize; i++)
            {
                SerializedProperty set = sets.GetArrayElementAtIndex(i);
                if (set.FindPropertyRelative("tool").intValue != (int)tool)
                {
                    continue;
                }

                matches++;
                SerializedProperty variants = set.FindPropertyRelative("variants");
                Require(variants.arraySize == importedVariants.Length,
                    toolName + " catalog variant count differs from its manifest");
                for (int variant = 0; variant < variants.arraySize; variant++)
                {
                    Sprite catalogSprite = variants.GetArrayElementAtIndex(variant).objectReferenceValue as Sprite;
                    Require(catalogSprite != null && catalogSprite.texture == importedVariants[variant].texture,
                        $"{toolName} catalog V{variant + 1:00} does not reference the authored texture");
                }
            }

            Require(matches == 1, toolName + " must have exactly one serialized catalog family");
        }
    }
}
