using EasySave.Core.Application;
using EasySave.Core.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EasyLog.Interfaces;
using EasyLog.Entries;

namespace EasySave.Core.Infrastructure
{
    /// <summary>
    /// Moteur de sauvegarde étendu avec support Pause/Play/Stop via IProgressReporter.
    /// Implémente à la fois IBackupEngine (compatibilité CLI) et IBackupEngineWithProgress (WPF parallèle).
    /// </summary>
    public class FileSystemBackupEngine : IBackupEngine, IBackupEngineWithProgress
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;
        private readonly AppSettings _settings;
        private readonly IBusinessSoftwareDetector _detector;

        public FileSystemBackupEngine(
            ILogWriter logWriter,
            IStateWriter stateWriter,
            AppSettings settings,
            IBusinessSoftwareDetector detector)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        }

        // ── IBackupEngine (CLI — sans progression) ────────────────────
        public void Run(BackupJob job)
            => Run(job, null);

        // ── IBackupEngineWithProgress (WPF parallèle) ─────────────────
        public void Run(BackupJob job, IProgressReporter reporter)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            // Vérification logiciel métier
            if (_detector.IsBlocked())
            {
                _stateWriter.WriteState(new StateEntry { State = JobRunState.BlockedByBusinessSoftware });
                _logWriter.WriteDailyLog(new LogEntry
                {
                    BackupName = job.Name,
                    Timestamp = DateTime.Now,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = job.TargetDirectory,
                    TransferTimeMs = -1
                });
                return;
            }

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
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = job.TargetDirectory,
                    FileSizeBytes = 0,
                    TransferTimeMs = 0
                });

                switch (job.Type)
                {
                    case BackupType.Full:
                        ExecuteFullBackup(job, state, reporter);
                        break;
                    case BackupType.Differential:
                        ExecuteDifferentialBackup(job, state, reporter);
                        break;
                    default:
                        throw new NotSupportedException($"Type de backup non supporté : {job.Type}");
                }

                // Vérifier si annulé avant de marquer Completed
                reporter?.CancellationToken.ThrowIfCancellationRequested();

                state.State = JobRunState.Completed;
                state.ProgressPct = 100;
                _stateWriter.WriteState(state);
            }
            catch (OperationCanceledException)
            {
                state.State = JobRunState.Stopped;
                _stateWriter.WriteState(state);
                throw; // propagé vers ParallelBackupOrchestrator
            }
            catch (Exception ex)
            {
                state.State = JobRunState.Failed;
                _stateWriter.WriteState(state);
                _logWriter.WriteDailyLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = job.Name,
                    SourcePathUNC = job.SourceDirectory,
                    TargetPathUNC = $"ERROR: {ex.Message}",
                    FileSizeBytes = 0,
                    TransferTimeMs = -1
                });
                throw;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // FULL BACKUP
        // ══════════════════════════════════════════════════════════════

        private void ExecuteFullBackup(BackupJob job, StateEntry state, IProgressReporter reporter)
        {
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Source introuvable : {job.SourceDirectory}");
            if (!targetDir.Exists)
                targetDir.Create();

            CryptoSoftEncryptorAdapter encryptor = BuildEncryptor(job);
            var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            state.TotalFiles = files.Length;
            state.TotalSizeBytes = files.Sum(f => f.Length);
            state.FilesRemaining = files.Length;

            int filesCopied = 0;
            foreach (var file in files)
            {
                // ── Point de contrôle Stop ──────────────────────────
                reporter?.CancellationToken.ThrowIfCancellationRequested();

                // ── Reporter : fichier en cours ─────────────────────
                reporter?.ReportFile(file.FullName, files.Length - filesCopied, files.Length);

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
                    int pct = files.Length > 0
                        ? (int)((filesCopied / (double)files.Length) * 100)
                        : 0;

                    state.FilesRemaining = files.Length - filesCopied;
                    state.ProgressPct = pct;
                    _stateWriter.WriteState(state);

                    // ── Reporter : mise à jour progression ──────────
                    reporter?.ReportProgress(pct, state.FilesRemaining);
                }

                // ── Point de contrôle Pause (APRÈS le fichier) ──────
                reporter?.WaitIfPaused();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // DIFFERENTIAL BACKUP
        // ══════════════════════════════════════════════════════════════

        private void ExecuteDifferentialBackup(BackupJob job, StateEntry state, IProgressReporter reporter)
        {
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists)
                throw new DirectoryNotFoundException($"Source introuvable : {job.SourceDirectory}");
            if (!targetDir.Exists)
                targetDir.Create();

            CryptoSoftEncryptorAdapter encryptor = BuildEncryptor(job);

            var sourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            var filesToCopy = new List<FileInfo>();
            long totalSize = 0;

            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir.FullName, sourceFile.FullName);
                var targetPath = Path.Combine(targetDir.FullName, relativePath);

                bool shouldCopy = !File.Exists(targetPath);
                if (!shouldCopy)
                {
                    var targetFile = new FileInfo(targetPath);
                    shouldCopy = sourceFile.Length != targetFile.Length ||
                                 Math.Abs((sourceFile.LastWriteTime - targetFile.LastWriteTime).TotalSeconds) > 1;
                }

                if (shouldCopy)
                {
                    filesToCopy.Add(sourceFile);
                    totalSize += sourceFile.Length;
                }
            }

            state.TotalFiles = filesToCopy.Count;
            state.TotalSizeBytes = totalSize;
            state.FilesRemaining = filesToCopy.Count;
            state.SizeRemainingBytes = totalSize;
            state.Timestamp = DateTime.UtcNow;
            _stateWriter.WriteState(state);

            int filesCopied = 0;
            foreach (var file in filesToCopy)
            {
                // ── Stop ─────────────────────────────────────────────
                reporter?.CancellationToken.ThrowIfCancellationRequested();

                reporter?.ReportFile(file.FullName, filesToCopy.Count - filesCopied, filesToCopy.Count);

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
                    int pct = filesToCopy.Count > 0
                        ? (int)((filesCopied / (double)filesToCopy.Count) * 100)
                        : 0;

                    state.FilesRemaining = filesToCopy.Count - filesCopied;
                    state.ProgressPct = pct;
                    _stateWriter.WriteState(state);

                    reporter?.ReportProgress(pct, state.FilesRemaining);
                }

                // ── Pause (APRÈS le fichier) ─────────────────────────
                reporter?.WaitIfPaused();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private CryptoSoftEncryptorAdapter BuildEncryptor(BackupJob job)
        {
            if (job.EnableEncryption && !string.IsNullOrEmpty(job.EncryptionKey))
                return new CryptoSoftEncryptorAdapter(job.EncryptionKey, _logWriter, _settings);
            return null;
        }

        private void ProcessFile(
            FileInfo file,
            DirectoryInfo sourceDir,
            DirectoryInfo targetDir,
            BackupJob job,
            CryptoSoftEncryptorAdapter encryptor)
        {
            var startTime = DateTime.Now;
            var relativePath = Path.GetRelativePath(sourceDir.FullName, file.FullName);
            var targetPath = Path.Combine(targetDir.FullName, relativePath);

            var targetFileDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir);

            File.Copy(file.FullName, targetPath, true);
            var copyTime = (DateTime.Now - startTime).TotalMilliseconds;

            int encryptTime = 0;
            if (encryptor != null && encryptor.ShouldEncrypt(targetPath))
            {
                encryptTime = encryptor.EncryptFile(targetPath, job.Name);
                if (encryptTime < 0)
                    Console.WriteLine($"Échec du chiffrement : {targetPath}");
            }

            _logWriter.WriteDailyLog(new LogEntry
            {
                Message = "Operation type : Copy File",
                Timestamp = DateTime.Now,
                BackupName = job.Name,
                SourcePathUNC = file.FullName,
                TargetPathUNC = targetPath,
                FileSizeBytes = file.Length,
                TransferTimeMs = (long)copyTime,
                EncryptionTimeMs = encryptTime,
            });
        }
    }
}