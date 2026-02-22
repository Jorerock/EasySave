using EasyLog.Entries;
using EasySave.Core.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Application
{
    /// <summary>
    /// Orchestre l'exécution de plusieurs BackupJob en parallèle via des Tasks.
    /// Vit dans le Core : aucune dépendance WPF.
    /// La remontée de progression et le contrôle Pause/Stop passent par IProgressReporter.
    /// </summary>
    public class ParallelBackupOrchestrator
    {
        private readonly IBackupEngineFactory _engineFactory;

        public ParallelBackupOrchestrator(IBackupEngineFactory engineFactory)
        {
            _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
        }

        /// <summary>
        /// Lance tous les jobs en parallèle.
        /// Retourne une liste de JobExecution, chacune contenant les primitives
        /// de contrôle (Pause/Stop) et les callbacks de progression.
        /// </summary>
        public List<JobExecution> RunAll(IEnumerable<BackupJob> jobs)
        {
            var executions = new List<JobExecution>();

            foreach (var job in jobs)
            {
                var execution = CreateAndStart(job);
                executions.Add(execution);
            }

            return executions;
        }

        /// <summary>
        /// Lance un seul job dans une Task dédiée.
        /// </summary>
        public JobExecution RunOne(BackupJob job)
        {
            return CreateAndStart(job);
        }

        private JobExecution CreateAndStart(BackupJob job)
        {
            var execution = new JobExecution(job);
            var reporter = new JobExecutionProgressReporter(execution);
            var engine = _engineFactory.Create(reporter);

            execution.Task = Task.Run(
                () => RunSafe(engine, job, execution, reporter),
                execution.CancellationToken
            );

            return execution;
        }

        private void RunSafe(
            IBackupEngineWithProgress engine,
            BackupJob job,
            JobExecution execution,
            JobExecutionProgressReporter reporter)
        {
            try
            {
                execution.State = JobState.Active;
                engine.Run(job, reporter);

                execution.State = execution.CancellationToken.IsCancellationRequested
                    ? JobState.Stopped
                    : JobState.Completed;

                execution.RaiseProgressChanged(100, 0, execution.TotalFiles);
            }
            catch (OperationCanceledException)
            {
                execution.State = JobState.Stopped;
            }
            catch (Exception ex)
            {
                execution.State = JobState.Failed;
                execution.CurrentFile = $"Erreur : {ex.Message}";
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // JOBEXECUTION
    // Représente un job en cours : primitives de contrôle + état observable.
    // Le WPF s'y abonne via les events pour mettre à jour son ViewModel.
    // Aucune dépendance WPF : que du C# pur.
    // ══════════════════════════════════════════════════════════════════

    public class JobExecution
    {
        // ── Identité ──────────────────────────────────────────────────
        public BackupJob Job { get; }

        // ── Task associée ─────────────────────────────────────────────
        public Task Task { get; internal set; }

        // ── Primitives de contrôle ────────────────────────────────────
        internal readonly ManualResetEventSlim PauseEvent = new ManualResetEventSlim(true);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken CancellationToken => _cts.Token;

        // ── Compteurs (mis à jour par le reporter) ────────────────────
        public int TotalFiles { get; private set; }
        public int FilesRemaining { get; private set; }

        // ── Events : le WPF s'y abonne, le Core ne connaît pas WPF ───
        public event Action<JobState> StateChanged;
        public event Action<int, int, int> ProgressChanged;  // pct, filesRemaining, totalFiles
        public event Action<string> CurrentFileChanged;

        // ── État ──────────────────────────────────────────────────────
        private JobState _state = JobState.Idle;
        public JobState State
        {
            get => _state;
            internal set { _state = value; StateChanged?.Invoke(value); }
        }

        private int _progress;
        public int Progress => _progress;

        private string _currentFile = string.Empty;
        public string CurrentFile
        {
            get => _currentFile;
            internal set { _currentFile = value; CurrentFileChanged?.Invoke(value); }
        }

        internal void RaiseProgressChanged(int pct, int remaining, int total)
        {
            _progress = pct;
            FilesRemaining = remaining;
            TotalFiles = total > 0 ? total : TotalFiles;
            ProgressChanged?.Invoke(pct, remaining, TotalFiles);
        }

        public JobExecution(BackupJob job)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
        }

        // ── Contrôles publics ─────────────────────────────────────────

        /// <summary>Pause effective après le transfert du fichier en cours.</summary>
        public void Pause()
        {
            if (State != JobState.Active) return;
            PauseEvent.Reset();
            State = JobState.Paused;
        }

        /// <summary>Reprend un job en pause.</summary>
        public void Play()
        {
            if (State != JobState.Paused) return;
            PauseEvent.Set();
            State = JobState.Active;
        }

        /// <summary>Arrêt immédiat : annule la Task et débloque la pause si nécessaire.</summary>
        public void Stop()
        {
            _cts.Cancel();
            PauseEvent.Set(); // débloque si en pause pour que le thread puisse sortir
            // State sera mis à Stopped par RunSafe via OperationCanceledException
        }


    }

    // ══════════════════════════════════════════════════════════════════
    // JOBEXECUTIONPROGRESSREPORTER
    // Implémentation interne de IProgressReporter pour une JobExecution.
    // Fait le lien entre FileSystemBackupEngine et JobExecution.
    // Interne au Core : pas besoin de l'exposer au WPF.
    // ══════════════════════════════════════════════════════════════════

    internal class JobExecutionProgressReporter : IProgressReporter
    {
        private readonly JobExecution _execution;

        public JobExecutionProgressReporter(JobExecution execution)
        {
            _execution = execution;
        }

        public CancellationToken CancellationToken => _execution.CancellationToken;

        public void ReportFile(string sourceFilePath, int filesRemaining, int totalFiles)
        {
            _execution.CurrentFile = System.IO.Path.GetFileName(sourceFilePath);
            _execution.RaiseProgressChanged(_execution.Progress, filesRemaining, totalFiles);
        }

        public void ReportProgress(int progressPct, int filesRemaining)
        {
            _execution.RaiseProgressChanged(progressPct, filesRemaining, _execution.TotalFiles);
        }

        public void WaitIfPaused()
        {
            _execution.PauseEvent.Wait(_execution.CancellationToken);
        }
    }
}