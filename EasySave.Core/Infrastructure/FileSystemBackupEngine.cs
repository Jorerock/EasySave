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
    /// 

    public class FileSystemBackupEngine : IBackupEngine, IBackupEngineWithProgress
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;
        private readonly AppSettings _settings;
        private readonly IBusinessSoftwareDetector _detector;
        private readonly long _largeFileSizeThreshold;
        // Sémaphore pour limiter le parallélisme des gros fichiers
        // On l'initialise à 1 pour forcer le mode séquentiel sur les gros fichiers
        private static readonly SemaphoreSlim _largeFileSemaphore = new SemaphoreSlim(1, 1);
        private readonly PriorityTransferCoordinator _priorityCoordinator;

        public FileSystemBackupEngine(
            ILogWriter logWriter,
            IStateWriter stateWriter,
            AppSettings settings,
            IBusinessSoftwareDetector detector,
            PriorityTransferCoordinator priorityCoordinator)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _priorityCoordinator = priorityCoordinator ?? throw new ArgumentNullException(nameof(priorityCoordinator));
        }

        // ── IBackupEngine (CLI — sans progression) ────────────────────
        public void Run(BackupJob job)
            => Run(job, null);


        public async Task Run(BackupJob job, IProgressReporter reporter)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            // 1. GESTION DE LA PAUSE / BLOCAGE (Logiciel Métier)
            // On boucle tant que le logiciel métier est détecté pour mettre en pause
            while (_detector.IsBlocked())
            {
                _stateWriter.WriteState(new StateEntry
                {
                    BackupName = job.Name,
                    State = JobRunState.BlockedByBusinessSoftware
                });

                // Attente avant nouvelle vérification (évite de saturer le CPU)
                Thread.Sleep(2000);

                // Permet l'arrêt si l'utilisateur clique sur "Stop" pendant la pause
                if (reporter?.CancellationToken.IsCancellationRequested == true) break;
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

                // 2. LOGIQUE DE TRANSFERT AVEC LIMITE DE PARALLÉLISME
                // Note : La logique de vérification de taille (n Ko) doit être injectée 
                // dans ExecuteFullBackup / ExecuteDifferentialBackup pour entourer le File.Copy

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

                reporter?.CancellationToken.ThrowIfCancellationRequested();

                state.State = JobRunState.Completed;
                state.ProgressPct = 100;
                _stateWriter.WriteState(state);
            }
            catch (OperationCanceledException)
            {
                state.State = JobRunState.Stopped;
                _stateWriter.WriteState(state);
                throw;
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

            if (!sourceDir.Exists) throw new DirectoryNotFoundException($"Source introuvable : {job.SourceDirectory}");
            if (!targetDir.Exists) targetDir.Create();

            CryptoSoftEncryptorAdapter encryptor = BuildEncryptor(job);
            var files = sourceDir.GetFiles("*", SearchOption.AllDirectories);

            
            int priorityCount = files.Count(IsPriorityFile);
            _priorityCoordinator.RegisterJob(job.Name, priorityCount);

            try
            {
                state.TotalFiles = files.Length;
                state.TotalSizeBytes = files.Sum(f => f.Length);
                state.FilesRemaining = files.Length;

                int filesCopied = 0;
                foreach (var file in files)
                {
                    reporter?.CancellationToken.ThrowIfCancellationRequested();

                    reporter?.ReportFile(file.FullName, files.Length - filesCopied, files.Length);

                    bool isPriority = IsPriorityFile(file);

                   
                    if (!isPriority)
                    {
                        var token = reporter?.CancellationToken ?? CancellationToken.None;
                        _priorityCoordinator.WaitIfPrioritiesExist(token);
                    }

                    try
                    {
                        ProcessFile(file, sourceDir, targetDir, job, encryptor);
                        filesCopied++;
                        if (isPriority)
                        {
                            _priorityCoordinator.MarkPriorityDone(job.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur sur {file.FullName}: {ex.Message}");

               
                        if (isPriority)
                        {
                            _priorityCoordinator.MarkPriorityDone(job.Name);
                        }
                    }
                    finally
                    {
                        int pct = files.Length > 0
                            ? (int)((filesCopied / (double)files.Length) * 100)
                            : 0;

                        state.FilesRemaining = files.Length - filesCopied;
                        state.ProgressPct = pct;
                        _stateWriter.WriteState(state);

                        reporter?.ReportProgress(pct, state.FilesRemaining);
                    }

                    reporter?.WaitIfPaused();
                }
            }
            finally
            {
                // ✅ 4) Toujours unregister même si exception/stop
                _priorityCoordinator.UnregisterJob(job.Name);
            }
        }

        // --- DIFFERENTIAL BACKUP ---
        private void ExecuteDifferentialBackup(BackupJob job, StateEntry state, IProgressReporter reporter)
        {
            var sourceDir = new DirectoryInfo(job.SourceDirectory);
            var targetDir = new DirectoryInfo(job.TargetDirectory);

            if (!sourceDir.Exists) throw new DirectoryNotFoundException($"Source introuvable : {job.SourceDirectory}");
            if (!targetDir.Exists) targetDir.Create();

            CryptoSoftEncryptorAdapter encryptor = BuildEncryptor(job);
            var filesToCopy = GetDifferentialFiles(sourceDir, targetDir);

            // ✅ Register priorité (sur la liste réellement copiée)
            int priorityCount = filesToCopy.Count(IsPriorityFile);
            _priorityCoordinator.RegisterJob(job.Name, priorityCount);

            try
            {
                state.TotalFiles = filesToCopy.Count;
                state.TotalSizeBytes = totalSize;
                state.FilesRemaining = filesToCopy.Count;
                state.SizeRemainingBytes = totalSize;
                state.Timestamp = DateTime.UtcNow;
                _stateWriter.WriteState(state);

                int filesCopied = 0;
                foreach (var file in filesToCopy)
                {
                    reporter?.CancellationToken.ThrowIfCancellationRequested();

                    reporter?.ReportFile(file.FullName, filesToCopy.Count - filesCopied, filesToCopy.Count);

                    bool isPriority = IsPriorityFile(file);

                    // ✅ Si non prioritaire => attendre qu'il n’y ait plus aucune priorité globale
                    if (!isPriority)
                    {
                        var token = reporter?.CancellationToken ?? CancellationToken.None;
                        _priorityCoordinator.WaitIfPrioritiesExist(token);
                    }

                    try
                    {
                        ProcessFile(file, sourceDir, targetDir, job, encryptor);
                        filesCopied++;

                        if (isPriority)
                        {
                            _priorityCoordinator.MarkPriorityDone(job.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur sur {file.FullName}: {ex.Message}");

                        // ⚠️ idem : ne pas bloquer le système
                        if (isPriority)
                        {
                            _priorityCoordinator.MarkPriorityDone(job.Name);
                        }
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

                    reporter?.WaitIfPaused();
                }
            }
            finally
            {
                _priorityCoordinator.UnregisterJob(job.Name);
            }
        }

        // Méthode utilitaire pour éviter la duplication du code de reporting
        private void UpdateStateAndReport(StateEntry state, int totalFiles, int filesCopied, IProgressReporter reporter)
        {
            int pct = totalFiles > 0 ? (int)((filesCopied / (double)totalFiles) * 100) : 0;
            state.FilesRemaining = totalFiles - filesCopied;
            state.ProgressPct = pct;
            _stateWriter.WriteState(state);
            reporter?.ReportProgress(pct, state.FilesRemaining);
        }

        /*  private void ExecuteFullBackup(BackupJob job, StateEntry state, IProgressReporter reporter)
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
          }*/

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        private CryptoSoftEncryptorAdapter BuildEncryptor(BackupJob job)
        {
            if (job.EnableEncryption && !string.IsNullOrEmpty(job.EncryptionKey))
                return new CryptoSoftEncryptorAdapter(job.EncryptionKey, _logWriter, _settings);
            return null;
        }

        // Added helper to compute differential files
        private List<FileInfo> GetDifferentialFiles(DirectoryInfo sourceDir, DirectoryInfo targetDir)
        {
            var sourceFiles = sourceDir.GetFiles("*", SearchOption.AllDirectories);
            var filesToCopy = new List<FileInfo>();

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
                    filesToCopy.Add(sourceFile);
            }

            return filesToCopy;
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

            // ✅ userId pour logs centralisés
            var userId = $"{Environment.MachineName}\\{Environment.UserName}";

            if (!Directory.Exists(targetFileDir))
                Directory.CreateDirectory(targetFileDir!);

            File.Copy(file.FullName, targetPath, true);
            var copyTime = (DateTime.Now - startTime).TotalMilliseconds;

            long encryptTime = 0;
            if (encryptor != null && encryptor.ShouldEncrypt(targetPath))
            {
                encryptTime = encryptor.EncryptFile(targetPath, job.Name);
                if (encryptTime < 0)
                    Console.WriteLine($"Échec du chiffrement : {targetPath}");
            }

            _logWriter.WriteDailyLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = job.Name,
                SourcePathUNC = file.FullName,
                TargetPathUNC = targetPath,
                FileSizeBytes = file.Length,
                TransferTimeMs = (long)copyTime,
                EncryptionTimeMs = encryptTime,

                // ⚠️ IMPORTANT : cette propriété doit exister dans EasyLog.dll
                User = userId
            });
        }

        private bool IsPriorityFile(FileInfo file)
        {
            var ext = file.Extension?.ToLowerInvariant() ?? "";
            foreach (var p in _settings.PriorityExtensions)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                var pe = p.StartsWith(".") ? p.ToLowerInvariant() : "." + p.ToLowerInvariant();
                if (ext == pe) return true;
            }
            return false;
        }
    }
}