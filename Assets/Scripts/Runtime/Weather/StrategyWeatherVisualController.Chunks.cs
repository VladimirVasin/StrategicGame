using System.Collections.Generic;
using UnityEngine;

namespace ProjectUnknown.Strategy
{
    public sealed partial class StrategyWeatherVisualController
    {
        private readonly List<int> weatherChunkQuery = new();
        private Color[] weatherChunkPixels;
        private bool cloudTextureNeedsFullPaint = true;
        private bool mistTextureNeedsFullPaint = true;

        private void PaintCloudFrame()
        {
            if (cloudTexture == null || cloudPixels == null || weather == null)
            {
                return;
            }

            float cloud = Mathf.Max(weather.CloudIntensity, weather.StormIntensity);
            if (cloud <= 0.02f)
            {
                ClearWeatherTextureOnce(cloudRenderer, cloudTexture, cloudPixels);
                cloudTextureNeedsFullPaint = true;
                return;
            }

            if (cloudRenderer != null)
            {
                cloudRenderer.enabled = true;
            }

            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            float time = Time.unscaledTime;
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (cloudTextureNeedsFullPaint || chunks == null || !chunks.IsConfigured)
            {
                PaintCloudFrameFull(cloud, windDirection, time);
                cloudTextureNeedsFullPaint = false;
                return;
            }

            int chunkCount = chunks.CopyActiveChunkIndices(weatherChunkQuery, StrategyWorldChunkDirtyFlags.Weather);
            if (chunkCount <= 0)
            {
                PaintCloudFrameFull(cloud, windDirection, time);
                return;
            }

            for (int i = 0; i < weatherChunkQuery.Count; i++)
            {
                if (chunks.TryGetChunkCellBounds(weatherChunkQuery[i], out Vector2Int origin, out Vector2Int size))
                {
                    PaintCloudChunk(origin, size, cloud, windDirection, time);
                }
            }

            cloudTexture.Apply(false, false);
        }

        private void PaintMistFrame()
        {
            if (mistTexture == null || mistPixels == null || weather == null)
            {
                return;
            }

            float mist = weather.HeavyRainIntensity * 0.20f;
            if (mist <= 0.02f)
            {
                ClearWeatherTextureOnce(mistRenderer, mistTexture, mistPixels);
                mistTextureNeedsFullPaint = true;
                return;
            }

            if (mistRenderer != null)
            {
                mistRenderer.enabled = true;
            }

            Vector2 windDirection = wind != null ? wind.PlanarDirection : Vector2.right;
            float time = Time.unscaledTime;
            StrategyWorldChunkRegistry chunks = StrategyWorldChunkRegistry.Active;
            if (mistTextureNeedsFullPaint || chunks == null || !chunks.IsConfigured)
            {
                PaintMistFrameFull(mist, windDirection, time);
                mistTextureNeedsFullPaint = false;
                return;
            }

            int chunkCount = chunks.CopyActiveChunkIndices(weatherChunkQuery, StrategyWorldChunkDirtyFlags.Weather);
            if (chunkCount <= 0)
            {
                PaintMistFrameFull(mist, windDirection, time);
                return;
            }

            for (int i = 0; i < weatherChunkQuery.Count; i++)
            {
                if (chunks.TryGetChunkCellBounds(weatherChunkQuery[i], out Vector2Int origin, out Vector2Int size))
                {
                    PaintMistChunk(origin, size, mist, windDirection, time);
                }
            }

            mistTexture.Apply(false, false);
        }

        private void PaintCloudFrameFull(float cloud, Vector2 windDirection, float time)
        {
            ClearPixels(cloudPixels);
            int width = cloudTexture.width;
            int height = cloudTexture.height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cloudPixels[y * width + x] = EvaluateCloudPixel(x, y, cloud, windDirection, time);
                }
            }

