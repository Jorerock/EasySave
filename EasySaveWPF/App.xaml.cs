using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Domain;
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


            // Path Configuration

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProSoft", "EasySave");
            Directory.CreateDirectory(baseDir);

            string configPath = Path.Combine(baseDir, "jobs.json");
            string settingsPath = Path.Combine(baseDir, "settings.json");  
            string logDir = Path.Combine(baseDir, "logs");
            string statePath = Path.Combine(baseDir, "state.json");

   
            // Building Repositories

            IJobRepository jobRepository = new JsonJobRepository(configPath);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(settingsPath);

            // Building Log & State Writers
  
            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);


            // managers & orchestrator

            JobManager jobManager = new JobManager(jobRepository);
            SettingsManager settingsManager = new SettingsManager(settingsRepository);
            AppSettings appSettings = settingsManager.Get();
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter, appSettings);

            BackupOrchestrator orchestrator = new BackupOrchestrator(jobRepository, engine);

    
            // ViewModel & View
 
            MainViewModel viewModel = new MainViewModel(
                jobManager, 
                orchestrator,
                settingsManager  
            );

            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };

            mainWindow.Show();
        }
    }
}