using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using EasySave.Core.Application;
using EasySave.Core.Domain;

namespace EasySave.WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly JobManager _jobManager;
        private readonly BackupOrchestrator _orchestrator;

        // Observable collection for WPF binding
        public ObservableCollection<BackupJob> Jobs { get; set; }

        // Selected job
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

        // Status message
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        // Commands
        public ICommand CreateJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand RunJobCommand { get; }
        public ICommand RunAllCommand { get; }

        public MainViewModel(JobManager jobManager, BackupOrchestrator orchestrator)
        {
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));

            // Load jobs
            Jobs = new ObservableCollection<BackupJob>(_jobManager.GetAll());

            // Initialize commands
            CreateJobCommand = new RelayCommand(_ => ExecuteCreateJob());
            DeleteJobCommand = new RelayCommand(_ => ExecuteDeleteJob(), _ => CanExecuteDeleteJob());
            RunJobCommand = new RelayCommand(_ => ExecuteRunJob(), _ => CanExecuteRunJob());
            RunAllCommand = new RelayCommand(_ => ExecuteRunAll(), _ => CanExecuteRunAll());
        }

        // Command implementations
        private void ExecuteCreateJob()
        {
            // Will be handled by View (opens CreateJobWindow)
        }

        public void AddJob(BackupJob job)
        {
            _jobManager.Add(job);
            Jobs.Add(job);
            StatusMessage = $"Job '{job.Name}' created successfully";
        }

        private void ExecuteDeleteJob()
        {
            if (SelectedJob == null) return;

            _jobManager.Remove(SelectedJob.Id);
            Jobs.Remove(SelectedJob);
            StatusMessage = $"Job '{SelectedJob.Name}' deleted";
            SelectedJob = null;
        }

        private bool CanExecuteDeleteJob() => SelectedJob != null;

        private void ExecuteRunJob()
        {
            if (SelectedJob == null) return;

            try
            {
                StatusMessage = $"Running backup '{SelectedJob.Name}'...";
                _orchestrator.RunOne(SelectedJob.Id);
                StatusMessage = $"Backup '{SelectedJob.Name}' completed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private bool CanExecuteRunJob() => SelectedJob != null;

        private void ExecuteRunAll()
        {
            try
            {
                StatusMessage = "Running all backups...";
                _orchestrator.RunAllSequential();
                StatusMessage = $"All {Jobs.Count} backups completed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private bool CanExecuteRunAll() => Jobs.Count > 0;

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}