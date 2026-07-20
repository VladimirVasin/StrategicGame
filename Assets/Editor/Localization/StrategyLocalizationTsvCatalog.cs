using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectUnknown.Strategy.EditorTools
{
    internal static class StrategyLocalizationTsvCatalog
    {
        private static readonly Regex PlaceholderPattern = new Regex(
            @"(?<!\{)\{([A-Za-z0-9_]+)(?:[^{}]*)\}(?!\})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        internal static IReadOnlyList<Row> Read(string tableName, string path)
        {
            if (!File.Exists(path))
            {
                return Array.Empty<Row>();
            }

            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
            {
                throw Error(tableName, 1, "catalog is empty");
            }

            string[] header = Split(lines[0].TrimStart('\uFEFF'));
            if (header.Length < 3
                || header[0] != "Key"
                || header[1] != "English"
                || header[2] != "Russian"
                || (header.Length > 3 && header[3] != "Smart")
                || header.Length > 4)
            {
                throw Error(tableName, 1,
                    "header must be Key<TAB>English<TAB>Russian[<TAB>Smart]");
            }

            var rows = new List<Row>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var legacySources = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 1; index < lines.Length; index++)
            {
                string line = lines[index];
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                {
                    continue;
                }

                string[] columns = Split(line);
                if (columns.Length < 3 || columns.Length > 4)
                {
                    throw Error(tableName, index + 1, "expected 3 or 4 tab-separated columns");
                }

                string english = Decode(columns[1]);
                string russian = Decode(columns[2]);
                string key = tableName == StrategyLocalizationTables.Legacy
                    ? StrategyLocalization.GetLegacyKey(english)
                    : columns[0].Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw Error(tableName, index + 1, "key is empty");
                }

                if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(russian))
                {
                    throw Error(tableName, index + 1, "both translations are required");
                }

                if (!keys.Add(key))
                {
                    throw Error(tableName, index + 1, "duplicate key: " + key);
                }

                if (tableName == StrategyLocalizationTables.Legacy)
                {
                    if (legacySources.TryGetValue(key, out string existing)
                        && !string.Equals(existing, english, StringComparison.Ordinal))
                    {
                        throw Error(tableName, index + 1, "legacy hash collision: " + key);
                    }

                    legacySources[key] = english;
                }

                ValidatePlaceholderSchema(tableName, index + 1, english, russian);
                bool smart = columns.Length == 4
                    ? ParseSmart(tableName, index + 1, columns[3])
                    : ContainsPlaceholder(english) || ContainsPlaceholder(russian);
                rows.Add(new Row(key, english, russian, smart));
            }

            return rows;
        }

        private static void ValidatePlaceholderSchema(
            string table,
            int line,
            string english,
            string russian)
        {
            ValidateBalancedBraces(table, line, "English", english);
            ValidateBalancedBraces(table, line, "Russian", russian);
            string[] englishPlaceholders = GetPlaceholders(english);
            string[] russianPlaceholders = GetPlaceholders(russian);
            if (!englishPlaceholders.SequenceEqual(russianPlaceholders))
            {
                throw Error(table, line,
                    "English/Russian placeholder multiplicities differ ("
                    + string.Join(",", englishPlaceholders) + " vs "
                    + string.Join(",", russianPlaceholders) + ")");
            }
        }

        private static void ValidateBalancedBraces(
            string table,
            int line,
            string language,
            string value)
        {
            int depth = 0;
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (current == '{')
                {
                    if (depth == 0
                        && index + 1 < value.Length
                        && value[index + 1] == '{')
                    {
                        index++;
                        continue;
                    }

                    depth++;
                }
                else if (current == '}')
                {
                    if (depth == 0
                        && index + 1 < value.Length
                        && value[index + 1] == '}')
                    {
                        index++;
                        continue;
                    }

                    depth--;
                    if (depth < 0)
                    {
                        throw Error(table, line, language + " has an unmatched closing brace");
                    }
                }
            }

            if (depth != 0)
            {
                throw Error(table, line, language + " has an unmatched opening brace");
            }
        }

        private static string[] GetPlaceholders(string value)
        {
            return PlaceholderPattern.Matches(value)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool ContainsPlaceholder(string value)
        {
            return PlaceholderPattern.IsMatch(value);
        }

        private static bool ParseSmart(string table, int line, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (bool.TryParse(value.Trim(), out bool result))
            {
                return result;
            }

            throw Error(table, line, "Smart must be true, false, or empty");
        }

        private static string Decode(string value)
        {
            return value.Replace("\\n", "\n").Replace("\\r", "\r");
        }

        private static string[] Split(string line)
        {
            return line.Split(new[] { '\t' }, StringSplitOptions.None);
        }

        private static InvalidDataException Error(string table, int line, string message)
        {
            return new InvalidDataException(table + ".tsv line " + line + ": " + message);
        }

        internal readonly struct Row
        {
            internal Row(string key, string english, string russian, bool smart)
            {
                Key = key;
                English = english;
                Russian = russian;
                Smart = smart;
            }

            internal string Key { get; }
            internal string English { get; }
            internal string Russian { get; }
            internal bool Smart { get; }
        }
    }
}
