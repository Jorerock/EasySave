using EasyLog.Entries;
using EasyLog.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EasyLog.Writers
{
    public sealed class JsonStateWriter : IStateWriter
    {
        private readonly string _stateFilePath;
        private readonly object _sync;

        public JsonStateWriter(string stateFilePath)
        {
            if (string.IsNullOrWhiteSpace(stateFilePath))
            {
                throw new ArgumentException("State file path cannot be null/empty.", nameof(stateFilePath));
            }

            _stateFilePath = stateFilePath;
            _sync = new object();
        }

        public void WriteState(StateEntry state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            string? dir = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(state, options);

            lock (_sync)
            {
                // Write atomically (best effort)
                string tempPath = _stateFilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(_stateFilePath))
                {
                    File.Delete(_stateFilePath);
                }

                File.Move(tempPath, _stateFilePath);
            }
        }
    }
}
