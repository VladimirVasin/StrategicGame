using System;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        [Serializable]
        private sealed class AuthoredBuildingManifest
        {
            public int schemaVersion = 0;
            public AuthoredBuildingFamily[] families = Array.Empty<AuthoredBuildingFamily>();
        }

        [Serializable]
        private sealed class AuthoredBuildingFamily
        {
            public string tool = string.Empty;
            public string source = string.Empty;
            public int[] sourceSize = Array.Empty<int>();
            public string sourceSha256 = string.Empty;
            public AuthoredBuildingVariant[] variants = Array.Empty<AuthoredBuildingVariant>();
        }

        [Serializable]
        private sealed class AuthoredBuildingVariant
        {
            public string id = string.Empty;
            public int[] sourceBounds = Array.Empty<int>();
            public int[] outputCanvas = Array.Empty<int>();
            public int[] targetBoundsTopLeft = Array.Empty<int>();
            public float ppu = 0f;
            public float[] pivotNormalized = Array.Empty<float>();
            public string legacyReference = string.Empty;
            public string output = string.Empty;
            public string expectedSha256 = string.Empty;
        }

        [Serializable]
        private sealed class AuthoredConstructionManifest
        {
            public int schemaVersion = 0;
            public AuthoredConstructionFamily[] families = Array.Empty<AuthoredConstructionFamily>();
        }

        [Serializable]
        private sealed class AuthoredConstructionFamily
        {
            public string tool = string.Empty;
            public int frameCount = 0;
            public AuthoredFrameContract sourceFrame = new();
            public AuthoredFrameContract outputFrame = new();
            public AuthoredConstructionVariant[] variants = Array.Empty<AuthoredConstructionVariant>();
        }

        [Serializable]
        private sealed class AuthoredFrameContract
        {
            public int[] size = Array.Empty<int>();
            public float ppu = 0f;
            public float[] pivotPixelsBottomLeft = Array.Empty<float>();
        }

        [Serializable]
        private sealed class AuthoredConstructionVariant
        {
            public string id = string.Empty;
            public string source = string.Empty;
            public string sourceSha256 = string.Empty;
            public string finalSprite = string.Empty;
            public string finalSha256 = string.Empty;
            public float finalPpu = 0f;
            public float[] finalPivotNormalized = Array.Empty<float>();
            public string output = string.Empty;
            public string expectedSha256 = string.Empty;
        }

        [Serializable]
        private sealed class AuthoredBuildingAnimationManifest
        {
            public int schemaVersion = 0;
            public AuthoredBuildingAnimationSequence[] sequences =
                Array.Empty<AuthoredBuildingAnimationSequence>();
        }

        [Serializable]
        private sealed class AuthoredBuildingAnimationSequence
        {
            public string id = string.Empty;
            public string tool = string.Empty;
            public int[] frameSize = Array.Empty<int>();
            public int frameCount = 0;
            public float ppu = 0f;
            public float[] pivotNormalized = Array.Empty<float>();
            public AuthoredBuildingAnimationFrame[] frames =
                Array.Empty<AuthoredBuildingAnimationFrame>();
            public string output = string.Empty;
            public string expectedSha256 = string.Empty;
        }

        [Serializable]
        private sealed class AuthoredBuildingAnimationFrame
        {
            public string source = string.Empty;
            public string sha256 = string.Empty;
        }

        [Serializable]
        private sealed class AuthoredBridgeManifest
        {
            public int schemaVersion = 0;
            public string source = string.Empty;
            public string sourceSha256 = string.Empty;
            public int[] sourceSize = Array.Empty<int>();
            public float pixelsPerUnit = 0f;
            public float[] pivotNormalized = Array.Empty<float>();
            public int constructionStageCount = 0;
            public AuthoredBridgeModule[] modules = Array.Empty<AuthoredBridgeModule>();
        }

        [Serializable]
        private sealed class AuthoredBridgeModule
        {
            public string orientation = string.Empty;
            public string module = string.Empty;
            public int[] sourceBounds = Array.Empty<int>();
            public int[] outputCanvas = Array.Empty<int>();
            public int[] targetBoundsTopLeft = Array.Empty<int>();
            public string seamEdge = string.Empty;
            public string output = string.Empty;
            public string expectedSha256 = string.Empty;
            public AuthoredBridgeConstructionFrame[] construction =
                Array.Empty<AuthoredBridgeConstructionFrame>();
        }

        [Serializable]
        private sealed class AuthoredBridgeConstructionFrame
        {
            public int stage = 0;
            public string output = string.Empty;
            public string expectedSha256 = string.Empty;
        }

        private readonly struct RawSpriteImage
        {
            public readonly TextureDimensions Size;
            public readonly UnityEngine.Color32[] Pixels;

            public RawSpriteImage(int width, int height, UnityEngine.Color32[] pixels)
            {
                Size = new TextureDimensions(width, height);
                Pixels = pixels;
            }
        }

        private readonly struct TextureDimensions
        {
            public readonly int Width;
            public readonly int Height;

            public TextureDimensions(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }
    }
}
