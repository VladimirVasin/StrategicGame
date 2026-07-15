using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static Sprite CreateForagerCampSprite(int variant)
        {
            Texture2D texture = CreateTexture(96, 80, $"Forager Camp 2.5D Sprite {variant + 1}");

            Color outline = Rgb(45, 38, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color canvas = variant == 1 ? Rgb(130, 147, 90) : variant == 2 ? Rgb(118, 138, 102) : Rgb(145, 125, 78);
            Color canvasLight = variant == 2 ? Rgb(165, 181, 126) : Rgb(185, 166, 103);
            Color wood = Rgb(119, 78, 42);
            Color woodLight = Rgb(172, 111, 58);
            Color leaf = Rgb(73, 132, 61);
            Color berry = Rgb(168, 48, 89);

            FillEllipse(texture, 48, 18, 35, 10, shadow);
            FillRect(texture, 23, 24, 50, 9, outline);
            FillRect(texture, 25, 26, 46, 8, wood);
            FillRect(texture, 30, 28, 36, 2, woodLight);

            FillPolygon(texture, new[] { P(21, 34), P(48, 62), P(75, 34) }, outline);
            FillPolygon(texture, new[] { P(25, 35), P(48, 58), P(71, 35) }, canvas);
            DrawLine(texture, P(48, 58), P(48, 35), outline);
            DrawLine(texture, P(31, 38), P(48, 54), canvasLight);
            DrawLine(texture, P(65, 38), P(49, 54), Rgb(92, 102, 67));

            FillRect(texture, 28, 20, 5, 18, outline);
            FillRect(texture, 30, 20, 2, 18, woodLight);
            FillRect(texture, 63, 20, 5, 18, outline);
            FillRect(texture, 65, 20, 2, 18, woodLight);

            FillEllipse(texture, 16, 22, 9, 6, outline);
            FillEllipse(texture, 16, 23, 8, 5, leaf);
            FillEllipse(texture, 80, 23, 10, 7, outline);
            FillEllipse(texture, 80, 24, 9, 6, leaf);
            FillEllipse(texture, 14, 24, 2, 2, berry);
            FillEllipse(texture, 19, 24, 2, 2, berry);
            FillEllipse(texture, 78, 25, 2, 2, berry);
            FillEllipse(texture, 84, 24, 2, 2, berry);

            FillRect(texture, 38, 20, 18, 8, outline);
            FillRect(texture, 40, 22, 14, 6, Rgb(132, 83, 42));
            DrawLine(texture, P(41, 28), P(53, 28), Rgb(198, 132, 70));
            FillEllipse(texture, 44, 31, 3, 2, berry);
            FillEllipse(texture, 50, 31, 3, 2, Rgb(142, 96, 54));

            DrawBuildingPolish(texture, StrategyBuildTool.ForagerCamp, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 10f, 88f, 58f), new Vector2(0.5f, 0.2f), PixelsPerUnit);
        }

        private static Sprite CreateChickenCoopSprite(int variant)
        {
            int frame = NormalizeVariant(
                variant,
                StrategyChickenCoopVisualProfile.AnimationFrameCount);
            Sprite source = StrategyBuildingUpgradeSpriteFactory.GetProceduralAnimatedSprite(
                StrategyBuildingUpgradeType.ChickenCoop,
                frame);
            Sprite standalone = Sprite.Create(
                source.texture,
                source.rect,
                new Vector2(0.5f, StrategyChickenCoopVisualProfile.StandalonePivotY),
                StrategyChickenCoopVisualProfile.ProceduralStandalonePixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                source.border);
            standalone.name = source.name + " Standalone Procedural";
            return standalone;
        }

        private static Sprite CreateTradingPostSprite(int variant)
        {
            Texture2D texture = CreateTexture(104, 82, $"Trading Post 2.5D Sprite {variant + 1}");

            Color outline = Rgb(48, 37, 28);
            Color shadow = new Color(0f, 0f, 0f, 0.25f);
            Color wood = variant == 1 ? Rgb(128, 84, 48) : variant == 2 ? Rgb(118, 91, 58) : Rgb(140, 78, 46);
            Color woodLight = Rgb(190, 128, 72);
            Color cloth = variant == 2 ? Rgb(86, 125, 130) : Rgb(154, 94, 64);
            Color clothLight = Shift(cloth, 0.16f);
            Color goods = Rgb(190, 150, 82);

            FillEllipse(texture, 52, 17, 39, 10, shadow);
            FillRect(texture, 25, 22, 54, 16, outline);
            FillRect(texture, 27, 24, 50, 14, wood);
            DrawLine(texture, P(29, 31), P(75, 31), woodLight);

            FillRect(texture, 20, 36, 64, 8, outline);
            FillRect(texture, 22, 38, 60, 6, woodLight);
            FillPolygon(texture, new[] { P(18, 43), P(52, 64), P(86, 43) }, outline);
            FillPolygon(texture, new[] { P(23, 44), P(52, 60), P(81, 44) }, cloth);
            DrawLine(texture, P(32, 46), P(51, 57), clothLight);
            DrawLine(texture, P(72, 46), P(53, 57), Shift(cloth, -0.14f));

            FillRect(texture, 30, 16, 8, 21, outline);
            FillRect(texture, 32, 16, 4, 21, woodLight);
            FillRect(texture, 67, 16, 8, 21, outline);
            FillRect(texture, 69, 16, 4, 21, woodLight);

            FillRect(texture, 37, 20, 12, 8, outline);
            FillRect(texture, 39, 22, 9, 6, goods);
            FillRect(texture, 55, 20, 15, 9, outline);
            FillRect(texture, 57, 22, 11, 7, Rgb(92, 116, 82));
            FillEllipse(texture, 53, 29, 4, 3, Rgb(164, 65, 58));
            FillRect(texture, 45, 14, 16, 5, outline);
            FillRect(texture, 47, 15, 12, 4, Rgb(93, 70, 50));

            DrawBuildingPolish(texture, StrategyBuildTool.TradingPost, variant);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(4f, 9f, 96f, 60f), new Vector2(0.5f, 0.2f), PixelsPerUnit);
        }
    }
}
