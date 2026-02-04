using System;
using System.Collections.Generic;
using System.Text;
using EasySave.ViewModel;

namespace EasySave.View
{
    internal class ConsoleView
    {
        private readonly MainViewModel _vm;
        private readonly Commandparser _parser;
        private readonly ILocalizationService _i18n;


        public ConsoleView(MainViewModel viewModel, ILocalizationService i18n)
        {
            _vm = viewModel;
            _i18n = i18n;
        }


        public void Start()
        {
            bool running = true;
            while (running)
            {
                ShowMenu();
                string choice = GetUserInput();

                switch (choice)
                {
                    case "1": CreateJobFlow(); break;
                    case "2": DeleteJobFlow(); break;
                    case "3": ExecuteOneFlow(); break;
                    case "4": ExecuteAllFlow(); break;
                    case "5": DisplayJobList(); break;
                    case "6": ChangeLanguageFlow(); break;
                    case "0": running = false; break;
                    default: ShowError(Lang("error_invalid_input")); break;
                }
            }
        }

        public void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine(Lang("menu_title"));
            Console.WriteLine(Lang("menu_create"));
            Console.WriteLine(Lang("menu_delete"));
            Console.WriteLine(Lang("menu_execute_one"));
            Console.WriteLine(Lang("menu_execute_all"));
            Console.WriteLine(Lang("menu_display"));
            Console.WriteLine(Lang("menu_language"));
            Console.WriteLine(Lang("menu_quit"));
            Console.WriteLine();
        }

        // ── Flux utilisateur ──────────────────────────
        // Chaque flow : saisit les données, appelle une commande du ViewModel,
        // puis affiche le résultat via Message / ErrorMessage.


        private void CreateJobFlow()
        {
            Console.Write(Lang("job_name"));
            string name = GetUserInput();

            Console.Write(Lang("job_source"));
            string source = GetUserInput();

            Console.Write(Lang("job_target"));
            string target = GetUserInput();

            Console.Write(Lang("job_type"));
            string typeInput = GetUserInput();
            BackupType type = typeInput == "2" ? BackupType.Differential : BackupType.Full;

            // ── Seule interaction avec le ViewModel ───
            _viewModel.CreateJob(name, source, target, type);
            ShowMessages();
            Console.ReadKey();
        }

        private void DeleteJobFlow()
        {
            DisplayJobList();
            Console.Write(Lang("job_id_delete"));

            if (int.TryParse(GetUserInput(), out int id))
                _viewModel.DeleteJob(id);
            else
                ShowError(Lang("error_invalid_input"));

            ShowMessages();
            Console.ReadKey();
        }

        private void ExecuteOneFlow()
        {
            DisplayJobList();
            Console.Write(Lang("job_id_execute"));

            if (int.TryParse(GetUserInput(), out int id))
            {
                Console.WriteLine(Lang("copying"));
                _viewModel.ExecuteJob(id);
            }
            else
            {
                ShowError(Lang("error_invalid_input"));
            }

            ShowMessages();
            Console.ReadKey();
        }

        private void ExecuteAllFlow()
        {
            Console.WriteLine(Lang("copying"));
            _viewModel.ExecuteAllJobs();
            ShowMessages();
            Console.ReadKey();
        }

        private void ChangeLanguageFlow()
        {
            Console.Write(Lang("lang_choice"));
            string lang = GetUserInput();
            _viewModel.ChangeLanguage(lang);
            ShowMessages();
            Console.ReadKey();
        }

        // ── Affichage de données (lit ViewModel.Jobs) ─

        /// <summary>
        /// Affiche la liste des jobs sous forme de tableau.
        /// Les données viennent uniquement de _viewModel.Jobs.
        /// </summary>
        public void DisplayJobList()
        {
            if (_viewModel.HasNoJobs)
            {
                Console.WriteLine(Lang("job_no_jobs"));
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"Source",-30} {"Target",-30} {"Type",-15} {"Last Backup",-20}");
            Console.WriteLine(new string('-', 122));

            foreach (var job in _viewModel.Jobs)
            {
                string typeName = job.Type == BackupType.Full ? Lang("backup_full") : Lang("backup_diff");
                Console.WriteLine($"{job.Id,-5} {job.Name,-20} {job.SourceDirectory,-30} {job.TargetDirectory,-30} {typeName,-15} {job.LastBackupDate,-20}");
            }
            Console.WriteLine();
        }

        // ── I/O de base ───────────────────────────────

        public string GetUserInput()
        {
            return Console.ReadLine() ?? string.Empty;
        }

        public void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Affiche le Message ou l'ErrorMessage du ViewModel après une commande.
        /// </summary>
        private void ShowMessages()
        {
            if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
                ShowError(_viewModel.ErrorMessage);

            if (!string.IsNullOrEmpty(_viewModel.Message))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_viewModel.Message);
                Console.ResetColor();
            }
        }

        // ── Helper ────────────────────────────────────
        /// Récupère le texte localisé pour une clé donnée.

        private string Lang(string key) => _i18n.GetText(key);
    }
}
