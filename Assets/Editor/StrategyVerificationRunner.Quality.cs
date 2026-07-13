using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectUnknown.Strategy.EditorTests
{
    public static partial class StrategyVerificationRunner
    {
        private const int MaximumCSharpFileLines = 500;

        internal static void VerifySourceQuality()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string assetsRoot = Path.Combine(projectRoot, "Assets");
            string[] sourceFiles = Directory.GetFiles(
                assetsRoot,
                "*.cs",
                SearchOption.AllDirectories);
            List<string> violations = new();
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                string sourcePath = sourceFiles[i];
                int lineCount = CountLines(sourcePath);
                if (lineCount > MaximumCSharpFileLines)
                {
                    violations.Add(
                        GetProjectRelativePath(projectRoot, sourcePath)
                        + " has " + lineCount + " lines");
                }

                if (!File.Exists(sourcePath + ".meta"))
                {
                    violations.Add(
                        GetProjectRelativePath(projectRoot, sourcePath)
                        + " is missing its .meta file");
                }

                string relativePath = GetProjectRelativePath(projectRoot, sourcePath);
                if (relativePath.StartsWith("Assets/Scripts/Runtime/", StringComparison.Ordinal)
                    && !relativePath.StartsWith("Assets/Scripts/Runtime/Input/", StringComparison.Ordinal))
                {
                    string source = File.ReadAllText(sourcePath);
                    if (source.Contains("Keyboard.current", StringComparison.Ordinal)
                        || source.Contains("Mouse.current", StringComparison.Ordinal)
                        || source.Contains("KeyControl", StringComparison.Ordinal))
                    {
                        violations.Add(relativePath + " bypasses StrategyInputRouter");
                    }
                }
            }

            Require(
                violations.Count == 0,
                "Source quality gate failed:\n" + string.Join("\n", violations));
        }

        private static int CountLines(string path)
        {
            int count = 0;
            using StreamReader reader = new(path);
            while (reader.ReadLine() != null)
            {
                count++;
            }

            return count;
        }

        private static string GetProjectRelativePath(string projectRoot, string path)
        {
            string relative = path.Substring(projectRoot.Length).TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }
}
