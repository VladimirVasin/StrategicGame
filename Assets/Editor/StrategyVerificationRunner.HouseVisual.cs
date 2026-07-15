using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const int AuthoredHouseSize = 160;
        private const int HouseConstructionFrameWidth = 184;
        private const int HouseConstructionFrameHeight = 164;
        private const int HouseConstructionFrameCount = 7;
        private const float AuthoredHousePixelsPerUnit = 48f;

        private static void VerifyAuthoredHouseFamily()
        {
            Vector2 expectedPivot = new(80f, 16f);
            for (int variant = 0; variant < StrategyBuildingSpriteFactory.HouseVariantCount; variant++)
            {
                string resourcePath = $"Visual/Authored/Buildings/House/V{variant + 1:00}";
                Sprite house = Resources.Load<Sprite>(resourcePath);
                Require(house != null, "Authored House sprite is missing: " + resourcePath);
                Require(
                    Mathf.RoundToInt(house.rect.width) == AuthoredHouseSize
                        && Mathf.RoundToInt(house.rect.height) == AuthoredHouseSize,
                    "Authored House sprite dimensions changed: " + resourcePath);
                Require(
                    Mathf.Approximately(house.pixelsPerUnit, AuthoredHousePixelsPerUnit)
                        && Vector2.Distance(house.pivot, expectedPivot) < 0.01f,
                    "Authored House sprite scale or pivot changed: " + resourcePath);
                Require(
                    house.texture != null && house.texture.filterMode == FilterMode.Point,
                    "Authored House sprite must use Point filtering: " + resourcePath);

                string assetPath = AssetDatabase.GetAssetPath(house.texture);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null, "Authored House texture importer is missing: " + assetPath);
                Require(
                    Mathf.Approximately(importer.spritePixelsPerUnit, AuthoredHousePixelsPerUnit),
                    "Authored House importer must stay at 48 PPU: " + assetPath);
                Require(!importer.mipmapEnabled, "Authored House mipmaps must stay disabled: " + assetPath);
                Require(!importer.isReadable, "Authored House texture must stay read-disabled: " + assetPath);
                Require(
                    importer.textureCompression == TextureImporterCompression.Uncompressed,
                    "Authored House texture must stay uncompressed: " + assetPath);
            }
        }

        private static void VerifyAuthoredHouseConstructionFamily(StrategyVisualCatalog catalog)
        {
            Vector2 expectedPivot = new(92f, 16f);
            for (int variant = 0; variant < StrategyBuildingSpriteFactory.HouseVariantCount; variant++)
            {
                string resourcePath = $"Visual/Authored/Construction/House/V{variant + 1:00}";
                Texture2D atlas = Resources.Load<Texture2D>(resourcePath);
                Require(atlas != null, "Authored House construction atlas is missing: " + resourcePath);
                Require(
                    atlas.width == HouseConstructionFrameWidth * HouseConstructionFrameCount
                        && atlas.height == HouseConstructionFrameHeight,
                    "Authored House construction atlas dimensions changed: " + resourcePath);
                Require(
                    atlas.filterMode == FilterMode.Point,
                    "Authored House construction atlas must use Point filtering: " + resourcePath);

                string assetPath = AssetDatabase.GetAssetPath(atlas);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Require(importer != null, "Authored House construction importer is missing: " + assetPath);
                Require(
                    importer.textureType == TextureImporterType.Sprite
                        && importer.spriteImportMode == SpriteImportMode.Single,
                    "Authored House construction atlas must stay Sprite/Single: " + assetPath);
                Require(
                    Mathf.Approximately(importer.spritePixelsPerUnit, AuthoredHousePixelsPerUnit),
                    "Authored House construction importer must stay at 48 PPU: " + assetPath);
                Require(!importer.mipmapEnabled, "Authored House construction mipmaps must stay disabled: " + assetPath);
                Require(!importer.isReadable, "Authored House construction atlas must stay read-disabled: " + assetPath);
                Require(
                    importer.textureCompression == TextureImporterCompression.Uncompressed,
                    "Authored House construction atlas must stay uncompressed: " + assetPath);
                Require(
                    importer.npotScale == TextureImporterNPOTScale.None,
                    "Authored House construction atlas must preserve NPOT size: " + assetPath);

                string sequenceId = $"Construction/{StrategyBuildTool.House}/V{variant}";
                for (int frame = 0; frame < HouseConstructionFrameCount; frame++)
                {
                    Require(
                        catalog.TryGetSequenceSprite(sequenceId, frame, out Sprite sprite) && sprite != null,
                        $"Authored House construction frame is missing: {sequenceId}/{frame}");
                    Require(
                        sprite.texture == atlas,
                        $"House construction frame does not reference authored texture: {sequenceId}/{frame}");
                    Require(
                        Mathf.RoundToInt(sprite.rect.x) == frame * HouseConstructionFrameWidth
                            && Mathf.RoundToInt(sprite.rect.y) == 0
                            && Mathf.RoundToInt(sprite.rect.width) == HouseConstructionFrameWidth
                            && Mathf.RoundToInt(sprite.rect.height) == HouseConstructionFrameHeight,
                        $"Authored House construction frame rect changed: {sequenceId}/{frame}");
                    Require(
                        Mathf.Approximately(sprite.pixelsPerUnit, AuthoredHousePixelsPerUnit)
                            && Vector2.Distance(sprite.pivot, expectedPivot) < 0.01f,
                        $"Authored House construction frame scale or pivot changed: {sequenceId}/{frame}");
                }

                VerifyEmbeddedFinalHousePixels(atlas, variant);
            }
        }

        private static void VerifyEmbeddedFinalHousePixels(Texture2D atlas, int variant)
        {
            const int stageSixLeft = 12;
            const int stageSixTop = 4;
            string housePath = $"Assets/Resources/Visual/Authored/Buildings/House/V{variant + 1:00}.png";
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
            Texture2D atlasCopy = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(source, File.ReadAllBytes(housePath), false), "Could not read " + housePath);
                string atlasPath = AssetDatabase.GetAssetPath(atlas);
                Require(ImageConversion.LoadImage(atlasCopy, File.ReadAllBytes(atlasPath), false), "Could not read " + atlasPath);
                Require(
                    source.width == AuthoredHouseSize && source.height == AuthoredHouseSize,
                    "Completed House dimensions changed: " + housePath);

                Color32[] sourcePixels = source.GetPixels32();
                Color32[] atlasPixels = atlasCopy.GetPixels32();
                int atlasX = 6 * HouseConstructionFrameWidth + stageSixLeft;
                int atlasY = HouseConstructionFrameHeight - source.height - stageSixTop;
                Require(atlasY >= 0, "Completed House exceeds the construction frame height: " + housePath);
                for (int y = 0; y < source.height; y++)
                {
                    for (int x = 0; x < source.width; x++)
                    {
                        Color32 expected = sourcePixels[y * source.width + x];
                        if (expected.a == 0)
                        {
                            continue;
                        }

                        Color32 actual = atlasPixels[(atlasY + y) * atlasCopy.width + atlasX + x];
                        Require(
                            VisiblePixelsEquivalent(expected, actual),
                            $"Construction stage 6 shifted or changed: House V{variant + 1:00} at {x},{y}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(atlasCopy);
            }
        }

        private static bool VisiblePixelsEquivalent(Color32 expected, Color32 actual)
        {
            if (expected.a != actual.a)
            {
                return false;
            }

            if (expected.a == byte.MaxValue)
            {
                return expected.Equals(actual);
            }

            return Mathf.Abs(expected.r * expected.a - actual.r * actual.a) <= byte.MaxValue
                && Mathf.Abs(expected.g * expected.a - actual.g * actual.a) <= byte.MaxValue
                && Mathf.Abs(expected.b * expected.a - actual.b * actual.a) <= byte.MaxValue;
        }
    }
}
