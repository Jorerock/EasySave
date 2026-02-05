using EasyLog.Entries;
using EasyLog.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace EasyLog.Writers
{
    public sealed class JsonLogWriter : ILogWriter
    {
        private readonly string _logDirectoryPath;
        private readonly object _sync;

        public JsonLogWriter(string logDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(logDirectoryPath))
            {
                throw new ArgumentException("Log directory path cannot be null/empty.", nameof(logDirectoryPath));
            }

            _logDirectoryPath = logDirectoryPath;
            _sync = new object();
        }

        public void WriteDailyLog(LogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            Directory.CreateDirectory(_logDirectoryPath);

            string filePath = Path.Combine(_logDirectoryPath, GetDailyLogFileName(entry.Timestamp));

            lock (_sync)
            {
                List<LogEntry> current = ReadExistingArray(filePath);
                current.Add(entry);
                WriteArray(filePath, current);
            }
        }

        private string GetDailyLogFileName(DateTime timestamp)
        {
            string datePart = timestamp.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return datePart + ".json";
        }

        private List<LogEntry> ReadExistingArray(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<LogEntry>();
            }

            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<LogEntry>();
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<LogEntry>? parsed = JsonSerializer.Deserialize<List<LogEntry>>(json, options);
            return parsed ?? new List<LogEntry>();
        }

        private void WriteArray(string filePath, List<LogEntry> entries)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(entries, options);

            // Write atomically (best effort): write temp then replace
            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.Move(tempPath, filePath);
        }
    }
}
