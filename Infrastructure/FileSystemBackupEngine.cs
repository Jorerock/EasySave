
// Infrastructure/FileSystemBackupEngine.cs
using EasySave.Application;
using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            // State must be written in (near) real-time to a SINGLE state.json file.
            // Logs must be written to a daily JSON file.
            var state = new StateEntry
            {
                Timestamp = DateTime.UtcNow,
                BackupName = job.Name,
                CurrentSourceUNC = job.SourceDirectory,
                CurrentTargetUNC = job.TargetDirectory,
                State = JobRunState.Active,
                TotalFiles = 0,
                TotalSizeBytes = 0,
                FilesRemaining = 0,
                SizeRemainingBytes = 0,
                ProgressPct = 0
            };

            try
            {
                _stateWriter.WriteState(state);

                // Execute the backup according to its type
                switch (job.Type)
                {
                    case BackupType.Full:
                        ExecuteFullBackup(job, state);
                        break;
                    case BackupType.Differential:
                        ExecuteDifferentialBackup(job, state);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported backup type: {job.Type}");
                }

                state.Timestamp = DateTime.UtcNow;
                state.State = JobRunState.Completed;
                state.ProgressPct = 100;
                state.FilesRemaining = 0;
                state.SizeRemainingBytes = 0;
                _stateWriter.WriteState(state);
            }
            catch (Exception ex)
            {
                // Mark job as failed in state file
                state.Timestamp = DateTime.UtcNow;
                state.State = JobRunState.Failed;
                _stateWriter.WriteState(state);

                // Log error: TransferTimeMs must be negative on error
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    BackupName = job.Name,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = job.TargetDirectory,
                    FileSizeBytes = 0,
                    TransferTimeMs = -1
                });

                // Preserve stack trace
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
            state.FilesRemaining = files.Length;
            state.SizeRemainingBytes = state.TotalSizeBytes;
            state.Timestamp = DateTime.UtcNow;
            _stateWriter.WriteState(state);

            int filesCopied = 0;

            foreach (var file in files)
            {
                var startTime = DateTime.UtcNow;

                // Calculer le chemin relatif
                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);

                // Créer le répertoire de destination si nécessaire
                var targetFileDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetFileDir))
                    Directory.CreateDirectory(targetFileDir);

                // Copier le fichier
                File.Copy(file.FullName, targetPath, true);

                var transferTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Log du fichier copié
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    BackupName = job.Name,
                    SourcePathUNC = file.FullName,
                    TargetPathUNC = targetPath,
                    FileSizeBytes = file.Length,
                    TransferTimeMs = (long)transferTime
                });

                // Mise à jour de la progression
                filesCopied++;
                state.FilesRemaining = files.Length - filesCopied;
                state.SizeRemainingBytes = Math.Max(0, state.SizeRemainingBytes - file.Length);
                state.ProgressPct = files.Length == 0 ? 100 : (int)((filesCopied / (double)files.Length) * 100);
                state.Timestamp = DateTime.UtcNow;
                state.CurrentSourceUNC = file.FullName;
                state.CurrentTargetUNC = targetPath;
                _stateWriter.WriteState(state);
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
            state.SizeRemainingBytes = state.TotalSizeBytes;
            state.Timestamp = DateTime.UtcNow;
            _stateWriter.WriteState(state);

            int filesCopied = 0;

            foreach (var file in filesToCopy)
            {
                var startTime = DateTime.UtcNow;

                var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);

                var targetFileDir = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetFileDir))
                    Directory.CreateDirectory(targetFileDir);

                File.Copy(file.FullName, targetPath, true);

                var transferTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    BackupName = job.Name,
                    SourcePathUNC = file.FullName,
                    TargetPathUNC = targetPath,
                    FileSizeBytes = file.Length,
                    TransferTimeMs = (long)transferTime
                });

                filesCopied++;
                state.FilesRemaining = filesToCopy.Count - filesCopied;
                state.SizeRemainingBytes = Math.Max(0, state.SizeRemainingBytes - file.Length);
                state.ProgressPct = filesToCopy.Count == 0 ? 100 : (int)((filesCopied / (double)filesToCopy.Count) * 100);
                state.Timestamp = DateTime.UtcNow;
                state.CurrentSourceUNC = file.FullName;
                state.CurrentTargetUNC = targetPath;
                _stateWriter.WriteState(state);
            }
        }
    }
}