using System;
using System.Collections.Generic;
using System.Globalization;

namespace Chronoforge
{
    /// <summary>
    /// Imports steps from CSV/TSV — the spreadsheet migration path. Auto-detects the delimiter
    /// (comma / semicolon / tab), tolerates an optional header row (maps known columns by name),
    /// and falls back to a fixed column order otherwise. Quoted fields are supported. Pure and
    /// allocation-light; never throws — problems are reported through <paramref name="error"/>.
    /// </summary>
    public static class BuildOrderCsvImporter
    {
        private static readonly string[] k_KnownHeaders =
            { "supply", "time", "type", "title", "notes" };

        public static List<BuildOrderStep> Parse(string text, out string error)
        {
            error = "";
            var steps = new List<BuildOrderStep>();
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Empty CSV.";
                return steps;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            char delimiter = DetectDelimiter(FirstNonEmpty(lines));

            int[] map = null;
            bool headerConsumed = false;

            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                List<string> cells = SplitLine(rawLine, delimiter);

                if (!headerConsumed)
                {
                    headerConsumed = true;
                    if (LooksLikeHeader(cells))
                    {
                        map = BuildColumnMap(cells);
                        continue;
                    }
                }

                BuildOrderStep step = ParseRow(cells, map);
                if (step != null)
                    steps.Add(step);
            }

            if (steps.Count == 0 && error.Length == 0)
                error = "No rows parsed.";
            return steps;
        }

        #region Row parsing
        private static BuildOrderStep ParseRow(List<string> cells, int[] map)
        {
            string supplyText = Column(cells, map, 0);
            string timeText = Column(cells, map, 1);
            string typeText = Column(cells, map, 2);
            string title = Column(cells, map, 3);
            string notes = Column(cells, map, 4);

            var step = BuildOrderStep.Create(ParseType(typeText));
            step.m_Title = title;
            step.m_DesignerNotes = notes;

            if (int.TryParse(supplyText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int supply))
                step.m_Supply = supply;
            if (BuildOrderTime.TryParse(timeText, out float seconds))
                step.m_TimeSeconds = seconds;

            return step;
        }

        private static BuildOrderActionType ParseType(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) &&
                Enum.TryParse(text.Trim(), ignoreCase: true, out BuildOrderActionType type))
                return type;
            return BuildOrderActionType.Custom;
        }
        #endregion

        #region Header & column mapping
        private static bool LooksLikeHeader(List<string> cells)
        {
            foreach (string cell in cells)
            {
                if (Array.IndexOf(k_KnownHeaders, cell.Trim().ToLowerInvariant()) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>Returns, for each logical field index (0..4), the source column, or -1.</summary>
        private static int[] BuildColumnMap(List<string> header)
        {
            var map = new[] { -1, -1, -1, -1, -1 };
            for (int column = 0; column < header.Count; column++)
            {
                int field = Array.IndexOf(k_KnownHeaders, header[column].Trim().ToLowerInvariant());
                if (field >= 0)
                    map[field] = column;
            }
            return map;
        }

        private static string Column(List<string> cells, int[] map, int field)
        {
            int index = map != null ? map[field] : field;
            return index >= 0 && index < cells.Count ? cells[index].Trim() : "";
        }
        #endregion

        #region Low-level CSV
        private static char DetectDelimiter(string line)
        {
            if (line == null)
                return ',';
            int commas = Count(line, ',');
            int semis = Count(line, ';');
            int tabs = Count(line, '\t');
            if (tabs >= commas && tabs >= semis && tabs > 0)
                return '\t';
            if (semis > commas)
                return ';';
            return ',';
        }

        private static List<string> SplitLine(string line, char delimiter)
        {
            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            cells.Add(current.ToString());
            return cells;
        }

        private static string FirstNonEmpty(string[] lines)
        {
            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    return line;
            }
            return null;
        }

        private static int Count(string text, char c)
        {
            int count = 0;
            foreach (char ch in text)
            {
                if (ch == c)
                    count++;
            }
            return count;
        }
        #endregion
    }
}
