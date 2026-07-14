using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private static void VerifyAuthoredForagerCamp(StrategyVisualCatalog catalog)
        {
            const int spriteWidth = 176;
            const int spriteHeight = 116;
            const int frameWidth = 184;
            const int frameHeight = 164;
            const int frameCount = 7;
            const float pixelsPerUnit = 48f;
            const string spriteResource = "Visual/Authored/Buildings/ForagerCamp/V01";
            const string atlasResource = "Visual/Authored/Construction/ForagerCamp/V01";
            Require(
                StrategyBuildingSpriteFactory.ForagerCampVariantCount == 1,
                "Forager Camp must keep one authored visual variant");

            Sprite camp = Resources.Load<Sprite>(spriteResource);
            Require(camp != null, "Authored Forager Camp sprite is missing: " + spriteResource);
            Require(
                Mathf.RoundToInt(camp.rect.width) == spriteWidth
                    && Mathf.RoundToInt(camp.rect.height) == spriteHeight,
                "Authored Forager Camp sprite dimensions changed");
            Require(
                Mathf.Approximately(camp.pixelsPerUnit, pixelsPerUnit)
                    && Vector2.Distance(camp.pivot, new Vector2(88f, 23.2f)) < 0.01f,
                "Authored Forager Camp scale or pivot changed");
            VerifyForagerImporter(camp.texture, "Forager Camp", pixelsPerUnit);

            Require(
                catalog.TryGetBuildingSprite(StrategyBuildTool.ForagerCamp, 0, out Sprite catalogCamp)
                    && catalogCamp != null
                    && catalogCamp.texture == camp.texture,
                "Visual catalog does not reference the authored Forager Camp sprite");

            Texture2D atlas = Resources.Load<Texture2D>(atlasResource);
            Require(atlas != null, "Authored Forager Camp construction atlas is missing: " + atlasResource);
            Require(
                atlas.width == frameWidth * frameCount && atlas.height == frameHeight,
                "Authored Forager Camp construction atlas dimensions changed");
            VerifyForagerImporter(
                atlas,
                "Forager Camp construction atlas",
                pixelsPerUnit,
                requireSpriteSingle: true);

            string sequenceId = $"Construction/{StrategyBuildTool.ForagerCamp}/V0";
            Vector2 expectedPivot = new(92f, 23.2f);
            for (int frame = 0; frame < frameCount; frame++)
            {
                Require(
                    catalog.TryGetSequenceSprite(sequenceId, frame, out Sprite sprite) && sprite != null,
                    $"Authored Forager Camp construction frame is missing: {sequenceId}/{frame}");
                Require(sprite.texture == atlas, $"Forager Camp frame does not reference authored atlas: {frame}");
                Require(
                    Mathf.RoundToInt(sprite.rect.x) == frame * frameWidth
                        && Mathf.RoundToInt(sprite.rect.y) == 0
                        && Mathf.RoundToInt(sprite.rect.width) == frameWidth
                        && Mathf.RoundToInt(sprite.rect.height) == frameHeight,
                    $"Forager Camp construction frame rect changed: {frame}");
                Require(
                    Mathf.Approximately(sprite.pixelsPerUnit, pixelsPerUnit)
                        && Vector2.Distance(sprite.pivot, expectedPivot) < 0.01f,
                    $"Forager Camp construction frame scale or pivot changed: {frame}");
            }

            VerifyEmbeddedFinalForagerCampPixels(atlas);
        }

        private static void VerifyForagerImporter(
            Texture2D texture,
            string label,
            float expectedPixelsPerUnit,
            bool requireSpriteSingle = false)
        {
            Require(texture != null && texture.filterMode == FilterMode.Point, label + " must use Point filtering");
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, label + " importer is missing: " + assetPath);
            Require(
                Mathf.Approximately(importer.spritePixelsPerUnit, expectedPixelsPerUnit),
                label + " importer must stay at 48 PPU");
            if (requireSpriteSingle)
            {
                Require(
                    importer.textureType == TextureImporterType.Sprite
                        && importer.spriteImportMode == SpriteImportMode.Single,
                    label + " must stay Sprite/Single");
                Require(importer.npotScale == TextureImporterNPOTScale.None, label + " must preserve NPOT size");
            }

            Require(!importer.mipmapEnabled, label + " mipmaps must stay disabled");
            Require(!importer.isReadable, label + " must stay read-disabled");
            Require(
                importer.textureCompression == TextureImporterCompression.Uncompressed,
                label + " must stay uncompressed");
        }

        private static void VerifyEmbeddedFinalForagerCampPixels(Texture2D atlas)
        {
            const int spriteWidth = 176;
            const int spriteHeight = 116;
            const int frameWidth = 184;
            const int frameHeight = 164;
            const int stageSixLeft = 4;
            const int stageSixTop = 48;
            const string spritePath = "Assets/Resources/Visual/Authored/Buildings/ForagerCamp/V01.png";
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false);
            Texture2D atlasCopy = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(source, File.ReadAllBytes(spritePath), false), "Could not read " + spritePath);
                string atlasPath = AssetDatabase.GetAssetPath(atlas);
                Require(ImageConversion.LoadImage(atlasCopy, File.ReadAllBytes(atlasPath), false), "Could not read " + atlasPath);
                Require(
                    source.width == spriteWidth && source.height == spriteHeight,
                    "Completed Forager Camp dimensions changed: " + spritePath);
                int atlasX = 6 * frameWidth + stageSixLeft;
                int atlasY = frameHeight - source.height - stageSixTop;
                Require(atlasY >= 0, "Completed Forager Camp exceeds its construction frame");
                Color32[] sourcePixels = source.GetPixels32();
                Color32[] atlasPixels = atlasCopy.GetPixels32();
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
                            $"Construction stage 6 shifted or changed at {x},{y}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(atlasCopy);
            }
        }
    }
}
