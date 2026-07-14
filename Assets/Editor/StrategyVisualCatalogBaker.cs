using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private const string CatalogPath = "Assets/Resources/Visual/StrategyVisualCatalog.asset";
        private const string BakedRoot = "Assets/Resources/Visual/Baked";
        private const string AuthoredRoot = "Assets/Resources/Visual/Authored";
        private const string ResultFileName = "VisualCatalogBake.txt";

        [MenuItem("ProjectUnknown/Visuals/Rebuild Baseline Catalog")]
        public static void BakeAll()
        {
            StrategyVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<StrategyVisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Visual catalog asset is missing: " + CatalogPath);
            }

            catalog.ReplaceBakedContent(
                Array.Empty<StrategyVisualCatalog.BuildingSpriteSet>(),
                Array.Empty<StrategyVisualCatalog.ResidentSpriteSet>(),
                Array.Empty<StrategyVisualCatalog.ResidentAtlasSet>(),
                Array.Empty<StrategyVisualCatalog.NatureSpriteSet>(),
                Array.Empty<StrategyVisualCatalog.VisualSequenceSet>(),
                Array.Empty<StrategyVisualCatalog.TerrainSpriteSet>());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            RecreateOutputRoot();

            List<StrategyVisualCatalog.BuildingSpriteSet> buildings = BakeBuildings();
            List<StrategyVisualCatalog.NatureSpriteSet> nature = BakeNature();
            List<StrategyVisualCatalog.TerrainSpriteSet> terrain = BakeTerrain();
            List<StrategyVisualCatalog.ResidentSpriteSet> portraits = new();
            List<StrategyVisualCatalog.ResidentAtlasSet> residents = BakeResidents(portraits);
            List<StrategyVisualCatalog.VisualSequenceSet> sequences = new();
            BakeConstruction(sequences, buildings);
            BakeTrails(sequences);
            BakeBuildingLayers(sequences);

            catalog.ReplaceBakedContent(
                buildings.ToArray(),
                portraits.ToArray(),
                residents.ToArray(),
                nature.ToArray(),
                sequences.ToArray(),
                terrain.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string message = $"PASS buildings={buildings.Count} residents={residents.Count} "
                + $"portraits={portraits.Count} nature={nature.Count} terrain={terrain.Count} sequences={sequences.Count}";
            File.WriteAllText(GetResultPath(), message);
            Debug.Log("[VisualCatalogBaker] " + message);
        }

        public static void BakeAllAndExit()
        {
            try
            {
                BakeAll();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(GetResultPath()) ?? ".");
                File.WriteAllText(GetResultPath(), "FAIL " + exception);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static List<StrategyVisualCatalog.BuildingSpriteSet> BakeBuildings()
        {
            List<StrategyVisualCatalog.BuildingSpriteSet> result = new();
            foreach (StrategyBuildTool tool in Enum.GetValues(typeof(StrategyBuildTool)))
            {
                int count = Mathf.Max(1, StrategyVisualBakeSource.GetBuildingVariantCount(tool));
                List<Sprite> variants = new(count);
                for (int variant = 0; variant < count; variant++)
                {
                    Sprite source = StrategyVisualBakeSource.GetBuildingSprite(tool, variant);
                    if (source == null)
                    {
                        break;
                    }

                    string relativePath = $"Buildings/{tool}/V{variant + 1:00}.png";
                    string path = $"{BakedRoot}/{relativePath}";
                    Sprite baked = BakeSpriteAsset(source, path);
                    variants.Add(ResolveAuthoredSprite(relativePath, baked));
                }

                if (variants.Count > 0)
                {
                    result.Add(new StrategyVisualCatalog.BuildingSpriteSet(tool, variants.ToArray()));
                }
            }

            return result;
        }

        private static List<StrategyVisualCatalog.NatureSpriteSet> BakeNature()
        {
            List<StrategyVisualCatalog.NatureSpriteSet> result = new();
            foreach (StrategyNaturePropKind kind in Enum.GetValues(typeof(StrategyNaturePropKind)))
            {
                int count = StrategyVisualBakeSource.GetNatureVariantCount(kind);
                Sprite[] variants = new Sprite[count];
                for (int variant = 0; variant < count; variant++)
                {
                    Sprite source = StrategyVisualBakeSource.GetNatureSprite(kind, variant);
                    string path = $"{BakedRoot}/Nature/{kind}/V{variant + 1:00}.png";
                    variants[variant] = BakeSpriteAsset(source, path);
                }

                result.Add(new StrategyVisualCatalog.NatureSpriteSet(kind, variants));
            }

            return result;
        }

        private static void BakeConstruction(
            List<StrategyVisualCatalog.VisualSequenceSet> sequences,
            List<StrategyVisualCatalog.BuildingSpriteSet> buildings)
        {
            for (int i = 0; i < buildings.Count; i++)
            {
                StrategyVisualCatalog.BuildingSpriteSet building = buildings[i];
                for (int variant = 0; variant < building.Variants.Length; variant++)
                {
                    Sprite[] frames = new Sprite[StrategyVisualBakeSource.ConstructionStageCount];
                    for (int stage = 0; stage < frames.Length; stage++)
                    {
                        frames[stage] = StrategyVisualBakeSource.GetConstructionSprite(building.Tool, variant, stage);
                    }

                    string id = $"Construction/{building.Tool}/V{variant}";
                    string relativePath = $"Construction/{building.Tool}/V{variant + 1:00}.png";
                    string path = $"{BakedRoot}/{relativePath}";
                    sequences.Add(BakeSequenceAsset(id, frames, path, relativePath));
                }
            }
        }

        private static void BakeTrails(List<StrategyVisualCatalog.VisualSequenceSet> sequences)
        {
            for (int mask = 0; mask < 16; mask++)
            {
                for (int level = 1; level <= 3; level++)
                {
                    Sprite[] variants = new Sprite[4];
                    for (int variant = 0; variant < variants.Length; variant++)
                    {
                        variants[variant] = StrategyVisualBakeSource.GetTrailSprite(mask, level, variant);
                    }

                    string id = $"Trail/M{mask}/L{level}";
                    string path = $"{BakedRoot}/Trails/M{mask:00}_L{level}.png";
                    sequences.Add(BakeSequenceAsset(id, variants, path));
                }
            }
        }

        private static string GetResultPath()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, ResultFileName);
        }
    }
}
