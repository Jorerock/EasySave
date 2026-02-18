using EasySave.Core.Application;
using EasySave.Core.Domain;
using System;
using System.IO;

namespace EasySave.Core.Infrastructure
{

    public sealed class JsonSettingsRepository : JsonRepositoryBase, ISettingsRepository
    {
        public JsonSettingsRepository(string configPath = null) : base(
            configPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave",
                "settings.json"
            ))
        {
        }

        public AppSettings Load()
        {
            return LoadFromFile<AppSettings>();
        }

        public void Save(AppSettings settings)
        {
            SaveToFile(settings);
        }
    }
}