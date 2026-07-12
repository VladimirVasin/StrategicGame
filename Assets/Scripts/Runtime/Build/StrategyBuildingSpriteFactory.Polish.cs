using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyBuildingSpriteFactory
    {
        private static void DrawBuildingPolish(Texture2D texture, StrategyBuildTool tool, int variant)
        {
            Color outline = Rgb(45, 34, 25);
            switch (tool)
            {
                case StrategyBuildTool.LumberjackCamp:
                    DrawLine(texture, P(18, 20), P(28, 23), Rgb(204, 143, 72));
                    DrawLine(texture, P(67, 17), P(76, 20), Rgb(92, 55, 32));
                    break;
                case StrategyBuildTool.StonecutterCamp:
                    FillRect(texture, 18 + variant * 2, 13, 3, 2, Rgb(178, 174, 153));
                    FillRect(texture, 69 - variant, 16, 2, 2, Rgb(78, 78, 72));
                    break;
                case StrategyBuildTool.HunterCamp:
                    DrawLine(texture, P(23, 18), P(28, 25), Rgb(188, 137, 70));
                    FillRect(texture, 69, 20, 3, 2, Rgb(113, 55, 38));
                    break;
                case StrategyBuildTool.FisherHut:
                    DrawLine(texture, P(21, 17), P(73, 17), Rgb(179, 124, 65));
                    DrawLine(texture, P(34, 14), P(34, 20), outline);
                    DrawLine(texture, P(59, 14), P(59, 20), outline);
                    break;
                case StrategyBuildTool.ForagerCamp:
                    FillRect(texture, 42, 23, 2, 2, Rgb(201, 73, 91));
                    FillRect(texture, 49, 24, 2, 2, Rgb(112, 151, 68));
                    FillRect(texture, 55, 22, 2, 2, Rgb(213, 171, 79));
                    break;
                case StrategyBuildTool.Mine:
                    DrawLine(texture, P(31, 22), P(62, 22), Rgb(104, 86, 63));
                    FillRect(texture, 72, 14, 3, 2, Rgb(151, 112, 65));
                    break;
                case StrategyBuildTool.CoalPit:
                    DrawLine(texture, P(31, 20), P(62, 20), Rgb(70, 66, 60));
                    FillRect(texture, 42 + variant * 3, 14, 4, 2, Rgb(38, 39, 41));
                    FillRect(texture, 67, 16, 2, 2, Rgb(103, 101, 91));
                    break;
                case StrategyBuildTool.ClayPit:
                    DrawLine(texture, P(25, 17), P(70, 17), Rgb(210, 123, 76));
                    FillRect(texture, 42, 12, 7, 2, Rgb(111, 62, 48));
                    FillRect(texture, 53 + variant, 19, 3, 2, Rgb(238, 158, 102));
                    break;
                case StrategyBuildTool.Sawmill:
                    DrawLine(texture, P(22, 18), P(79, 18), Rgb(193, 132, 68));
                    FillRect(texture, 68, 26, 5, 2, Rgb(224, 174, 91));
                    break;
                case StrategyBuildTool.Kiln:
                    DrawLine(texture, P(38, 26), P(63, 26), Rgb(226, 135, 72));
                    FillRect(texture, 48 + variant, 17, 4, 2, Rgb(68, 47, 38));
                    break;
                case StrategyBuildTool.Forge:
                    FillRect(texture, 46, 22, 8, 2, Rgb(238, 145, 66));
                    FillRect(texture, 49, 24, 3, 2, Rgb(255, 203, 94));
                    DrawLine(texture, P(67, 18), P(76, 20), Rgb(132, 137, 128));
                    break;
                case StrategyBuildTool.StorageYard:
                    DrawLine(texture, P(18, 17), P(84, 17), Rgb(178, 124, 66));
                    FillRect(texture, 25 + variant * 3, 23, 4, 2, Rgb(205, 151, 76));
                    break;
                case StrategyBuildTool.Granary:
                    DrawLine(texture, P(29, 21), P(71, 21), Rgb(205, 153, 73));
                    FillRect(texture, 73, 17, 3, 2, Rgb(222, 181, 88));
                    break;
                case StrategyBuildTool.TradingPost:
                    FillRect(texture, 41, 23, 3, 2, Rgb(217, 177, 91));
                    FillRect(texture, 50, 23, 3, 2, Rgb(164, 67, 58));
                    FillRect(texture, 59, 23, 3, 2, Rgb(91, 132, 92));
                    break;
            }
        }
    }
}
