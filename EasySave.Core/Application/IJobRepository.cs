using EasySave.Core.Domain;
using System.Collections.Generic;

namespace EasySave.Core.Application
{
    public interface IJobRepository
    {
        List<BackupJob> LoadAll();
        void SaveAll(List<BackupJob> jobs);
    }
}
