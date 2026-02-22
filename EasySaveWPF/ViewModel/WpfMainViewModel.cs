using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using EasySave.Core.Application;
using EasySave.Core.Domain;
using EasySave.Core.ViewModels;


namespace EasySave.WPF.ViewModels
{
    /// <summary>
    /// ViewModel WPF étendu avec exécution parallèle et contrôles Pause/Play/Stop.
    /// </summary>
    public class WpfMainViewModel : MainViewModel
    {
        private readonly ParallelBackupOrchestrator _parallelOrchestrator;

        // ── Liste des jobs enregistrés (édition) ──────────────────────
        public ObservableCollection<BackupJob> Jobs { get; private set; }

        // ── Liste des jobs EN COURS (monitoring live) ─────────────────
        public ObservableCollection<BackupJobViewModel> RunningJobs { get; private set; }

        // ── Job sélectionné dans la liste Jobs ────────────────────────
        private BackupJob _selectedJob;
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                OnPropertyChanged(nameof(SelectedJob));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ── Commandes ─────────────────────────────────────────────────
        public ICommand DeleteJobCommand { get; }
        public ICommand RunJobCommand { get; }
        public ICommand RunAllCommand { get; }

        // Commandes globales (agissent sur tous les RunningJobs)
        public ICommand PauseAllCommand { get; }
        public ICommand PlayAllCommand { get; }
        public ICommand StopAllCommand { get; }
        public ICommand ClearFinishedCommand { get; }

        // ══════════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ══════════════════════════════════════════════════════════════

        public WpfMainViewModel(
            JobManager jobManager,
            BackupOrchestrator orchestrator,
            SettingsManager settingsManager,
            ParallelBackupOrchestrator parallelOrchestrator)
            : base(jobManager, orchestrator, settingsManager)
        {
            _parallelOrchestrator = parallelOrchestrator ?? throw new ArgumentNullException(nameof(parallelOrchestrator));

            Jobs = new ObservableCollection<BackupJob>(ListJobs());
            RunningJobs = new ObservableCollection<BackupJobViewModel>();

            JobsChanged += OnJobsChangedHandler;

            // Commandes jobs
            DeleteJobCommand = new RelayCommand(_ => ExecuteDeleteJob(), _ => SelectedJob != null);
            RunJobCommand = new RelayCommand(_ => ExecuteRunJob(), _ => SelectedJob != null);
            RunAllCommand = new RelayCommand(_ => ExecuteRunAll(), _ => Jobs.Count > 0);

            // Commandes globales
            PauseAllCommand = new RelayCommand(_ => PauseAll());
            PlayAllCommand = new RelayCommand(_ => PlayAll());
            StopAllCommand = new RelayCommand(_ => StopAll());
            ClearFinishedCommand = new RelayCommand(_ => ClearFinished());
        }

        // ══════════════════════════════════════════════════════════════
        // EXÉCUTION PARALLÈLE
        // ══════════════════════════════════════════════════════════════

        private void ExecuteRunJob()
        {
            if (SelectedJob == null) return;

            var execution = _parallelOrchestrator.RunOne(SelectedJob);
            Application.Current.Dispatcher.Invoke(() => RunningJobs.Add(new BackupJobViewModel(execution)));
            StatusMessage = $"Démarrage de '{SelectedJob.Name}'...";
        }

        private void ExecuteRunAll()
        {
            var executions = _parallelOrchestrator.RunAll(Jobs);
            foreach (var execution in executions)
                RunningJobs.Add(new BackupJobViewModel(execution));
            StatusMessage = $"{executions.Count} job(s) lancés en parallèle.";
        }

        // ══════════════════════════════════════════════════════════════
        // CONTRÔLES GLOBAUX
        // ══════════════════════════════════════════════════════════════

        public void PauseAll()
        {
            foreach (var vm in RunningJobs)
                if (vm.IsRunning) vm.Pause();
        }

        public void PlayAll()
        {
            foreach (var vm in RunningJobs)
                if (vm.IsPaused) vm.Play();
        }

        public void StopAll()
        {
            foreach (var vm in RunningJobs)
                if (!vm.IsFinished) vm.Stop();
        }

        public void ClearFinished()
        {
            var finished = RunningJobs.Where(vm => vm.IsFinished).ToList();
            foreach (var vm in finished)
                RunningJobs.Remove(vm);
        }

        // ══════════════════════════════════════════════════════════════
        // GESTION JOBS (liste)
        // ══════════════════════════════════════════════════════════════

        public new void AddJob(BackupJob job)
        {
            base.AddJob(job);
            Application.Current.Dispatcher.Invoke(() => Jobs.Add(job));
        }

        public new void DeleteJob(int id)
        {
            var job = Jobs.FirstOrDefault(j => j.Id == id);
            base.DeleteJob(id);

            if (job != null)
                Application.Current.Dispatcher.Invoke(() => Jobs.Remove(job));

            if (SelectedJob?.Id == id)
                SelectedJob = null;
        }

        private void ExecuteDeleteJob()
        {
            if (SelectedJob != null) DeleteJob(SelectedJob.Id);
        }

        private void OnJobsChangedHandler(object sender, EventArgs e)
        {
            var newJobs = ListJobs();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Jobs.Clear();
                foreach (var job in newJobs)
                    Jobs.Add(job);
            });
        }
    }
}