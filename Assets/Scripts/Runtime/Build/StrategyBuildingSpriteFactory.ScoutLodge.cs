using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static Sprite CreateScoutLodgeSprite(int variant)
        {
            Texture2D texture = CreateTexture(160, 116, $"Scout Lodge 2.5D Sprite {variant + 1}");

            Color outline = Rgb(43, 34, 27);
            Color shadow = new Color(0f, 0f, 0f, 0.26f);
            Color earth = Rgb(94, 73, 49);
            Color stone = Rgb(113, 113, 101);
            Color stoneLight = Rgb(166, 164, 145);
            Color wall = Rgb(164, 116, 68);
            Color wallLight = Rgb(202, 151, 91);
            Color wallSide = Rgb(120, 80, 49);
            Color timber = Rgb(78, 50, 34);
            Color timberLight = Rgb(137, 86, 45);
            Color roof = Rgb(73, 88, 66);
            Color roofLight = Rgb(112, 129, 83);
            Color roofSide = Rgb(50, 65, 52);
            Color cloth = Rgb(169, 91, 58);
            Color parchment = Rgb(218, 191, 129);
            Color ink = Rgb(77, 79, 62);
            Color glass = Rgb(102, 154, 163);

            FillEllipse(texture, 80, 17, 69, 12, shadow);
            Vector2Int[] ground = { P(9, 24), P(62, 8), P(151, 28), P(91, 51) };
            FillPolygon(texture, ground, earth);
            DrawPolygon(texture, ground, outline);

            DrawScoutLodgeFoundation(texture, outline, stone, stoneLight);
            DrawScoutLodgeRearTower(texture, outline, timber, timberLight);

            Vector2Int[] sideWall = { P(28, 31), P(51, 47), P(51, 74), P(28, 57) };
            Vector2Int[] frontWall = { P(51, 47), P(132, 38), P(132, 64), P(51, 74) };
            FillPolygon(texture, sideWall, wallSide);
            DrawPolygon(texture, sideWall, outline);
            FillPolygon(texture, frontWall, wall);
            DrawPolygon(texture, frontWall, outline);

            DrawScoutLodgeWallTimbers(texture, outline, timber, timberLight);
            DrawScoutLodgeWindows(texture, outline, glass);
            DrawScoutLodgeDoor(texture, outline, timber, parchment, ink);
            DrawScoutLodgeRoof(texture, outline, roof, roofLight, roofSide);
            DrawScoutLodgeLookout(texture, outline, timber, timberLight, roof, roofLight, glass);
            DrawScoutLodgeFlag(texture, outline, timberLight, cloth);
            DrawScoutLodgeMapTable(texture, outline, timber, timberLight, parchment, ink);

            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(4f, 5f, 152f, 106f),
                new Vector2(0.5f, 0.10f),
                PixelsPerUnit);
        }

        private static void DrawScoutLodgeFoundation(
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

        private static void DrawScoutLodgeRearTower(
            Texture2D texture,
            Color outline,
            Color timber,
            Color light)
        {
            DrawThickLine(texture, P(108, 45), P(108, 91), outline, 2);
            DrawThickLine(texture, P(108, 46), P(108, 90), timber, 1);
            DrawLine(texture, P(109, 48), P(109, 89), light);
            DrawThickLine(texture, P(132, 47), P(132, 92), outline, 2);
            DrawThickLine(texture, P(132, 48), P(132, 91), timber, 1);
            DrawLine(texture, P(133, 49), P(133, 90), light);
            DrawLine(texture, P(108, 60), P(132, 84), outline);
            DrawLine(texture, P(132, 60), P(108, 84), light);
        }

        private static void DrawScoutLodgeWallTimbers(
            Texture2D texture,
            Color outline,
            Color timber,
            Color light)
        {
            DrawThickLine(texture, P(51, 48), P(132, 39), outline, 2);
            DrawThickLine(texture, P(52, 49), P(131, 40), light, 1);
            DrawThickLine(texture, P(51, 72), P(132, 62), outline, 2);
            DrawThickLine(texture, P(52, 71), P(131, 61), timber, 1);
            DrawThickLine(texture, P(68, 46), P(68, 71), outline, 2);
            DrawThickLine(texture, P(69, 47), P(69, 70), timber, 1);
            DrawThickLine(texture, P(96, 43), P(96, 67), outline, 2);
            DrawThickLine(texture, P(97, 44), P(97, 66), timber, 1);
            DrawThickLine(texture, P(119, 40), P(119, 65), outline, 2);
            DrawThickLine(texture, P(120, 41), P(120, 64), timber, 1);
            DrawLine(texture, P(30, 35), P(49, 49), light);
            DrawLine(texture, P(30, 55), P(49, 70), timber);
        }

        private static void DrawScoutLodgeWindows(Texture2D texture, Color outline, Color glass)
        {
            DrawScoutWindow(texture, 76, 53, outline, glass);
            DrawScoutWindow(texture, 103, 50, outline, glass);
            DrawScoutWindow(texture, 124, 48, outline, glass);
        }

        private static void DrawScoutWindow(Texture2D texture, int x, int y, Color outline, Color glass)
        {
            FillRect(texture, x, y, 10, 9, outline);
            FillRect(texture, x + 2, y + 2, 6, 5, glass);
            DrawLine(texture, P(x + 5, y + 2), P(x + 5, y + 7), Rgb(205, 211, 180));
            DrawLine(texture, P(x + 2, y + 4), P(x + 8, y + 4), Rgb(59, 92, 96));
        }

        private static void DrawScoutLodgeDoor(
            Texture2D texture,
            Color outline,
            Color timber,
            Color parchment,
            Color ink)
        {
            Vector2Int[] door = { P(34, 40), P(47, 49), P(47, 68), P(34, 58) };
            FillPolygon(texture, door, outline);
            Vector2Int[] inner = { P(36, 42), P(45, 49), P(45, 64), P(36, 57) };
            FillPolygon(texture, inner, timber);
            FillRect(texture, 39, 54, 3, 3, parchment);
            SetPixelSafe(texture, 40, 55, ink);
        }

        private static void DrawScoutLodgeRoof(
            Texture2D texture,
            Color outline,
            Color roof,
            Color light,
            Color side)
        {
            Vector2Int[] rearPlane = { P(27, 57), P(108, 49), P(120, 72), P(40, 84) };
            Vector2Int[] frontPlane = { P(40, 84), P(120, 72), P(139, 62), P(51, 75) };
            FillPolygon(texture, rearPlane, side);
            DrawPolygon(texture, rearPlane, outline);
            FillPolygon(texture, frontPlane, roof);
            DrawPolygon(texture, frontPlane, outline);
            DrawThickLine(texture, P(40, 84), P(120, 72), outline, 1);
            DrawLine(texture, P(43, 82), P(118, 71), light);
            DrawLine(texture, P(56, 77), P(129, 66), Rgb(49, 62, 48));
            DrawLine(texture, P(49, 70), P(121, 59), Rgb(93, 111, 72));
        }

        private static void DrawScoutLodgeLookout(
            Texture2D texture,
            Color outline,
            Color timber,
            Color light,
            Color roof,
            Color roofLight,
            Color glass)
        {
            FillRect(texture, 104, 82, 34, 7, outline);
            FillRect(texture, 106, 84, 30, 4, light);
            DrawThickLine(texture, P(109, 83), P(109, 98), outline, 2);
            DrawThickLine(texture, P(109, 84), P(109, 97), timber, 1);
            DrawThickLine(texture, P(133, 84), P(133, 98), outline, 2);
            DrawThickLine(texture, P(133, 85), P(133, 97), timber, 1);
            Vector2Int[] canopy = { P(102, 98), P(119, 109), P(142, 98), P(133, 93), P(119, 101), P(109, 94) };
            FillPolygon(texture, canopy, roof);
            DrawPolygon(texture, canopy, outline);
            DrawLine(texture, P(107, 98), P(119, 105), roofLight);

            DrawThickLine(texture, P(115, 89), P(128, 94), outline, 1);
            DrawLine(texture, P(115, 90), P(128, 95), glass);
            FillEllipse(texture, 130, 95, 3, 3, outline);
            FillEllipse(texture, 130, 95, 2, 2, glass);
            DrawLine(texture, P(120, 91), P(116, 85), timber);
        }

        private static void DrawScoutLodgeFlag(
            Texture2D texture,
            Color outline,
            Color pole,
            Color cloth)
        {
            DrawThickLine(texture, P(103, 88), P(103, 110), outline, 1);
            DrawLine(texture, P(103, 88), P(103, 110), pole);
            Vector2Int[] flag = { P(104, 108), P(119, 104), P(104, 99) };
            FillPolygon(texture, flag, cloth);
            DrawPolygon(texture, flag, outline);
        }

        private static void DrawScoutLodgeMapTable(
            Texture2D texture,
            Color outline,
            Color timber,
            Color light,
            Color parchment,
            Color ink)
        {
            DrawThickLine(texture, P(31, 17), P(28, 30), outline, 1);
            DrawLine(texture, P(31, 18), P(29, 29), timber);
            DrawThickLine(texture, P(54, 18), P(57, 31), outline, 1);
            DrawLine(texture, P(54, 19), P(56, 30), light);
            Vector2Int[] table = { P(23, 29), P(47, 22), P(63, 29), P(38, 37) };
            FillPolygon(texture, table, outline);
            Vector2Int[] map = { P(27, 29), P(47, 24), P(58, 29), P(38, 34) };
            FillPolygon(texture, map, parchment);
            DrawLine(texture, P(34, 29), P(44, 31), ink);
            DrawLine(texture, P(44, 27), P(48, 31), ink);
            FillEllipse(texture, 51, 28, 2, 2, Rgb(151, 63, 48));
        }
    }
}
