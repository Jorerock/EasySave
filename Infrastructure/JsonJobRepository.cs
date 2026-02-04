using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EasySave.Infrastructure
{
    /// <summary>
    /// Implémentation JSON du repository de jobs
    /// </summary>
    internal class JsonJobRepository : Application.IJobRepository
    {
        private readonly string _configPath;

        public JsonJobRepository(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException(
                    "Le chemin du fichier de configuration est invalide.",
                    nameof(configPath)
                );
            }

            _configPath = configPath;
        }

        public List<Backupjob> LoadAll()
        {
            if (!File.Exists(_configPath))
            {
                return new List<Backupjob>();
            }

            string jsonContent = File.ReadAllText(_configPath);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return new List<Backupjob>();
            }

            List<Backupjob>? jobs =
                JsonSerializer.Deserialize<List<Backupjob>>(jsonContent);

            if (jobs == null)
            {
                return new List<Backupjob>();
            }

            return jobs;
        }

        public void SaveAll(List<Backupjob> jobs)
        {
            if (jobs == null)
            {
                throw new ArgumentNullException(nameof(jobs));
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent =
                JsonSerializer.Serialize<List<Backupjob>>(jobs, options);

            File.WriteAllText(_configPath, jsonContent);
        }
    }
}

