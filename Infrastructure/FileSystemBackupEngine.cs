using EasyLog.Entries;
using EasyLog.Interfaces;
using EasySave.Application;
using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EasySave.Infrastructure
{
    public sealed class FileSystemBackupEngine : IBackupEngine
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;

        public FileSystemBackupEngine(ILogWriter logWriter, IStateWriter stateWriter)
        {
            if (logWriter == null)
            {
                throw new ArgumentNullException(nameof(logWriter));
            }

            if (stateWriter == null)
            {
                throw new ArgumentNullException(nameof(stateWriter));
            }

            _logWriter = logWriter;
            _stateWriter = stateWriter;
        }

        public void Run(BackupJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            ValidateJob(job);

            string sourceRoot = job.SourceDirectory;
            string targetRoot = job.TargetDirectory;

            Directory.CreateDirectory(targetRoot);

            List<string> eligibleFiles = GetEligibleFiles(job, sourceRoot, targetRoot);

            int totalFiles = eligibleFiles.Count;
            long totalSizeBytes = ComputeTotalSizeBytes(eligibleFiles);

            int filesRemaining = totalFiles;
            long sizeRemainingBytes = totalSizeBytes;

            WriteState(
                backupName: job.Name,
                state: JobRunState.Active,
                totalFiles: totalFiles,
                totalSizeBytes: totalSizeBytes,
                filesRemaining: filesRemaining,
                sizeRemainingBytes: sizeRemainingBytes,
                progressPct: 0,
                currentSource: string.Empty,
                currentTarget: string.Empty
            );

            for (int i = 0; i < eligibleFiles.Count; i++)
            {
                string srcFile = eligibleFiles[i];
                string relative = Path.GetRelativePath(sourceRoot, srcFile);
                string dstFile = Path.Combine(targetRoot, relative);

                EnsureDirectoryExistsForFile(dstFile);

                FileInfo info = new FileInfo(srcFile);
                long fileSize = info.Length;

                // Update state before copy (current file)
                int progressBefore = ComputeProgressPct(doneBytes: totalSizeBytes - sizeRemainingBytes, totalBytes: totalSizeBytes);
                WriteState(
                    backupName: job.Name,
                    state: JobRunState.Active,
                    totalFiles: totalFiles,
                    totalSizeBytes: totalSizeBytes,
                    filesRemaining: filesRemaining,
                    sizeRemainingBytes: sizeRemainingBytes,
                    progressPct: progressBefore,
                    currentSource: ToFullPathOrUnc(srcFile),
                    currentTarget: ToFullPathOrUnc(dstFile)
                );

                long transferMs = CopyOneFileWithTiming(srcFile, dstFile);

                // Log for this file
                LogEntry logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    BackupName = job.Name,
                    SourcePathUNC = ToFullPathOrUnc(srcFile),
                    TargetPathUNC = ToFullPathOrUnc(dstFile),
                    FileSizeBytes = fileSize,
                    TransferTimeMs = transferMs
                };
                _logWriter.WriteDailyLog(logEntry);

                // Update remaining counters AFTER copy attempt
                filesRemaining = filesRemaining - 1;
                sizeRemainingBytes = sizeRemainingBytes - fileSize;
                if (sizeRemainingBytes < 0)
                {
                    sizeRemainingBytes = 0;
                }

                int progressAfter = ComputeProgressPct(doneBytes: totalSizeBytes - sizeRemainingBytes, totalBytes: totalSizeBytes);
                WriteState(
                    backupName: job.Name,
                    state: JobRunState.Active,
                    totalFiles: totalFiles,
                    totalSizeBytes: totalSizeBytes,
                    filesRemaining: filesRemaining,
                    sizeRemainingBytes: sizeRemainingBytes,
                    progressPct: progressAfter,
                    currentSource: ToFullPathOrUnc(srcFile),
                    currentTarget: ToFullPathOrUnc(dstFile)
                );
            }

            WriteState(
                backupName: job.Name,
                state: JobRunState.Completed,
                totalFiles: totalFiles,
                totalSizeBytes: totalSizeBytes,
                filesRemaining: 0,
                sizeRemainingBytes: 0,
                progressPct: 100,
                currentSource: string.Empty,
                currentTarget: string.Empty
            );
        }

        // -------------------------------------------------------
        // Méthodes internes demandées par UML
        // -------------------------------------------------------

        private void CopyFull(string src, string dst)
        {
            // Ici on s'appuie sur Run() pour log + state en temps réel.
            // On garde la méthode pour le diagramme et une extension future.
            Directory.CreateDirectory(dst);
        }

        private void CopyDifferential(string src, string dst)
        {
            // Ici on s'appuie sur Run() pour log + state en temps réel.
            Directory.CreateDirectory(dst);
        }

        private bool ShouldCopy(string srcFile, string dstFile)
        {
            if (!File.Exists(dstFile))
            {
                return true;
            }

            DateTime srcTimeUtc = File.GetLastWriteTimeUtc(srcFile);
            DateTime dstTimeUtc = File.GetLastWriteTimeUtc(dstFile);

            return srcTimeUtc > dstTimeUtc;
        }

        private long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
            {
                return 0L;
            }

            long total = 0L;

            IEnumerable<string> files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                total += info.Length;
            }

            return total;
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private void ValidateJob(BackupJob job)
        {
            if (string.IsNullOrWhiteSpace(job.Name))
            {
                throw new InvalidOperationException("BackupJob.Name is required.");
            }

            if (string.IsNullOrWhiteSpace(job.SourceDirectory))
            {
                throw new InvalidOperationException("BackupJob.SourceDirectory is required.");
            }

            if (string.IsNullOrWhiteSpace(job.TargetDirectory))
            {
                throw new InvalidOperationException("BackupJob.TargetDirectory is required.");
            }

            if (!Directory.Exists(job.SourceDirectory))
            {
                throw new DirectoryNotFoundException("Source directory not found: " + job.SourceDirectory);
            }
        }

        private List<string> GetEligibleFiles(BackupJob job, string sourceRoot, string targetRoot)
        {
            List<string> allFiles = new List<string>(Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories));
            List<string> eligible = new List<string>();

            for (int i = 0; i < allFiles.Count; i++)
            {
                string srcFile = allFiles[i];

                string relative = Path.GetRelativePath(sourceRoot, srcFile);
                string dstFile = Path.Combine(targetRoot, relative);

                bool include;
                if (job.Type == BackupType.Full)
                {
                    include = true;
                }
                else
                {
                    include = ShouldCopy(srcFile, dstFile);
                }

                if (include)
                {
                    eligible.Add(srcFile);
                }
            }

            return eligible;
        }

        private long ComputeTotalSizeBytes(List<string> files)
        {
            long total = 0L;

            for (int i = 0; i < files.Count; i++)
            {
                FileInfo info = new FileInfo(files[i]);
                total += info.Length;
            }

            return total;
        }

        private void EnsureDirectoryExistsForFile(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            Directory.CreateDirectory(dir);
        }

        private long CopyOneFileWithTiming(string srcFile, string dstFile)
        {
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                File.Copy(srcFile, dstFile, true);
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch
            {
                sw.Stop();
                long ms = sw.ElapsedMilliseconds;
                if (ms <= 0)
                {
                    ms = 1;
                }

                // négatif si erreur (spécification)
                return -ms;
            }
        }

        private void WriteState(
            string backupName,
            JobRunState state,
            int totalFiles,
            long totalSizeBytes,
            int filesRemaining,
            long sizeRemainingBytes,
            int progressPct,
            string currentSource,
            string currentTarget)
        {
            StateEntry entry = new StateEntry
            {
                Timestamp = DateTime.UtcNow,
                BackupName = backupName,
                State = state,
                TotalFiles = totalFiles,
                TotalSizeBytes = totalSizeBytes,
                FilesRemaining = filesRemaining,
                SizeRemainingBytes = sizeRemainingBytes,
                ProgressPct = progressPct,
                CurrentSourceUNC = currentSource,
                CurrentTargetUNC = currentTarget
            };

            _stateWriter.WriteState(entry);
        }

        private int ComputeProgressPct(long doneBytes, long totalBytes)
        {
            if (totalBytes <= 0L)
            {
                return 100;
            }

            double ratio = (double)doneBytes / (double)totalBytes;
            int pct = (int)Math.Round(ratio * 100.0, MidpointRounding.AwayFromZero);

            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;

            return pct;
        }

        private string ToFullPathOrUnc(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            // If already UNC path, keep it
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return path;
            }

            return Path.GetFullPath(path);
        }
    }
}
