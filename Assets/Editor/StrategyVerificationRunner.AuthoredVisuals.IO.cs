using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const string AuthoredBuildingManifestPath =
            "Tools/Art/HighResolutionBuildings.manifest.json";
        private const string AuthoredConstructionManifestPath =
            "Tools/Art/HighResolutionConstruction.manifest.json";
        private const string AuthoredBuildingAnimationManifestPath =
            "Tools/Art/HighResolutionBuildingAnimations.manifest.json";
        private const string AuthoredBridgeManifestPath =
            "Tools/Art/HighResolutionBridge.manifest.json";
        private const float AuthoredVisualTolerance = 0.001f;
        private const byte AuthoredVisibleAlphaThreshold = 16;

        private static T LoadAuthoredManifest<T>(string relativePath) where T : class
        {
            string fullPath = ResolveProjectPath(relativePath);
            Require(File.Exists(fullPath), "Authored visual manifest is missing: " + relativePath);
            T manifest = JsonUtility.FromJson<T>(File.ReadAllText(fullPath));
            Require(manifest != null, "Authored visual manifest is invalid JSON: " + relativePath);
            return manifest;
        }

        private static string ResolveProjectPath(string relativePath)
        {
            Require(!string.IsNullOrWhiteSpace(relativePath), "Authored visual path is empty");
            string root = Path.GetFullPath(Directory.GetCurrentDirectory())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            string rootPrefix = root + Path.DirectorySeparatorChar;
            Require(
                fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase),
                "Authored visual path leaves the project root: " + relativePath);
            return fullPath;
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }

        private static string GetResourcePath(string assetPath)
        {
            const string prefix = "Assets/Resources/";
            string normalized = NormalizeAssetPath(assetPath);
            Require(
                normalized.StartsWith(prefix, StringComparison.Ordinal)
                    && string.Equals(Path.GetExtension(normalized), ".png", StringComparison.OrdinalIgnoreCase),
                "Authored visual output must be a PNG below Assets/Resources: " + assetPath);
            return normalized.Substring(prefix.Length, normalized.Length - prefix.Length - 4);
        }

        private static void VerifyExactSha256(string relativePath, string expected, string label)
        {
            Require(
                !string.IsNullOrWhiteSpace(expected) && expected.Length == 64,
                label + " must declare a 64-character SHA-256");
            string actual = ComputeSha256(ResolveProjectPath(relativePath));
            Require(
                string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                label + " SHA-256 changed: " + relativePath);
        }

        private static string ComputeSha256(string fullPath)
        {
            Require(File.Exists(fullPath), "Authored visual file is missing: " + fullPath);
            using FileStream stream = File.OpenRead(fullPath);
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static RawSpriteImage LoadRawSpriteImage(string relativePath, string label)
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string fullPath = ResolveProjectPath(relativePath);
                Require(File.Exists(fullPath), label + " is missing: " + relativePath);
                Require(
                    ImageConversion.LoadImage(texture, File.ReadAllBytes(fullPath), false),
                    label + " is not a readable PNG: " + relativePath);
                return new RawSpriteImage(texture.width, texture.height, texture.GetPixels32());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static RectInt GetAlphaBoundsTopLeft(RawSpriteImage image, string label)
        {
            int minX = image.Size.Width;
            int minY = image.Size.Height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < image.Size.Height; y++)
            {
                for (int x = 0; x < image.Size.Width; x++)
                {
                    if (image.Pixels[y * image.Size.Width + x].a == 0)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            Require(maxX >= minX && maxY >= minY, label + " has no visible pixels");
            int top = image.Size.Height - 1 - maxY;
            return new RectInt(minX, top, maxX - minX + 1, maxY - minY + 1);
        }

        private static void VerifyVisiblePixelQuality(
            RawSpriteImage image,
            string label,
            bool requireHorizontalMargin,
            bool requireVerticalMargin)
        {
            bool hasTransparent = false;
            int magentaPixels = 0;
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                Color32 pixel = image.Pixels[i];
                hasTransparent |= pixel.a == 0;
                if (pixel.a >= AuthoredVisibleAlphaThreshold
                    && pixel.r >= 238
                    && pixel.g <= 48
                    && pixel.b >= 238)
                {
                    magentaPixels++;
                }
            }

            Require(hasTransparent, label + " must preserve transparent background pixels");
            Require(magentaPixels == 0, label + " contains visible magenta chroma-key pixels");
            RectInt bounds = GetAlphaBoundsTopLeft(image, label);
            if (requireHorizontalMargin)
            {
                Require(
                    bounds.xMin > 0 && bounds.xMax < image.Size.Width,
                    label + " visible alpha is clipped by a horizontal canvas edge");
            }

            if (requireVerticalMargin)
            {
                Require(
                    bounds.yMin > 0 && bounds.yMax < image.Size.Height,
                    label + " visible alpha is clipped by a vertical canvas edge");
            }
        }

        private static bool IsExactNearestNeighbor2x(RawSpriteImage output, RawSpriteImage source)
        {
            if (output.Size.Width != source.Size.Width * 2
                || output.Size.Height != source.Size.Height * 2)
            {
                return false;
            }

            for (int y = 0; y < output.Size.Height; y++)
            {
                for (int x = 0; x < output.Size.Width; x++)
                {
                    Color32 expected = source.Pixels[(y / 2) * source.Size.Width + x / 2];
                    Color32 actual = output.Pixels[y * output.Size.Width + x];
                    if (!AuthoredPixelsEquivalent(expected, actual))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AuthoredPixelsEquivalent(Color32 expected, Color32 actual)
        {
            return expected.a == 0 && actual.a == 0 || expected.Equals(actual);
        }

        private static bool AuthoredCompositedPixelsEquivalent(Color32 expected, Color32 actual)
        {
            if (expected.a != actual.a)
            {
                return false;
            }

            if (expected.a == 0)
            {
                return true;
            }

            if (expected.a == byte.MaxValue)
            {
                return expected.Equals(actual);
            }

            return Mathf.Abs(expected.r * expected.a - actual.r * actual.a) <= byte.MaxValue
                && Mathf.Abs(expected.g * expected.a - actual.g * actual.a) <= byte.MaxValue
                && Mathf.Abs(expected.b * expected.a - actual.b * actual.a) <= byte.MaxValue;
        }

        internal static void VerifyAuthoredVisualNearestNeighborGuard()
        {
            Color32[] sourcePixels =
            {
                new(12, 34, 56, 255),
                new(78, 90, 12, 255),
                new(23, 45, 67, 128),
                new(255, 0, 255, 0)
            };
            Color32[] outputPixels = new Color32[16];
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    outputPixels[y * 4 + x] = sourcePixels[(y / 2) * 2 + x / 2];
                }
            }

            RawSpriteImage source = new(2, 2, sourcePixels);
            RawSpriteImage exactCopy = new(4, 4, outputPixels);
            Require(IsExactNearestNeighbor2x(exactCopy, source),
                "Nearest-neighbor guard did not recognize an exact 2x copy");
            outputPixels[0] = new Color32(13, 34, 56, 255);
            RawSpriteImage authoredCopy = new(4, 4, outputPixels);
            Require(!IsExactNearestNeighbor2x(authoredCopy, source),
                "Nearest-neighbor guard rejected a materially changed authored pixel");
        }

        private static void VerifyAuthoredImporter(
            Texture2D texture,
            string expectedPath,
            float expectedPpu,
            string label)
        {
            Require(texture != null, label + " texture is missing");
            string assetPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(texture));
            Require(assetPath == NormalizeAssetPath(expectedPath), label + " resolved to " + assetPath);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, label + " texture importer is missing: " + assetPath);
            Require(
                importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single,
                label + " must stay Sprite/Single");
            Require(importer.filterMode == FilterMode.Point, label + " must use Point filtering");
            Require(
                Mathf.Abs(importer.spritePixelsPerUnit - expectedPpu) <= AuthoredVisualTolerance,
                label + " importer PPU changed");
            Require(!importer.mipmapEnabled, label + " mipmaps must stay disabled");
            Require(!importer.isReadable, label + " must stay read-disabled after import");
            Require(importer.alphaIsTransparency, label + " must import alpha as transparency");
            Require(importer.wrapMode == TextureWrapMode.Clamp, label + " must use Clamp wrapping");
            Require(importer.npotScale == TextureImporterNPOTScale.None, label + " must preserve NPOT dimensions");
            Require(
                importer.textureCompression == TextureImporterCompression.Uncompressed,
                label + " must stay uncompressed");
            Require(
                importer.maxTextureSize >= Mathf.Max(texture.width, texture.height),
                label + " importer max size would downscale the authored texture");
        }

        private static void RequirePair(int[] values, string label)
        {
            Require(values != null && values.Length == 2, label + " must contain exactly two integers");
            Require(values[0] > 0 && values[1] > 0, label + " dimensions must be positive");
        }

        private static void RequirePair(float[] values, string label, bool normalized)
        {
            Require(values != null && values.Length == 2, label + " must contain exactly two numbers");
            Require(float.IsFinite(values[0]) && float.IsFinite(values[1]), label + " contains non-finite values");
            if (normalized)
            {
                Require(
                    values[0] >= 0f && values[0] <= 1f && values[1] >= 0f && values[1] <= 1f,
                    label + " must be normalized");
            }
        }

        private static void RequireRect(int[] values, string label)
        {
            Require(values != null && values.Length == 4, label + " must contain four integers");
            Require(values[0] >= 0 && values[1] >= 0 && values[2] > 0 && values[3] > 0,
                label + " is invalid");
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) <= AuthoredVisualTolerance;
        }
    }
}
