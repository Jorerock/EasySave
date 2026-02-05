using System;
using EasySave.View;
using EasySave.ViewModel;
using EasySave.Application;
using EasySave.Infrastructure;
using EasySave.Domain;

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

            // Engine de secours pour tester sans easylog.dll
            IBackupEngine engine = new SimpleConsoleBackupEngine();

            // Orchestrator (dépend du repo et d'un engine)
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);

            // ViewModel attendu : JobManager + Orchestrator
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator);

            // Vue console
            ConsoleView consoleView = new ConsoleView(viewModel, localizationService);

            // Lancer l'application
            consoleView.Start();

            Console.WriteLine("Application terminated. Press any key to exit...");
            Console.ReadKey();
        }

        // Implémentation minimale d'IBackupEngine pour tester sans EasyLog.
        private sealed class SimpleConsoleBackupEngine : IBackupEngine
        {
            public void Run(BackupJob job)
            {
                if (job == null) throw new ArgumentNullException(nameof(job));
                Console.WriteLine($"[Engine] Backup start '{job.Name}' from '{job.SourceDirectory}' to '{job.TargetDirectory}' ({job.Type})");
                System.Threading.Thread.Sleep(150);
                Console.WriteLine($"[Engine] Backup finished '{job.Name}'");
            }
        }
    }
}
