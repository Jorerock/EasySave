using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Application;
using EasySave.Domain;

namespace EasySave.Infrastructure
{
    /// <summary>
    /// Implémentation JSON du repository de jobs.
    /// </summary>
    public sealed class JsonJobRepository : IJobRepository
    {
        private readonly string _configPath;

        public JsonJobRepository(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException("Le chemin du fichier de configuration est invalide.", nameof(configPath));
            }

            _configPath = configPath;
        }

        public List<BackupJob> LoadAll()
        {
            if (!File.Exists(_configPath))
            {
                return new List<BackupJob>();
            }

            string jsonContent = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new List<BackupJob>();
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<BackupJob>? jobs = JsonSerializer.Deserialize<List<BackupJob>>(jsonContent, options);
            return jobs ?? new List<BackupJob>();
        }

        public void SaveAll(List<BackupJob> jobs)
        {
            if (jobs == null)
            {
                throw new ArgumentNullException(nameof(jobs));
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(jobs, options);

            string? dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(_configPath, jsonContent);
        }
    }
}
