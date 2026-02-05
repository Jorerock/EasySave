// Infrastructure/FileSystemBackupEngine.cs
using EasySave.Application;
using EasySave.Domain;
using EasyLog; // Référence à la DLL
using System;
using System.IO;

namespace EasySave.Infrastructure
{
    internal class FileSystemBackupEngine : IBackupEngine
    {
        private readonly ILogWriter _logWriter;
        private readonly ILogWriter _stateWriter;

        public FileSystemBackupEngine(ILogWriter logWriter, ILogWriter stateWriter)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
        }

        public void Run(BackupJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                // Créer l'état initial
                var state = new StateEntry
                {
                    Name = job.Name,
                    SourceDirectory = job.SourceDirectory,
                    TargetDirectory = job.TargetDirectory,
                    State = "Active",
                    TotalFilesToCopy = 0,
                    TotalFilesSize = 0,
                    NbFilesLeftToDo = 0,
                    Progression = 0
                };

                // Log de début
                _logWriter.WriteEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Name = job.Name,
                    FileSource = job.SourceDirectory,
                    FileTarget = job.TargetDirectory,
                    FileSize = 0,
                    FileTransferTime = 0
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
                state.State = "Completed";
                state.Progression = 100;
                _stateWriter.WriteEntry(state);
            }
            catch (Exception ex)
            {
                // Log d'erreur
                _logWriter.WriteEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Name = job.Name,
                    FileSource = job.SourceDirectory,
                    FileTarget = $"ERROR: {ex.Message}",
                    FileSize = 0,
                    FileTransferTime = 0
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
            state.TotalFilesToCopy = files.Length;
            state.TotalFilesSize = files.Sum(f => f.Length);
            state.NbFilesLeftToDo = files.Length;

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
                _logWriter.WriteEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Name = job.Name,
                    FileSource = file.FullName,
                    FileTarget = targetPath,
                    FileSize = file.Length,
                    FileTransferTime = transferTime
                });

                // Mise à jour de la progression
                filesCopied++;
                state.NbFilesLeftToDo = files.Length - filesCopied;
                state.Progression = (int)((filesCopied / (double)files.Length) * 100);
                _stateWriter.WriteEntry(state);
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

            state.TotalFilesToCopy = filesToCopy.Count;
            state.TotalFilesSize = filesToCopy.Sum(f => f.Length);
            state.NbFilesLeftToDo = filesToCopy.Count;

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

                _logWriter.WriteEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Name = job.Name,
                    FileSource = file.FullName,
                    FileTarget = targetPath,
                    FileSize = file.Length,
                    FileTransferTime = transferTime
                });

                filesCopied++;
                state.NbFilesLeftToDo = filesToCopy.Count - filesCopied;
                state.Progression = (int)((filesCopied / (double)filesToCopy.Count) * 100);
                _stateWriter.WriteEntry(state);
            }
        }
    }
}