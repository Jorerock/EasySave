using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Infrastructure;
using EasySave.WPF.ViewModels;
using EasySave.WPF.Views;

using System;
using System.IO;
using System.Windows;

namespace EasySave.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Same initialization as Program.cs
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProSoft", "EasySave");
            Directory.CreateDirectory(baseDir);

            string configPath = Path.Combine(baseDir, "jobs.json");
            string logDir = Path.Combine(baseDir, "logs");
            string statePath = Path.Combine(baseDir, "state.json");

            // Initialize services (exactly like Program.cs)
            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);
            IJobRepository repo = new JsonJobRepository(configPath);
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter);

            // Create managers and orchestrator
            JobManager jobManager = new JobManager(repo);
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);

            // Localization (optional for WPF)
            //ILocalizationService localizationService = new LocalizationService();

            // Create ViewModel
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator);

            // Create and show Main Window
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            mainWindow.Show();
        }
    }
}