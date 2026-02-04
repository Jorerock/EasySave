using System;
using System.Collections.Generic;
using System.Text;
using EasySave.ViewModel;
using EasySave.Domain;

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
        // Eqch flow : input first then call the ViewModel Command,
        // then show of the result/ error message.


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

            _vm.CreateJob(name, source, target, type);
            ShowMessages();
            Console.ReadKey();
        }

        private void DeleteJobFlow()
        {
            DisplayJobList();
            Console.Write(Lang("job_id_delete"));

            if (int.TryParse(GetUserInput(), out int id))
                _vm.DeleteJob(id);
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
                _vm.ExecuteJob(id);
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
            _vm.ExecuteAllJobs();
            ShowMessages();
            Console.ReadKey();
        }

        private void ChangeLanguageFlow()
        {
            Console.Write(Lang("lang_choice"));
            string lang = GetUserInput();
            _vm.ChangeLanguage(lang);
            ShowMessages();
            Console.ReadKey();
        }

        // ── show data (read ViewModel.Jobs) ─
        /// show the jobs list.
        /// data came from _vm.Jobs.
   
        public void DisplayJobList()
        {
            if (_vm.HasNoJobs)
            {
                Console.WriteLine(Lang("job_no_jobs"));
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"Source",-30} {"Target",-30} {"Type",-15} {"Last Backup",-20}");
            Console.WriteLine(new string('-', 122));

            foreach (var job in _vm.Jobs)
            {
                string typeName = job.Type == BackupType.Full ? Lang("backup_full") : Lang("backup_diff");
                Console.WriteLine($"{job.Id,-5} {job.Name,-20} {job.SourceDirectory,-30} {job.TargetDirectory,-30} {typeName,-15} {job.LastBackupDate,-20}");
            }
            Console.WriteLine();
        }


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

        /// show the error message or normal message from the ViewModel.
        private void ShowMessages()
        {
            if (!string.IsNullOrEmpty(_vm.ErrorMessage))
                ShowError(_vm.ErrorMessage);

            if (!string.IsNullOrEmpty(_vm.Message))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_vm.Message);
                Console.ResetColor();
            }
        }
        /// return traduction for a key.

        private string Lang(string key) => _i18n.GetText(key);
    }
}
