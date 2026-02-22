using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Domain;
using EasySave.Core.Infrastructure;
using EasySave.WPF.Localization;

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

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProSoft", "EasySave");
            Directory.CreateDirectory(baseDir);

            string configPath = Path.Combine(baseDir, "jobs.json");
            string settingsPath = Path.Combine(baseDir, "settings.json");
            string logDir = Path.Combine(baseDir, "logs");
            string statePath = Path.Combine(baseDir, "state.json");

            // Repositories
            IJobRepository jobRepository = new JsonJobRepository(configPath);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(settingsPath);

            // Writers
            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);

            // Managers
            JobManager jobManager = new JobManager(jobRepository);
            SettingsManager settingsManager = new SettingsManager(settingsRepository);
            AppSettings appSettings = settingsManager.Get();

            // Detector
            IBusinessSoftwareDetector detector = new ProcessBusinessSoftwareDetector();

            // Engine (CLI / séquentiel)
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter, appSettings, detector);

            // Orchestrateur séquentiel (héritage CLI)
            BackupOrchestrator orchestrator = new BackupOrchestrator(jobRepository, engine);

            // ── NOUVEAU : Orchestrateur parallèle ──────────────────────────
            // Factory qui crée un moteur par job (chaque job a son propre reporter)
            var engineFactory = new FileSystemBackupEngineFactory(logWriter, stateWriter, appSettings, detector);
            var parallelOrchestrator = new ParallelBackupOrchestrator(engineFactory);

            // Langue
            if (appSettings.Language == AppLanguage.Francais)
                LocalizationManager.SetCulture("fr-FR");
            else
                LocalizationManager.SetCulture("en-US");

            // ViewModel
            WpfMainViewModel viewModel = new WpfMainViewModel(
                jobManager,
                orchestrator,
                settingsManager,
                parallelOrchestrator
            );

            // MainWindow
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = viewModel;
            mainWindow.Show();
        }
    }
}