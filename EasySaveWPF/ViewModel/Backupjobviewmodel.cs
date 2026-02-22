using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySave.Core.Application;
using EasySave.Core.Domain;

namespace EasySave.WPF.ViewModels
{
    /// <summary>
    /// ViewModel dédié à un BackupJob en cours d'exécution.
    /// Expose la progression, l'état et les commandes Pause/Play/Stop.
    /// </summary>
    public class BackupJobViewModel : INotifyPropertyChanged
    {
        private readonly JobExecution _execution;

        public BackupJobViewModel(JobExecution execution)
        {
            _execution = execution;

            // S'abonner aux events du Core
            _execution.StateChanged += OnStateChanged;
            _execution.ProgressChanged += OnProgressChanged;
            _execution.CurrentFileChanged += OnCurrentFileChanged;

            // Déléguer les commandes à JobExecution
            PauseCommand = new RelayCommand(_ => _execution.Pause(), _ => _execution.State == JobState.Active);
            PlayCommand = new RelayCommand(_ => _execution.Play(), _ => _execution.State == JobState.Paused);
            StopCommand = new RelayCommand(_ => _execution.Stop(), _ => !IsFinished);
        }

        // Les callbacks marshallent sur le thread UI
        private void OnStateChanged(JobState state)
            => Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(StateLabel));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsFinished));
                CommandManager.InvalidateRequerySuggested();
            });

        private void OnProgressChanged(int pct, int remaining, int total)
            => Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(FilesRemaining));
                OnPropertyChanged(nameof(TotalFiles));
            });

        private void OnCurrentFileChanged(string file)
            => Application.Current.Dispatcher.Invoke(() =>
                OnPropertyChanged(nameof(CurrentFile)));

        // Propriétés bindables — lisent directement depuis JobExecution
        public BackupJob Job => _execution.Job;
        public JobState State => _execution.State;
        public int Progress => _execution.Progress;
        public int FilesRemaining => _execution.FilesRemaining;
        public int TotalFiles => _execution.TotalFiles;
        public string CurrentFile => _execution.CurrentFile;
        public bool IsRunning => State == JobState.Active;
        public bool IsPaused => State == JobState.Paused;
        public bool IsFinished => State is JobState.Completed
                                                  or JobState.Failed
                                                  or JobState.Stopped;
        public void Pause() => _execution.Pause();
        public void Play() => _execution.Play();
        public void Stop() => _execution.Stop();

        public string StateLabel => State switch
        {
            JobState.Idle => "En attente",
            JobState.Active => "En cours",
            JobState.Paused => "En pause",
            JobState.Completed => "Terminé ✔",
            JobState.Failed => "Erreur ✘",
            JobState.Stopped => "Arrêté",
            JobState.BlockedByBusinessSoftware => "Bloqué",
            _ => State.ToString()
        };

        public ICommand PauseCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Extension de l'enum existant — à ajouter dans EasySave.Core.Domain si absent
    public static class JobStateExtensions { }
}