using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Domain;
using EasySave.Core.Infrastructure;
using EasySave.Core.ViewModels;
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

            IJobRepository jobRepository = new JsonJobRepository(configPath);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(settingsPath);

            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);

            JobManager jobManager = new JobManager(jobRepository);
            SettingsManager settingsManager = new SettingsManager(settingsRepository);
            AppSettings appSettings = settingsManager.Get();

            // ✅ Appliquer la langue WPF au démarrage
            if (appSettings.Language == AppLanguage.Francais)
            {
                LocalizationManager.SetCulture("fr-FR");
            }
            else
            {
                LocalizationManager.SetCulture("en-US");
            }

            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter, appSettings);
            BackupOrchestrator orchestrator = new BackupOrchestrator(jobRepository, engine);

            WpfMainViewModel viewModel = new WpfMainViewModel(jobManager, orchestrator,settingsManager);

            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = viewModel;
            mainWindow.Show();
        }
    }
}
