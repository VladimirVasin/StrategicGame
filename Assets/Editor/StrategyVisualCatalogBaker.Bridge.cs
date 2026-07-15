using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTools
{
    public static partial class StrategyVisualCatalogBaker
    {
        private static void PrepareBridgeModulesForBake()
        {
            foreach (bool horizontal in new[] { true, false })
            {
                foreach (StrategyBridgeVisualProfile.Module module in
                    Enum.GetValues(typeof(StrategyBridgeVisualProfile.Module)))
                {
                    Vector2Int expectedSize =
                        StrategyBridgeVisualProfile.GetModulePixelSize(horizontal, module);
                    ImportAndReadBridgeModule(
                        GetBridgeModuleAssetPath(horizontal, module, false, 0),
                        expectedSize);
                    for (int stage = 0; stage < StrategyConstructionSpriteFactory.StageCount; stage++)
                    {
                        ImportAndReadBridgeModule(
                            GetBridgeModuleAssetPath(horizontal, module, true, stage),
                            expectedSize);
                    }
                }
            }
        }

        private static void BakeBridgeModules(
            List<StrategyVisualCatalog.VisualSequenceSet> sequences)
        {
            foreach (bool horizontal in new[] { true, false })
            {
                foreach (StrategyBridgeVisualProfile.Module module in
                    Enum.GetValues(typeof(StrategyBridgeVisualProfile.Module)))
                {
                    sequences.Add(BakeBridgeModuleSequence(horizontal, module, false));
                    sequences.Add(BakeBridgeModuleSequence(horizontal, module, true));
                }
            }
        }

        private static StrategyVisualCatalog.VisualSequenceSet BakeBridgeModuleSequence(
            bool horizontal,
            StrategyBridgeVisualProfile.Module module,
            bool construction)
        {
            int frameCount = construction ? StrategyConstructionSpriteFactory.StageCount : 1;
            Vector2Int frameSize = StrategyBridgeVisualProfile.GetModulePixelSize(horizontal, module);
            Color32[] atlasPixels = new Color32[frameSize.x * frameCount * frameSize.y];
            for (int frame = 0; frame < frameCount; frame++)
            {
                string assetPath = GetBridgeModuleAssetPath(
                    horizontal,
                    module,
                    construction,
                    frame);
                Color32[] pixels = ImportAndReadBridgeModule(assetPath, frameSize);
                for (int y = 0; y < frameSize.y; y++)
                {
                    Array.Copy(
                        pixels,
                        y * frameSize.x,
                        atlasPixels,
                        y * frameSize.x * frameCount + frame * frameSize.x,
                        frameSize.x);
                }
            }

            string orientation = horizontal ? "Horizontal" : "Vertical";
            string kind = construction ? "Construction" : "Final";
            string atlasPath = $"{BakedRoot}/Bridge/{kind}/{orientation}/{module}.png";
            WriteTexture(atlasPath, atlasPixels, frameSize.x * frameCount, frameSize.y);
            ConfigureSpriteImporter(
                atlasPath,
                StrategyBridgeVisualProfile.AuthoredPixelsPerUnit,
                new Vector2(0.5f, 0.5f),
                readable: true);
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (atlas == null)
            {
                throw new InvalidOperationException("Bridge module atlas import failed: " + atlasPath);
            }

            return new StrategyVisualCatalog.VisualSequenceSet(
                StrategyBridgeVisualProfile.GetCatalogSequenceId(
                    horizontal,
                    module,
                    construction),
                atlas,
                frameSize.x,
                frameSize.y,
                frameCount,
                StrategyBridgeVisualProfile.AuthoredPixelsPerUnit,
                new Vector2(0.5f, 0.5f));
        }

        private static string GetBridgeModuleAssetPath(
            bool horizontal,
            StrategyBridgeVisualProfile.Module module,
            bool construction,
            int frame)
        {
            string orientation = horizontal ? "Horizontal" : "Vertical";
            return construction
                ? $"{AuthoredRoot}/Construction/Bridge/{orientation}/S{frame + 1:00}/{module}.png"
                : $"{AuthoredRoot}/Buildings/Bridge/{orientation}/{module}.png";
        }

        private static Color32[] ImportAndReadBridgeModule(
            string assetPath,
            Vector2Int expectedSize)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                throw new InvalidOperationException("Authored Bridge module is missing: " + assetPath);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureSpriteImporter(
                assetPath,
                StrategyBridgeVisualProfile.AuthoredPixelsPerUnit,
                new Vector2(0.5f, 0.5f),
                readable: true);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null
                || Mathf.RoundToInt(sprite.rect.width) != expectedSize.x
                || Mathf.RoundToInt(sprite.rect.height) != expectedSize.y
                || Mathf.Abs(sprite.pixelsPerUnit - StrategyBridgeVisualProfile.AuthoredPixelsPerUnit) > 0.01f)
            {
                throw new InvalidOperationException(
                    $"Authored Bridge module contract changed: {assetPath} must be "
                    + $"{expectedSize.x}x{expectedSize.y} @ "
                    + $"{StrategyBridgeVisualProfile.AuthoredPixelsPerUnit} PPU");
            }

            Texture2D readable = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(readable, File.ReadAllBytes(absolutePath), false)
                    || readable.width != expectedSize.x
                    || readable.height != expectedSize.y)
                {
                    throw new InvalidOperationException(
                        $"Authored Bridge module PNG dimensions changed: {assetPath}");
                }

                return readable.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static void DestroyBridgeBakeSource(Sprite sprite)
        {
            if (sprite == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sprite)))
            {
                return;
            }

            Texture2D texture = sprite.texture;
            UnityEngine.Object.DestroyImmediate(sprite);
            if (texture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
