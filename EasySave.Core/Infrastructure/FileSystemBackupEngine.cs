
// Infrastructure/FileSystemBackupEngine.cs
using EasySave.Core.Application;
using EasySave.Core.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyLog.Interfaces;
using EasyLog.Entries;
namespace EasySave.Core.Infrastructure
{
    public class FileSystemBackupEngine : IBackupEngine
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
            // Validate and prepare directories
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            if (!targetDir.Exists)
                targetDir.Create();

            // Initialize encryptor if encryption is enabled
            CryptoSoftEncryptorAdapter encryptor = null;
            if (job.EnableEncryption && !string.IsNullOrEmpty(job.EncryptionKey))
            {
                encryptor = new CryptoSoftEncryptorAdapter(job.EncryptionKey, _logWriter);
            }

            // Select ALL files for full backup
            var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            // Initialize state
            state.TotalFiles = files.Length;
            state.TotalSizeBytes = files.Sum(f => f.Length);
            state.FilesRemaining = files.Length;

            // Process each file
            int filesCopied = 0;
            foreach (var file in files)
            {
                try
                {
                    // Process single file
                    ProcessFile(file, sourceDir, targetDir, job, encryptor);
                    filesCopied++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file.FullName}: {ex.Message}");
                }
                finally
                {
                    // Update progress
                    state.FilesRemaining = files.Length - filesCopied;
                    state.ProgressPct = files.Length > 0
                        ? (int)((filesCopied / (double)files.Length) * 100)
                        : 0;
                    _stateWriter.WriteState(state);
                }
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

            // Encryptor initialization
            CryptoSoftEncryptorAdapter encryptor = null;
            if (job.EnableEncryption && !string.IsNullOrEmpty(job.EncryptionKey))
            {
                encryptor = new CryptoSoftEncryptorAdapter(job.EncryptionKey, _logWriter);
            }

            // Sélection des fichiers (logique différentielle)
            var sourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            var filesToCopy = new List<FileInfo>();
            long totalSize = 0;

            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir.FullName, sourceFile.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);

                bool shouldCopy = false;

                if (!File.Exists(targetPath))
                {
                    shouldCopy = true;
                }
                else
                {
                    var targetFile = new FileInfo(targetPath);
                    if (sourceFile.Length != targetFile.Length ||
                        Math.Abs((sourceFile.LastWriteTime - targetFile.LastWriteTime).TotalSeconds) > 1)
                    {
                        shouldCopy = true;
                    }
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(sourceFile);
                    totalSize += sourceFile.Length;
                }
            }

            // Initialiser l'état
            state.TotalFiles = filesToCopy.Count;
            state.TotalSizeBytes = totalSize;
            state.FilesRemaining = filesToCopy.Count;
            state.SizeRemainingBytes = state.TotalSizeBytes;
            state.Timestamp = DateTime.UtcNow;
            _stateWriter.WriteState(state);


            int filesCopied = 0;
            foreach (var file in filesToCopy)
            {
                try
                {
                    ProcessFile(file, sourceDir, targetDir, job, encryptor);
                    filesCopied++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur sur {file.FullName}: {ex.Message}");
                }
                finally
                {
                    // state updating after each file
                    state.FilesRemaining = filesToCopy.Count - filesCopied;
                    state.ProgressPct = filesToCopy.Count > 0
                        ? (int)((filesCopied / (double)filesToCopy.Count) * 100)
                        : 0;
                    _stateWriter.WriteState(state);
                }
            }
        }

        private void ProcessFile(FileInfo file, DirectoryInfo sourceDir, DirectoryInfo targetDir, BackupJob job, CryptoSoftEncryptorAdapter encryptor)
        {
            var startTime = DateTime.Now;
            var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
            var targetPath = Path.Combine(targetDir.FullName, relativePath);

            var targetFileDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir);

            // Copy
            File.Copy(file.FullName, targetPath, true);
            var copyTime = (DateTime.Now - startTime).TotalMilliseconds;

            // Logger
            _logWriter.WriteDailyLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = job.Name,
                SourcePathUNC = file.FullName,
                TargetPathUNC = targetPath,
                FileSizeBytes = file.Length,
                TransferTimeMs = (long)copyTime
            });

            // Crypt
            if (encryptor != null &&
                CryptoSoftEncryptorAdapter.ShouldEncrypt(targetPath, job.ExtensionsToEncrypt))
            {
                int encryptTime = encryptor.EncryptFile(targetPath, job.Name);

                if (encryptTime < 0)
                {
                    Console.WriteLine($"Échec du cryptage - {targetPath}");
                }
            }
        }

    }



  }
