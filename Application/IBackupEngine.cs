
using EasySave.Domain;

namespace EasySave.Application
{
    internal interface IBackupEngine
    {
        void Run(BackupJob job);
    }
}
