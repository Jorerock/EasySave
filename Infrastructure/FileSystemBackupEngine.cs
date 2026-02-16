
// Infrastructure/FileSystemBackupEngine.cs
using EasySave.Application;
using EasySave.Domain;
using EasyLog; // Référence à la DLL
using System;
using System.IO;
using EasyLog.Interfaces;
using EasyLog.Entries;

namespace EasySave.Infrastructure
{

    internal class FileSystemBackupEngine : IBackupEngine
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;

        public FileSystemBackupEngine(ILogWriter logWriter, IStateWriter stateWriter)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
        }


       
        public void Run(BackupJob job)
        {
            // 1. Vérification via le détecteur injecté
            if (_businessSoftwareDetector.IsBlocked())
            {
                // 2. Création de l'état bloqué
                StateEntry blockedState = new StateEntry
                {
                    State = JobRunState.BlockedByBusinessSoftware,
                    // ... autres infos de progression à 0
                };
                _stateWriter.WriteState(blockedState);

                // 3. Consignation dans le Log (comme demandé)
                LogEntry log = new LogEntry
                {
                    BackupName = job.Name,
                    Timestamp = DateTime.Now,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = job.TargetDirectory,
                    TransferTimeMs = -1
                };
                _logWriter.WriteDailyLog(log);

                return; // On arrête l'exécution ici
            }

            // ... sinon, continuer la sauvegarde normale
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                // Créer l'état initial
                var state = new StateEntry
                {
                    BackupName = job.Name,
                    CurrentSourceUNC = job.SourceDirectory,
                    CurrentTargetUNC = job.TargetDirectory,
                    State = JobRunState.Active,
                    TotalFiles = 0,
                    TotalSizeBytes = 0,
                    FilesRemaining = 0,
                    ProgressPct = 0
                };

                // Log de début
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = job.TargetDirectory,
                    FileSizeBytes = 0,
                    TransferTimeMs = 0
                });

                // Exécuter la sauvegarde selon le type
                switch (job.Type)
                {
                    case BackupType.Full:
                        ExecuteFullBackup(job, state);
                        break;
                    case BackupType.Differential:
                        ExecuteDifferentialBackup(job, state);
                        break;
                    default:
                        throw new NotSupportedException($"Type de backup non supporté: {job.Type}");
                }

                // Mettre à jour l'état final
                state.State = JobRunState.Completed;
                state.ProgressPct = 100;
                //_stateWriter.WriteState(state);
                //Todo Write State
            }
            catch (Exception ex)
            {
                // Log d'erreur
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = $"ERROR: {ex.Message}",
                    FileSizeBytes = 0,
                    TransferTimeMs = 0
                });

                throw;
            }
        }

        private void ExecuteFullBackup(BackupJob job, StateEntry state)
        {
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Répertoire source introuvable: {job.SourceDirectory}");

            if (!targetDir.Exists)
                targetDir.Create();

            var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            state.TotalFiles = files.Length;
            state.TotalSizeBytes = files.Sum(f => f.Length);
            state.SizeRemainingBytes = files.Length;

            int filesCopied = 0;

            foreach (var file in files)
            {
                var startTime = DateTime.Now;
                
                // Calculer le chemin relatif
                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);
                
                // Créer le répertoire de destination si nécessaire
                var targetFileDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetFileDir))
                    Directory.CreateDirectory(targetFileDir);

                // Copier le fichier
                File.Copy(file.FullName, targetPath, true);

                var transferTime = (DateTime.Now - startTime).TotalMilliseconds;

                // Log du fichier copié
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = file.FullName,
                    TargetPathUNC = targetPath,
                    FileSizeBytes = file.Length,
                    TransferTimeMs = (long)transferTime
                });

                // Mise à jour de la progression
                filesCopied++;
                state.FilesRemaining = files.Length - filesCopied;
                state.ProgressPct = (int)((filesCopied / (double)files.Length) * 100);
                //_stateWriter.(state);
                //Todo Write State
            }
        }

        private void ExecuteDifferentialBackup(BackupJob job, StateEntry state)
        {
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Répertoire source introuvable: {job.SourceDirectory}");

            if (!targetDir.Exists)
                targetDir.Create();

            var sourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            var filesToCopy = new List<FileInfo>();

            // Ne copier que les fichiers nouveaux ou modifiés
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir.FullName, sourceFile.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);

                if (!File.Exists(targetPath))
                {
                    filesToCopy.Add(sourceFile);
                }
                else
                {
                    var targetFile = new FileInfo(targetPath);
                    if (sourceFile.LastWriteTime > targetFile.LastWriteTime)
                    {
                        filesToCopy.Add(sourceFile);
                    }
                }
            }

            state.TotalFiles = filesToCopy.Count;
            state.TotalSizeBytes = filesToCopy.Sum(f => f.Length);
            state.FilesRemaining = filesToCopy.Count;

            int filesCopied = 0;

            foreach (var file in filesToCopy)
            {
                var startTime = DateTime.Now;
                
                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);
                
                var targetFileDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetFileDir))
                    Directory.CreateDirectory(targetFileDir);

                File.Copy(file.FullName, targetPath, true);

                var transferTime = (DateTime.Now - startTime).TotalMilliseconds;

                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = file.FullName,
                    TargetPathUNC = targetPath,
                    FileSizeBytes = file.Length,
                    TransferTimeMs = (long)transferTime
                });

                filesCopied++;
                state.FilesRemaining = filesToCopy.Count - filesCopied;
                state.ProgressPct = (int)((filesCopied / (double)filesToCopy.Count) * 100);
                _stateWriter.WriteState(state);
                //tODO repair state
            }
        }
    }
}