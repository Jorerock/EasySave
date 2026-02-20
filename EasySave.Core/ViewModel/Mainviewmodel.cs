using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EasySave.Core.Application;
using EasySave.Core.Domain;

namespace EasySave.Core.ViewModels
{
    /// <summary>
    /// ViewModel unifié pour CLI et WPF (sans dépendances WPF)
    /// </summary>
    public class MainViewModel : IMainViewModel, INotifyPropertyChanged
    {
        private readonly JobManager _jobManager;
        private readonly BackupOrchestrator _orchestrator;
        private readonly SettingsManager _settingsManager;

        // ══════════════════════════════════════════════════════════════════
        // PROPRIÉTÉS COMMUNES (CLI + WPF)
        // ══════════════════════════════════════════════════════════════════

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

        public string MessageKey { get; private set; }
        public string ErrorKey { get; private set; }

        // ══════════════════════════════════════════════════════════════════
        // CONSTRUCTEUR
        // ══════════════════════════════════════════════════════════════════

        public MainViewModel(JobManager jobManager, BackupOrchestrator orchestrator, SettingsManager settingsManager)
        {
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        // ══════════════════════════════════════════════════════════════════
        // INTERFACE IMainViewModel - Méthodes communes
        // ══════════════════════════════════════════════════════════════════

        public List<BackupJob> ListJobs()
        {
            return _jobManager.GetAll();
        }

        public void CreateJob(string name, string src, string dst, BackupType type, bool enableEncrypt, string encryptionKey, List<string> extensions)
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst))
            {
                ErrorKey = "error_invalid_path";
                StatusMessage = "Invalid path";
                return;
            }

            try
            {
                BackupJob job = new BackupJob
                {
                    Name = name,
                    SourceDirectory = src,
                    TargetDirectory = dst,
                    EnableEncryption = enableEncrypt,
                    Type = type,
                    EncryptionKey = encryptionKey,
                    ExtensionsToEncrypt = extensions
                };

                _jobManager.Add(job);

                MessageKey = "success_job_created";
                StatusMessage = $"Job '{name}' created successfully";

                // Notifie les observateurs (pour WPF)
                OnJobsChanged();
            }
            catch (Exception ex)
            {
                ErrorKey = "error_invalid_input";
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public void AddJob(BackupJob job)
        {
            _jobManager.Add(job);
            StatusMessage = $"Job '{job.Name}' created successfully";
            OnJobsChanged();
        }

        public void DeleteJob(int id)
        {
            ClearMessages();

            try
            {
                _jobManager.Remove(id);

                MessageKey = "success_job_deleted";
                StatusMessage = "Job deleted";

                OnJobsChanged();
            }
            catch
            {
                ErrorKey = "error_job_not_found";
                StatusMessage = "Job not found";
            }
        }

        public void RunJob(int id)
        {
            ClearMessages();

            try
            {
                StatusMessage = "Running backup...";
                _orchestrator.RunOne(id);

                MessageKey = "success_job_executed";
                StatusMessage = "Backup completed";
            }
            catch (Exception ex)
            {
                ErrorKey = "error_job_not_found";
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public void RunAll()
        {
            ClearMessages();

            try
            {
                StatusMessage = "Running all backups...";
                _orchestrator.RunAllSequential();

                MessageKey = "success_job_executed";
                StatusMessage = "All backups completed";
            }
            catch (Exception ex)
            {
                ErrorKey = "error_invalid_input";
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public AppSettings GetCurrentSettings()
        {
            return _settingsManager.Get();
        }

        // Dans MainViewModel.cs - Méthode ApplySettings

        public void ApplySettings(AppSettings newSettings)
        {
            if (newSettings == null) throw new ArgumentNullException(nameof(newSettings));

            _settingsManager.ApplySettings(newSettings);

            // ✅ OPTION 1 : Si BackupOrchestrator a UpdateSettings()
            //_orchestrator.UpdateSettings();

            MessageKey = "success_settings_updated";
            StatusMessage = "Settings updated successfully";

            OnPropertyChanged(nameof(GetCurrentSettings));
        }

        // ══════════════════════════════════════════════════════════════════
        // ÉVÉNEMENTS POUR WPF
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Événement déclenché quand la liste des jobs change
        /// WPF peut s'y abonner pour rafraîchir l'UI
        /// </summary>
        public event EventHandler JobsChanged;

        protected virtual void OnJobsChanged()
        {
            JobsChanged?.Invoke(this, EventArgs.Empty);
        }

        // ══════════════════════════════════════════════════════════════════
        // INotifyPropertyChanged (pour WPF)
        // ══════════════════════════════════════════════════════════════════

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        private void ClearMessages()
        {
            MessageKey = null;
            ErrorKey = null;
        }
    }
}