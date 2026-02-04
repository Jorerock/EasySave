using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EasySave.View
{
    public sealed class CommandParser
    {
        private readonly int _minJobId;
        private readonly int _maxJobId;

        public CommandParser(int minJobId = 1, int maxJobId = 5)
        {
            _minJobId = minJobId;
            _maxJobId = maxJobId;
        }

        public List<int> ParseJobSelection(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new List<int>();
            }

            string joined = string.Join(" ", args);
            return ParseJobSelection(joined);
        }

        /// <summary>
        /// Parse "1-3" => [1,2,3] ; "1;3" => [1,3]
        /// Supports separators: ';' ',' ' '.
        /// </summary>
        public List<int> ParseJobSelection(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return new List<int>();
            }

            string cleaned = commandLine.Trim();

            List<int> result = new List<int>();
            HashSet<int> unique = new HashSet<int>();

            // split by ';' ',' whitespace
            string[] parts = cleaned.Split(new char[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                // range 1-3
                int dashIndex = token.IndexOf('-', StringComparison.Ordinal);
                if (dashIndex > 0 && dashIndex < token.Length - 1)
                {
                    string left = token.Substring(0, dashIndex);
                    string right = token.Substring(dashIndex + 1);

                    int start = ParseIntStrict(left);
                    int end = ParseIntStrict(right);

                    if (end < start)
                    {
                        int tmp = start;
                        start = end;
                        end = tmp;
                    }

                    for (int id = start; id <= end; id++)
                    {
                        AddIfValid(unique, result, id);
                    }

                    continue;
                }

                int single = ParseIntStrict(token);
                AddIfValid(unique, result, single);
            }

            result.Sort();
            return result;
        }

        private int ParseIntStrict(string value)
        {
            bool ok = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed);
            if (!ok)
            {
                throw new FormatException($"Invalid job selection token: '{value}'.");
            }

            return parsed;
        }

        private void AddIfValid(HashSet<int> unique, List<int> result, int id)
        {
            if (id < _minJobId || id > _maxJobId)
            {
                return;
            }

            bool added = unique.Add(id);
            if (added)
            {
                result.Add(id);
            }
        }
    }
}
