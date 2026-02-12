using EasySave.Domain;

namespace EasySave.Application
{
    public interface IJobRepository
    {
        List<BackupJob> LoadAll();
        void SaveAll(List<BackupJob> jobs);
    }
}
