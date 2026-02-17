using System;
using System.Collections.Generic;
using System.IO;
using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Application;
using EasySave.Core.Infrastructure;
using EasySave.View;
using EasySave.ViewModel;

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

            ILogWriter logWriter = new JsonLogWriter(logDir);
            IStateWriter stateWriter = new JsonStateWriter(statePath);

            IJobRepository repo = new JsonJobRepository(configPath);
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter);

            JobManager jobManager = new JobManager(repo);
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator);

            // Mode CLI : EasySave.exe 1-3 / 1;3
            if (args != null && args.Length > 0)
            {
                CommandParser parser = new CommandParser(1, int.MaxValue);
                List<int> ids = parser.ParseJobSelection(args);

                if (ids.Count > 0)
                {
                    // Correction : itérer et appeler RunJob pour chaque id (RunJob attend un int)
                    foreach (int id in ids)
                    {
                        viewModel.RunJob(id);
                    }
                }
                return;
            }

            ILocalizationService localizationService = new LocalizationService();
            ConsoleView consoleView = new ConsoleView(viewModel, localizationService);
            consoleView.Start();
        }
    }
}
