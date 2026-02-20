using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EasySave.Core.Domain;
using EasySave.Core.ViewModels;
using EasySave.Core.Application;

namespace EasySave.WPF.ViewModels
{
    /// <summary>
    /// ViewModel WPF qui étend MainViewModel avec ObservableCollection et Commands
    /// </summary>
    public class WpfMainViewModel : MainViewModel
    {
        // ══════════════════════════════════════════════════════════════════
        // PROPRIÉTÉS WPF (ObservableCollection + SelectedJob)
        // ══════════════════════════════════════════════════════════════════


        //Mise a jour de l'ui en auto
        public ObservableCollection<BackupJob> Jobs { get; private set; }

        private BackupJob _selectedJob;
        public BackupJob SelectedJob
        {
            get => _selectedJob;
            set
            {
                _selectedJob = value;
                //Notifie WPF:
                OnPropertyChanged(nameof(SelectedJob));
                // Active/désactive 
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // COMMANDES WPF
        // ══════════════════════════════════════════════════════════════════

        public ICommand DeleteJobCommand { get; }
        public ICommand RunJobCommand { get; }
        public ICommand RunAllCommand { get; }

        // ══════════════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ══════════════════════════════════════════════════════════════════

        public WpfMainViewModel(JobManager jobManager, BackupOrchestrator orchestrator, SettingsManager settingsManager)
            : base(jobManager, orchestrator, settingsManager)
        {
            // Charge les jobs dans l'ObservableCollection
            Jobs = new ObservableCollection<BackupJob>(ListJobs());

            // S'abonne aux changements de jobs pour synchroniser l'ObservableCollection
            JobsChanged += OnJobsChangedHandler;

            // Initialise les commandes WPF
            DeleteJobCommand = new RelayCommand(_ => ExecuteDeleteJob(), _ => CanExecuteDeleteJob());
            RunJobCommand = new RelayCommand(_ => ExecuteRunJob(), _ => CanExecuteRunJob());
            RunAllCommand = new RelayCommand(_ => ExecuteRunAll(), _ => CanExecuteRunAll());
        }

        // ══════════════════════════════════════════════════════════════════
        // SYNCHRONISATION OBSERVABLECOLLECTION
        // ══════════════════════════════════════════════════════════════════

        private void OnJobsChangedHandler(object sender, EventArgs e)
        {
            // Recharge la liste depuis le Core
            var newJobs = ListJobs();

            // Synchronise l'ObservableCollection
            Jobs.Clear();
            foreach (var job in newJobs)
            {
                Jobs.Add(job);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // SURCHARGES POUR SYNCHRONISER OBSERVABLECOLLECTION
        // ══════════════════════════════════════════════════════════════════

        public new void AddJob(BackupJob job)
        {
            base.AddJob(job);
            Jobs.Add(job);
        }

        public new void DeleteJob(int id)
        {
            var job = Jobs.FirstOrDefault(j => j.Id == id);
            base.DeleteJob(id);

            if (job != null)
            {
                Jobs.Remove(job);
            }

            if (SelectedJob?.Id == id)
            {
                SelectedJob = null;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // IMPLÉMENTATION DES COMMANDES
        // ══════════════════════════════════════════════════════════════════

        private void ExecuteDeleteJob()
        {
            if (SelectedJob == null) return;
            DeleteJob(SelectedJob.Id);
        }

        private bool CanExecuteDeleteJob() => SelectedJob != null;

        private void ExecuteRunJob()
        {
            if (SelectedJob == null) return;
            RunJob(SelectedJob.Id);
        }

        private bool CanExecuteRunJob() => SelectedJob != null;

        private void ExecuteRunAll()
        {
            RunAll();
        }

        private bool CanExecuteRunAll() => Jobs.Count > 0;
    }
}