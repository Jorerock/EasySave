using System;
using EasySave.View;
using EasySave.ViewModel;
using EasySave.Application;
using EasySave.Infrastructure;
using EasySave.Domain;
using EasyLog.Interfaces;
using EasyLog.Entries;

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialisation du service de localisation
            ILocalizationService localizationService = new LocalizationService();

            // Repository + JobManager
            IJobRepository repo = new JsonJobRepository("jobs.json");
            JobManager jobManager = new JobManager(repo);

            // Writers EasyLog (implémentations dans Infrastructure)
            ILogWriter logWriter = new SimpleFileLogWriter("logs");
            IStateWriter stateWriter = new SimpleFileStateWriter("states");

            // FileSystemBackupEngine utilisant EasyLog writers (une seule instance)
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter);

            // Orchestrator (dépend du repo et d'un engine)
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);

            // ViewModel attendu : JobManager + Orchestrator
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator);

            CommandParser commandParser = new CommandParser();

            // Vue console
            ConsoleView consoleView = new ConsoleView(viewModel, commandParser, localizationService);

            // Lancer l'application
            consoleView.Start();

            Console.WriteLine("Application terminated. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
