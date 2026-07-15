using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    internal static partial class StrategyConstructionSpriteFactory
    {
        private static void FillEllipse(
            Texture2D texture,
            int centerX,
            int centerY,
            int radiusX,
            int radiusY,
            Color color)
        {
            int radiusXSqr = radiusX * radiusX;
            int radiusYSqr = radiusY * radiusY;
            int radiusProduct = radiusXSqr * radiusYSqr;

            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    if (x * x * radiusYSqr + y * y * radiusXSqr <= radiusProduct)
                    {
                        SetPixelSafe(texture, centerX + x, centerY + y, color);
                    }
                }
            }
        }

        private static void FillPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            if (points == null || points.Length < 3)
            {
                return;
            }

            int minY = points[0].y;
            int maxY = points[0].y;
            for (int i = 1; i < points.Length; i++)
            {
                minY = Mathf.Min(minY, points[i].y);
                maxY = Mathf.Max(maxY, points[i].y);
            }

            for (int y = minY; y <= maxY; y++)
            {
                List<int> nodes = new();
                int j = points.Length - 1;
                for (int i = 0; i < points.Length; i++)
                {
                    if ((points[i].y < y && points[j].y >= y)
                        || (points[j].y < y && points[i].y >= y))
                    {
                        int denominator = Mathf.Max(1, points[j].y - points[i].y);
                        int x = points[i].x
                            + (y - points[i].y) * (points[j].x - points[i].x) / denominator;
                        nodes.Add(x);
                    }

                    j = i;
                }

                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    FillRect(texture, nodes[i], y, nodes[i + 1] - nodes[i] + 1, 1, color);
                }
            }
        }

        private static void DrawPolygon(Texture2D texture, Vector2Int[] points, Color color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                DrawLine(texture, points[i], points[(i + 1) % points.Length], color);
            }
        }
    }
}
