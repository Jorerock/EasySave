using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Domain;
using EasySave.Core.Infrastructure;
using EasySave.View;
using EasySave.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace EasySave
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProSoft", "EasySave");
            Directory.CreateDirectory(baseDir);

            string configPath = Path.Combine(baseDir, "jobs.json");
            string logDir = Path.Combine(baseDir, "logs");
            string statePath = Path.Combine(baseDir, "state.json");
            string settingsPath = Path.Combine(baseDir, "settings.json");

            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);

            IJobRepository repo = new JsonJobRepository(configPath);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(settingsPath);

            SettingsManager settingsManager = new SettingsManager(settingsRepository);
            AppSettings appSettings = settingsManager.Get();

            // Instantiate detector required by FileSystemBackupEngine
            IBusinessSoftwareDetector detector = new ProcessBusinessSoftwareDetector();
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter, appSettings, detector);


            JobManager jobManager = new JobManager(repo);
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator, settingsManager);

            // Mode CLI : EasySave.exe 1-3 / 1;3
            if (args != null && args.Length > 0)
            {
                CommandParser parser = new CommandParser(1, int.MaxValue);
                List<int> ids = parser.ParseJobSelection(args);

                if (ids.Count > 0)
                {
                    foreach (int id in ids)
                    {
                        viewModel.RunJob(id);
                    }
                }
                return;
            }

            // ✅ LOCALISATION : appliquer la langue depuis settings.json
            ILocalizationService localizationService = new LocalizationService();
            if (appSettings.Language == AppLanguage.Anglais)
            {
                localizationService.CurrentLanguage = "en";
            }
            else
            {
                localizationService.CurrentLanguage = "fr";
            }

            ConsoleView consoleView = new ConsoleView(viewModel, localizationService);
            consoleView.Start();
        }
    }
}
