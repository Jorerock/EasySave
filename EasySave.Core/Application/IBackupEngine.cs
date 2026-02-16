
using EasySave.Core.Domain;

namespace EasySave.Core.Application{
    public interface IBackupEngine
    {
        void Run(BackupJob job);
    }
}