            ApplyTexture(cloudTexture, cloudPixels);
        }

        private void PaintMistFrameFull(float mist, Vector2 windDirection, float time)
        {
            ClearPixels(mistPixels);
            int width = mistTexture.width;
            int height = mistTexture.height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    mistPixels[y * width + x] = EvaluateMistPixel(x, y, mist, windDirection, time);
                }
            }

            ApplyTexture(mistTexture, mistPixels);
        }

        private void PaintCloudChunk(
            Vector2Int origin,
            Vector2Int size,
            float cloud,
            Vector2 windDirection,
            float time)
        {
            int startX = origin.x * CloudPixelsPerCell;
            int startY = origin.y * CloudPixelsPerCell;
            int widthPixels = size.x * CloudPixelsPerCell;
            int heightPixels = size.y * CloudPixelsPerCell;
            if (widthPixels <= 0 || heightPixels <= 0)
            {
                return;
            }

            Color[] pixels = EnsureWeatherChunkPixels(widthPixels * heightPixels);
            int cursor = 0;
            for (int y = 0; y < heightPixels; y++)
            {
                int textureY = startY + y;
                for (int x = 0; x < widthPixels; x++)
                {
                    pixels[cursor++] = EvaluateCloudPixel(startX + x, textureY, cloud, windDirection, time);
                }
            }

            cloudTexture.SetPixels(startX, startY, widthPixels, heightPixels, pixels);
        }

        private void PaintMistChunk(
            Vector2Int origin,
            Vector2Int size,
            float mist,
            Vector2 windDirection,
            float time)
        {
            int startX = origin.x * MistPixelsPerCell;
            int startY = origin.y * MistPixelsPerCell;
            int widthPixels = size.x * MistPixelsPerCell;
            int heightPixels = size.y * MistPixelsPerCell;
            if (widthPixels <= 0 || heightPixels <= 0)
            {
                return;
            }

            Color[] pixels = EnsureWeatherChunkPixels(widthPixels * heightPixels);
            int cursor = 0;
            for (int y = 0; y < heightPixels; y++)
            {
                int textureY = startY + y;
                for (int x = 0; x < widthPixels; x++)
                {
                    pixels[cursor++] = EvaluateMistPixel(startX + x, textureY, mist, windDirection, time);
                }
            }

            mistTexture.SetPixels(startX, startY, widthPixels, heightPixels, pixels);
        }

        private static Color EvaluateCloudPixel(
            int x,
            int y,
            float cloud,
            Vector2 windDirection,
            float time)
        {
            float nx = x * 0.032f + time * 0.012f * windDirection.x;
            float ny = y * 0.032f + time * 0.012f * windDirection.y;
            float noise = Mathf.PerlinNoise(nx, ny) * 0.74f
                + Mathf.PerlinNoise(nx * 0.43f + 17.5f, ny * 0.43f + 9.75f) * 0.26f;
            if (noise < 0.44f)
            {
                return Color.clear;
            }

            float alpha = Mathf.Clamp01((noise - 0.44f) / 0.56f) * cloud * 0.15f;
            return new Color(0.015f, 0.025f, 0.025f, alpha);
        }

        private static Color EvaluateMistPixel(
            int x,
            int y,
            float mist,
            Vector2 windDirection,
            float time)
        {
            float band = Mathf.Sin((y + time * 2.4f) * 0.055f + x * 0.015f) * 0.5f + 0.5f;
            float noise = Mathf.PerlinNoise(
                x * 0.055f + time * 0.018f * windDirection.x,
                y * 0.055f + time * 0.012f * windDirection.y);
            float alpha = (noise * 0.65f + band * 0.35f) * mist * 0.18f;
            return alpha <= 0.01f ? Color.clear : new Color(0.68f, 0.76f, 0.74f, alpha);
        }

        private static void ClearWeatherTextureOnce(
            SpriteRenderer renderer,
            Texture2D texture,
            Color[] pixels)
        {
            if (renderer != null && !renderer.enabled)
            {
                return;
            }

            ClearPixels(pixels);
            ApplyTexture(texture, pixels);
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private Color[] EnsureWeatherChunkPixels(int requiredLength)
        {
            if (weatherChunkPixels == null || weatherChunkPixels.Length != requiredLength)
            {
                weatherChunkPixels = new Color[requiredLength];
            }

            return weatherChunkPixels;
        }
    }
}
