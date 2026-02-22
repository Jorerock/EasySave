using EasySave.Core.Application;

namespace EasySave.Core.Application
{
    /// <summary>
    /// Fabrique un IBackupEngine configuré avec un IProgressReporter.
    /// Permet l'injection de dépendances et le découplage.
    /// </summary>
    public interface IBackupEngineFactory
    {
        /// <summary>Crée un moteur de sauvegarde lié au reporter fourni.</summary>
        IBackupEngineWithProgress Create(IProgressReporter reporter);
    }

    /// <summary>
    /// Extension de IBackupEngine qui accepte un IProgressReporter.
    /// FileSystemBackupEngine implémentera cette interface.
    /// </summary>
    public interface IBackupEngineWithProgress
    {
        void Run(EasySave.Core.Domain.BackupJob job, IProgressReporter reporter);
    }
}