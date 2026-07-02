using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyFogOfWarController
    {
        private readonly List<int> fogChunkQuery = new();
        private Color[] fogChunkPixels;
        private bool fogTextureNeedsFullPaint = true;

        private void PaintFogTexture()
        {
            if (fogTexture == null || fogPixels == null)
            {
                return;
            }

            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (fogTextureNeedsFullPaint || chunks == null || !chunks.IsConfigured)
            {
                PaintFullFogTexture();
                fogTextureNeedsFullPaint = false;
                return;
            }

            int chunkCount = chunks.CopyActiveChunkIndices(
                fogChunkQuery,
                StrategyWorldChunkDirtyFlags.Fog,
                true);
            if (chunkCount <= 0)
            {
                PaintFullFogTexture();
                return;
            }

            for (int i = 0; i < fogChunkQuery.Count; i++)
            {
                if (chunks.TryGetChunkCellBounds(fogChunkQuery[i], out Vector2Int origin, out Vector2Int size))
                {
                    PaintFogTextureChunk(origin, size);
                }
            }

            fogTexture.Apply(false, false);
        }

        private void PaintFullFogTexture()
        {
            int textureWidth = fogTexture.width;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Color color = GetFogCellColor(x, y);
                    int pixelX = x * FogPixelsPerCell;
                    int pixelY = y * FogPixelsPerCell;
                    for (int py = 0; py < FogPixelsPerCell; py++)
                    {
                        int row = (pixelY + py) * textureWidth + pixelX;
                        for (int px = 0; px < FogPixelsPerCell; px++)
                        {
                            fogPixels[row + px] = color;
                        }
                    }
                }
            }

            fogTexture.SetPixels(fogPixels);
            fogTexture.Apply(false, false);
        }

        private void PaintFogTextureChunk(Vector2Int origin, Vector2Int size)
        {
            int widthPixels = size.x * FogPixelsPerCell;
            int heightPixels = size.y * FogPixelsPerCell;
            if (widthPixels <= 0 || heightPixels <= 0)
            {
                return;
            }

            Color[] pixels = EnsureFogChunkPixels(widthPixels * heightPixels);
            int cursor = 0;
            int endX = origin.x + size.x;
            int endY = origin.y + size.y;
            for (int y = origin.y; y < endY; y++)
            {
                for (int py = 0; py < FogPixelsPerCell; py++)
                {
                    for (int x = origin.x; x < endX; x++)
                    {
                        Color color = GetFogCellColor(x, y);
                        for (int px = 0; px < FogPixelsPerCell; px++)
                        {
                            pixels[cursor++] = color;
                        }
                    }
                }
            }

            fogTexture.SetPixels(
                origin.x * FogPixelsPerCell,
                origin.y * FogPixelsPerCell,
                widthPixels,
                heightPixels,
                pixels);
        }

        private Color[] EnsureFogChunkPixels(int requiredLength)
        {
            if (fogChunkPixels == null || fogChunkPixels.Length != requiredLength)
            {
                fogChunkPixels = new Color[requiredLength];
            }

            return fogChunkPixels;
        }
    }
}
