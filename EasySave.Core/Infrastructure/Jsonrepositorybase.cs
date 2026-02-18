using System;
using System.IO;
using System.Text.Json;

namespace EasySave.Core.Infrastructure
{
    /// <summary>
    /// Classe de base abstraite pour les repositories JSON.
    /// Factorise la logique de sérialisation/désérialisation.
    /// </summary>
    public abstract class JsonRepositoryBase
    {
        protected readonly string ConfigPath;

        protected JsonRepositoryBase(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException("Le chemin du fichier de configuration est invalide.", nameof(configPath));
            }

            ConfigPath = configPath;

            // Crée le répertoire si nécessaire
            string? dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Charge un objet depuis le fichier JSON
        /// </summary>
        protected T LoadFromFile<T>() where T : class
        {
            if (!File.Exists(ConfigPath))
            {
                return null; // Retourne null si le fichier n'existe pas
            }

            string jsonContent = File.ReadAllText(ConfigPath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return null;
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            T? result = JsonSerializer.Deserialize<T>(jsonContent, options);
            return result;
        }

        /// <summary>
        /// Sauvegarde un objet dans le fichier JSON
        /// </summary>
        protected void SaveToFile<T>(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(data, options);
            File.WriteAllText(ConfigPath, jsonContent);
        }
    }
}