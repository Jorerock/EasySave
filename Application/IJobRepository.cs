using EasySave.Domain;
using System.Collections.Generic;

namespace EasySave.Application
{
    public interface IJobRepository
    {
        List<BackupJob> LoadAll();
        void SaveAll(List<BackupJob> jobs);
    }
}
