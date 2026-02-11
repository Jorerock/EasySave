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
            string basePath = AppContext.BaseDirectory;
            string jobFilePath = Path.Combine(basePath, "jobs.json");

            // Initialization localizationService
            ILocalizationService localizationService = new LocalizationService();

            // Repository + JobManager
            IJobRepository repo = new JsonJobRepository(jobFilePath);
            JobManager jobManager = new JobManager(repo);

            // Writers EasyLog
            ILogWriter logWriter = new SimpleFileLogWriter("logs");
            IStateWriter stateWriter = new SimpleFileStateWriter("states");

            // FileSystemBackupEngine using EasyLog writers 
            IBackupEngine engine = new FileSystemBackupEngine(logWriter, stateWriter);

            // Orchestrator (using repo and engine)
            BackupOrchestrator orchestrator = new BackupOrchestrator(repo, engine);

            // ViewModel : JobManager + Orchestrator
            MainViewModel viewModel = new MainViewModel(jobManager, orchestrator);

            CommandParser commandParser = new CommandParser();


            //Command line

            if (args != null && args.Length > 0)
            {
                try
                {
                    // take id from the arguments Parser 
                    List<int> jobIds = commandParser.ParseJobSelection(args);

                    if (jobIds.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Erreur : Aucun job valide détecté dans les arguments.");
                        Console.WriteLine("Utilisation : EasySave.exe <job_ids>");
                        Console.WriteLine("  Exemples :");
                        Console.WriteLine("    EasySave.exe 1-3    (exécute les jobs 1, 2 et 3)");
                        Console.WriteLine("    EasySave.exe 1;3    (exécute les jobs 1 et 3)");
                        Console.WriteLine("    EasySave.exe 1,2,5  (exécute les jobs 1, 2 et 5)");
                        Console.ResetColor();
                        return;
                    }

                    Console.WriteLine($"Mode ligne de commande activé.");
                    Console.WriteLine($"Jobs à exécuter : {string.Join(", ", jobIds)}");
                    Console.WriteLine();

                    foreach (int jobId in jobIds)
                    {
                        Console.WriteLine($"Exécution du job {jobId}...");
                        viewModel.RunJob(jobId);

                        // result
                        if (!string.IsNullOrEmpty(viewModel.ErrorKey))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"  ✗ Erreur : {viewModel.ErrorKey}");
                            Console.ResetColor();
                        }
                        else if (!string.IsNullOrEmpty(viewModel.MessageKey))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  ✓ {viewModel.MessageKey}");
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("Toutes les sauvegardes ont été traitées.");
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    return;
                }
                catch (FormatException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Erreur de format : {ex.Message}");
                    Console.WriteLine("Utilisation : EasySave.exe <job_ids>");
                    Console.WriteLine("  Exemples :");
                    Console.WriteLine("    EasySave.exe 1-3");
                    Console.WriteLine("    EasySave.exe 1;3");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    return;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Erreur inattendue : {ex.Message}");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Appuyez sur une touche pour quitter...");
                    Console.ReadKey();
                    return;
                }
            }

            // console view
            ConsoleView consoleView = new ConsoleView(viewModel, commandParser, localizationService);
            consoleView.Start();

            Console.WriteLine("Application terminated. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
