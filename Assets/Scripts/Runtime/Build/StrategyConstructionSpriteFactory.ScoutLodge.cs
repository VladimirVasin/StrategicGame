using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyConstructionSpriteFactory
    {
        private static Sprite CreateScoutLodgeConstructionSprite(int stage)
        {
            int level = Mathf.Clamp(stage, 0, StageCount - 1);
            if (level >= StageCount - 1
                && StrategyBuildingSpriteFactory.TryGetBuildSprite(
                    StrategyBuildTool.ScoutLodge,
                    0,
                    out Sprite completed)
                && completed != null)
            {
                return completed;
            }

            Texture2D texture = CreateTexture(
                160,
                116,
                $"Scout Lodge Construction Stage {level + 1}");
            Color outline = Rgb(43, 34, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.24f);
            Color earth = Rgb(99, 76, 51);
            Color stone = Rgb(112, 113, 102);
            Color stoneLight = Rgb(167, 165, 147);
            Color timberDark = Rgb(78, 50, 34);
            Color timber = Rgb(126, 77, 41);
            Color timberLight = Rgb(181, 119, 62);
            Color wall = Rgb(174, 126, 76);
            Color wallSide = Rgb(124, 83, 51);
            Color roof = Rgb(72, 88, 66);
            Color roofLight = Rgb(111, 130, 83);
            Color rope = Rgb(195, 154, 86);

            FillEllipse(texture, 80, 17, 69, 12, shadow);
            Vector2Int[] ground = { P(9, 24), P(62, 8), P(151, 28), P(91, 51) };
            FillPolygon(texture, ground, earth);
            DrawPolygon(texture, ground, outline);
            DrawScoutSurveyStakes(texture, outline, timberLight, rope);

            if (level >= 1)
            {
                DrawScoutConstructionFoundation(texture, outline, stone, stoneLight);
            }

            if (level >= 2)
            {
                DrawScoutConstructionFrame(texture, outline, timberDark, timber, timberLight, false);
            }

            if (level >= 3)
            {
                DrawScoutConstructionFrame(texture, outline, timberDark, timber, timberLight, true);
                DrawScoutConstructionScaffold(texture, outline, timber, timberLight, rope);
            }

            if (level >= 4)
            {
                DrawScoutConstructionWalls(texture, outline, wall, wallSide);
                DrawScoutConstructionFrame(texture, outline, timberDark, timber, timberLight, true);
            }

            if (level >= 5)
            {
                DrawScoutConstructionRoof(texture, outline, roof, roofLight);
                DrawScoutConstructionLookout(texture, outline, timberDark, timber, timberLight, roof);
            }

            DrawScoutConstructionMaterials(
                texture,
                level,
                outline,
                timber,
                timberLight,
                stone,
                stoneLight);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(4f, 5f, 152f, 106f),
                new Vector2(0.5f, 0.10f),
                PixelsPerUnit);
        }

        private static void DrawScoutSurveyStakes(
            Texture2D texture,
            Color outline,
            Color wood,
            Color rope)
        {
            Vector2Int[] stakes = { P(20, 27), P(61, 12), P(143, 30), P(87, 48) };
            for (int i = 0; i < stakes.Length; i++)
            {
                Vector2Int point = stakes[i];
                DrawThickLine(texture, point, P(point.x, point.y + 8), outline, 1);
                DrawLine(texture, point, P(point.x, point.y + 7), wood);
                Vector2Int next = stakes[(i + 1) % stakes.Length];
                DrawLine(texture, P(point.x, point.y + 5), P(next.x, next.y + 5), rope);
            }
        }

        private static void DrawScoutConstructionFoundation(
            Texture2D texture,
            Color outline,
            Color stone,
            Color light)
        {
            Vector2Int[] foundation = { P(22, 29), P(84, 15), P(140, 32), P(76, 52) };
            FillPolygon(texture, foundation, outline);
            Vector2Int[] inner = { P(26, 30), P(84, 18), P(135, 33), P(76, 49) };
            FillPolygon(texture, inner, stone);
            DrawLine(texture, P(31, 31), P(83, 20), light);
            DrawLine(texture, P(82, 49), P(132, 34), Rgb(76, 78, 72));
        }

        private static void DrawScoutConstructionFrame(
            Texture2D texture,
            Color outline,
            Color dark,
            Color timber,
            Color light,
            bool upperBeams)
        {
            Vector2Int[] feet = { P(28, 31), P(51, 47), P(77, 44), P(104, 41), P(132, 38) };
            for (int i = 0; i < feet.Length; i++)
            {
                Vector2Int foot = feet[i];
                int height = i == 0 ? 27 : 26;
                DrawThickLine(texture, foot, P(foot.x, foot.y + height), outline, 2);
                DrawThickLine(texture, P(foot.x + 1, foot.y), P(foot.x + 1, foot.y + height - 1), dark, 1);
                DrawLine(texture, P(foot.x + 2, foot.y + 1), P(foot.x + 2, foot.y + height - 2), light);
            }

            DrawBeam(texture, 50, 48, 132, 39, dark, light, outline);
            if (!upperBeams)
            {
                return;
            }

            DrawBeam(texture, 50, 73, 132, 63, dark, light, outline);
            DrawBeam(texture, 28, 57, 50, 73, dark, timber, outline);
            DrawLine(texture, P(52, 49), P(75, 70), timber);
            DrawLine(texture, P(75, 45), P(101, 67), light);
            DrawLine(texture, P(103, 42), P(130, 63), timber);
        }

        private static void DrawScoutConstructionScaffold(
            Texture2D texture,
            Color outline,
            Color timber,
            Color light,
            Color rope)
        {
            DrawThickLine(texture, P(18, 29), P(18, 70), outline, 1);
            DrawLine(texture, P(18, 30), P(18, 69), timber);
            DrawThickLine(texture, P(146, 31), P(146, 68), outline, 1);
            DrawLine(texture, P(146, 32), P(146, 67), light);
            DrawBeam(texture, 15, 55, 148, 55, timber, light, outline);
            DrawLine(texture, P(19, 34), P(47, 65), rope);
            DrawLine(texture, P(143, 34), P(119, 66), rope);
        }

        private static void DrawScoutConstructionWalls(
            Texture2D texture,
            Color outline,
            Color wall,
            Color side)
        {
            Vector2Int[] sideWall = { P(28, 31), P(51, 47), P(51, 68), P(28, 53) };
            Vector2Int[] frontWall = { P(51, 47), P(132, 38), P(132, 59), P(51, 69) };
            FillPolygon(texture, sideWall, side);
            DrawPolygon(texture, sideWall, outline);
            FillPolygon(texture, frontWall, wall);
            DrawPolygon(texture, frontWall, outline);

            for (int x = 61; x <= 116; x += 27)
            {
                FillRect(texture, x, 51 - (x - 61) / 9, 11, 8, outline);
                FillRect(texture, x + 2, 53 - (x - 61) / 9, 7, 4, Rgb(68, 103, 109));
            }
        }

        private static void DrawScoutConstructionRoof(
            Texture2D texture,
            Color outline,
            Color roof,
            Color light)
        {
            Vector2Int[] rearPlane = { P(27, 57), P(108, 49), P(120, 72), P(40, 84) };
            Vector2Int[] frontPlane = { P(40, 84), P(120, 72), P(139, 62), P(51, 75) };
            FillPolygon(texture, rearPlane, Rgb(50, 65, 52));
            DrawPolygon(texture, rearPlane, outline);
            FillPolygon(texture, frontPlane, roof);
            DrawPolygon(texture, frontPlane, outline);
            DrawThickLine(texture, P(40, 84), P(120, 72), outline, 1);
            DrawLine(texture, P(43, 82), P(118, 71), light);
        }

        private static void DrawScoutConstructionLookout(
            Texture2D texture,
            Color outline,
            Color dark,
            Color timber,
            Color light,
            Color roof)
        {
            DrawPosts(texture, 108, 62, 29, dark, timber, outline);
            DrawPosts(texture, 132, 63, 29, dark, light, outline);
            DrawBeam(texture, 104, 83, 138, 87, dark, light, outline);
            DrawLine(texture, P(108, 68), P(132, 89), timber);
            DrawLine(texture, P(132, 68), P(108, 89), light);
            Vector2Int[] canopy = { P(102, 98), P(119, 109), P(142, 98), P(133, 93), P(119, 101), P(109, 94) };
            FillPolygon(texture, canopy, roof);
            DrawPolygon(texture, canopy, outline);
        }

        private static void DrawScoutConstructionMaterials(
            Texture2D texture,
            int stage,
            Color outline,
            Color timber,
            Color timberLight,
            Color stone,
            Color stoneLight)
        {
            int logCount = Mathf.Clamp(7 - stage, 1, 7);
            for (int i = 0; i < logCount; i++)
            {
                int x = 17 + i * 10;
                int y = 13 + i % 2 * 3;
                DrawThickLine(texture, P(x, y), P(x + 13, y + 2), outline, 2);
                DrawLine(texture, P(x + 1, y + 1), P(x + 12, y + 2), timber);
                SetPixelSafe(texture, x + 12, y + 2, timberLight);
            }

            int stoneCount = Mathf.Clamp(5 - stage, 0, 5);
            for (int i = 0; i < stoneCount; i++)
            {
                int x = 124 + i % 3 * 7;
                int y = 14 + i / 3 * 5;
                FillEllipse(texture, x, y, 4, 3, outline);
                FillEllipse(texture, x, y + 1, 3, 2, stone);
                SetPixelSafe(texture, x - 1, y + 2, stoneLight);
            }
        }
    }
}
