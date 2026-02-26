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

            string jobsPath = Path.Combine(baseDir, "jobs.json");
            string settingsPath = Path.Combine(baseDir, "settings.json");
            string logDir = Path.Combine(baseDir, "logs");
            string statePath = Path.Combine(baseDir, "state.json");

            Directory.CreateDirectory(logDir);

            // Repositories
            IJobRepository jobRepository = new JsonJobRepository(jobsPath);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(settingsPath);

            // Managers + Settings (IMPORTANT)
            SettingsManager settingsManager = new SettingsManager(settingsRepository);
            AppSettings appSettings = settingsManager.Get();

            JobManager jobManager = new JobManager(jobRepository);

            // Coordinator priorités
            PriorityTransferCoordinator priorityCoordinator = new PriorityTransferCoordinator();

            // Detector
            IBusinessSoftwareDetector detector = new ProcessBusinessSoftwareDetector(appSettings);

            //
            long maxParallelSizeKo = appSettings.MaxParallelSizeKo;
           


            // Writers
            ILogWriter logWriter = LogWriterFactory.Create(appSettings, logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);

            // Engine + orchestrator "legacy" (pour base MainViewModel)
            IBackupEngine engine = new FileSystemBackupEngine(
                logWriter,
                stateWriter,
                appSettings,
                detector,
                priorityCoordinator
            );

            BackupOrchestrator orchestrator = new BackupOrchestrator(jobRepository, engine);

            // Parallèle v3
            var engineFactory = new FileSystemBackupEngineFactory(
                logWriter,
                stateWriter,
                appSettings,
                detector,
                priorityCoordinator
            );

            var parallelOrchestrator = new ParallelBackupOrchestrator(engineFactory);

            // Langue
            if (appSettings.Language == AppLanguage.Francais)
                LocalizationManager.SetCulture("fr-FR");
            else
                LocalizationManager.SetCulture("en-US");

            // VM
            WpfMainViewModel viewModel = new WpfMainViewModel(
                jobManager,
                orchestrator,
                settingsManager,
                parallelOrchestrator
            );

            // UI
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = viewModel;
            mainWindow.Show();
        }
    }
}