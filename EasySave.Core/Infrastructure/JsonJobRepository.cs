using System.Collections.Generic;
using EasySave.Core.Application;
using EasySave.Core.Domain;

namespace EasySave.Core.Infrastructure
{
    public sealed class JsonJobRepository : JsonRepositoryBase, IJobRepository
    {
        public JsonJobRepository(string configPath) : base(configPath)
        {
        }

        public List<BackupJob> LoadAll()
        {
            return LoadFromFile<List<BackupJob>>() ?? new List<BackupJob>();
        }

        public void SaveAll(List<BackupJob> jobs)
        {
            SaveToFile(jobs);
        }
    }
}