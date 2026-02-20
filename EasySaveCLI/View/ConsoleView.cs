using EasySave.Core.Domain;
using EasySave.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EasySave.View
{
    public sealed class ConsoleView
    {
        private readonly MainViewModel _vm;
        private readonly ILocalizationService _i18n;

        public ConsoleView(MainViewModel vm, ILocalizationService i18n)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _i18n = i18n ?? throw new ArgumentNullException(nameof(i18n));
        }

        public void Start()
        {
            bool exit = false;

            while (!exit)
            {
                ShowMenu();
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string choice = input.Trim();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            ShowJobs();
                            break;

                        case "2":
                            CreateJobFlow();
                            break;

                        case "3":
                            DeleteJobFlow();
                            break;

                        case "4":
                            RunSelectedJobsFlow();
                            break;

                        case "5":
                            RunAllFlow();
                            break;

                        case "6":
                            ChangeLanguageFlow(); // ✅ persiste dans settings.json
                            break;

                        case "7":
                            Menu_CryptList();
                            break;

                        case "0":
                            exit = true;
                            break;

                        default:
                            Console.WriteLine(_i18n.T("InvalidChoice"));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(_i18n.T("Error") + " " + ex.Message);
                }

                Console.WriteLine();
            }
        }

        private void ShowMenu()
        {
            Console.WriteLine("======================================");
            Console.WriteLine("EasySave");
            Console.WriteLine("======================================");
            Console.WriteLine("1) " + _i18n.T("Menu_ListJobs"));
            Console.WriteLine("2) " + _i18n.T("Menu_CreateJob"));
            Console.WriteLine("3) " + _i18n.T("Menu_DeleteJob"));
            Console.WriteLine("4) " + _i18n.T("Menu_RunSelected"));
            Console.WriteLine("5) " + _i18n.T("Menu_RunAll"));
            Console.WriteLine("6) " + _i18n.T("Menu_ChangeLanguage"));
            Console.WriteLine("7) " + _i18n.T("Menu_CryptList"));
            Console.WriteLine("0) " + _i18n.T("Menu_Exit"));
            Console.Write(_i18n.T("Prompt_Choice") + " ");
        }

        private void ShowJobs()
        {
            List<BackupJob> jobs = _vm.ListJobs();

            if (jobs.Count == 0)
            {
                Console.WriteLine(_i18n.T("NoJobs"));
                return;
            }

            Console.WriteLine(_i18n.T("JobsHeader"));
            for (int i = 0; i < jobs.Count; i++)
            {
                BackupJob job = jobs[i];
                Console.WriteLine($"[{job.Id}] {job.Name} | {job.Type} | {job.SourceDirectory} -> {job.TargetDirectory}");
            }
        }

        private void CreateJobFlow()
        {
            Console.Clear();
            Console.Write(_i18n.T("Prompt_Name") + " ");
            string name = Console.ReadLine() ?? string.Empty;

            Console.Write(_i18n.T("Prompt_Source") + " ");
            string src = Console.ReadLine() ?? string.Empty;

            Console.Write(_i18n.T("Prompt_Target") + " ");
            string dst = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("1) Full");
            Console.WriteLine("2) Differential");
            Console.Write(_i18n.T("Prompt_Type") + " ");
            string typeInput = Console.ReadLine() ?? "1";

            BackupType type = typeInput.Trim() == "2" ? BackupType.Differential : BackupType.Full;

            Console.WriteLine("\n" + _i18n.T("EncryptionOptions") + ":");
            Console.Write(_i18n.T("Prompt_EnableEncryption") + " (O/N): ");
            string encryptChoice = Console.ReadLine()?.Trim().ToUpper() ?? "N";
            bool enableEncryption = encryptChoice == "O" || encryptChoice == "Y" || encryptChoice == "YES" || encryptChoice == "OUI";

            string encryptionKey = null;
            List<string> extensionsToEncrypt = new List<string>();

            if (enableEncryption)
            {
                Console.Write(_i18n.T("Prompt_EncryptionKey") + " ");
                encryptionKey = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(encryptionKey))
                {
                    Console.WriteLine(_i18n.T("Warning_NoKey"));
                    encryptionKey = "DefaultKey_EasySave_2024";
                    Console.WriteLine(_i18n.T("Info_UsingDefaultKey"));
                }

                Console.WriteLine("\n" + _i18n.T("Prompt_ExtensionsToEncrypt"));
                Console.WriteLine(_i18n.T("Info_ExtensionsExample"));
                Console.WriteLine(_i18n.T("Info_ExtensionsEmpty"));
                Console.Write("> ");
                string extensionsInput = Console.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(extensionsInput))
                {
                    extensionsToEncrypt = extensionsInput
                        .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                        .Select(ext => ext.ToLowerInvariant())
                        .Distinct()
                        .ToList();

                    Console.WriteLine(_i18n.T("Info_ExtensionsSelected") + ": " + string.Join(", ", extensionsToEncrypt));
                }
                else
                {
                    Console.WriteLine(_i18n.T("Info_AllFilesEncrypted"));
                }
            }

            _vm.CreateJob(name, src, dst, type, enableEncryption, encryptionKey, extensionsToEncrypt);

            Console.WriteLine("\n=== " + _i18n.T("JobSummary") + " ===");
            Console.WriteLine(_i18n.T("Name") + ": " + name);
            Console.WriteLine(_i18n.T("Source") + ": " + src);
            Console.WriteLine(_i18n.T("Target") + ": " + dst);
            Console.WriteLine(_i18n.T("Type") + ": " + type);
            Console.WriteLine(_i18n.T("Encryption") + ": " + (enableEncryption ? _i18n.T("Yes") : _i18n.T("No")));

            if (enableEncryption)
            {
                string extText = extensionsToEncrypt.Any()
                    ? string.Join(", ", extensionsToEncrypt)
                    : _i18n.T("All");
                Console.WriteLine(_i18n.T("Extensions") + ": " + extText);
            }

            Console.WriteLine(_i18n.T("JobCreated"));
        }

        private void DeleteJobFlow()
        {
            Console.Write(_i18n.T("Prompt_Id") + " ");
            string input = Console.ReadLine() ?? string.Empty;

            if (!int.TryParse(input.Trim(), out int id))
            {
                Console.WriteLine(_i18n.T("InvalidId"));
                return;
            }

            _vm.DeleteJob(id);
            Console.WriteLine(_i18n.T("JobDeleted"));
        }

        private void RunSelectedJobsFlow()
        {
            Console.WriteLine(_i18n.T("Prompt_RunSelected"));
            Console.WriteLine(_i18n.T("Hint_RunSelected"));
            Console.Write("> ");
            string selection = Console.ReadLine() ?? string.Empty;

            CommandParser parser = new CommandParser(1, int.MaxValue);
            List<int> ids = parser.ParseJobSelection(selection);

            if (ids.Count == 0)
            {
                Console.WriteLine(_i18n.T("NoValidSelection"));
                return;
            }

            foreach (int id in ids)
            {
                _vm.RunJob(id);
            }

            Console.WriteLine(_i18n.T("RunDone"));
        }

        private void RunAllFlow()
        {
            _vm.RunAll();
            Console.WriteLine(_i18n.T("RunDone"));
        }

        // ✅ IMPORTANT : change langue + sauvegarde dans settings.json
        private void ChangeLanguageFlow()
        {
            Console.WriteLine("1) FR");
            Console.WriteLine("2) EN");
            Console.Write("> ");
            string input = Console.ReadLine() ?? "1";

            bool english = input.Trim() == "2";

            // 1) change i18n immédiat
            _i18n.CurrentLanguage = english ? "en" : "fr";

            // 2) persist via settings
            AppSettings settings = _vm.GetCurrentSettings();
            settings.SetLanguage(english ? AppLanguage.Anglais : AppLanguage.Francais);
            _vm.ApplySettings(settings);

            Console.WriteLine(_i18n.T("LanguageChanged"));
        }

        private List<string> _extensionsToEncrypt = new List<string>();

        private readonly List<string> _availableExtensions = new List<string>
        {
            ".txt", ".csv", ".png", ".jpg", ".docx", ".pdf"
        };

        private void Menu_CryptList()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Choix des extensions à crypter ===\n");

                for (int i = 0; i < _availableExtensions.Count; i++)
                {
                    string ext = _availableExtensions[i];
                    bool isSelected = _extensionsToEncrypt.Contains(ext);
                    string status = isSelected ? "[X]" : "[ ]";
                    ConsoleColor color = isSelected ? ConsoleColor.Green : ConsoleColor.Gray;

                    Console.ForegroundColor = color;
                    Console.WriteLine("  " + (i + 1) + ") " + status + " " + ext);
                    Console.ResetColor();
                }

                Console.WriteLine("\n  0) Valider et quitter");
                Console.WriteLine("\nEntrez un numéro pour cocher/décocher une extension.");
                Console.Write("> ");

                string input = Console.ReadLine();

                if (input == "0")
                    break;

                if (int.TryParse(input, out int choice) && choice >= 1 && choice <= _availableExtensions.Count)
                {
                    string selected = _availableExtensions[choice - 1];

                    if (_extensionsToEncrypt.Contains(selected))
                        _extensionsToEncrypt.Remove(selected);
                    else
                        _extensionsToEncrypt.Add(selected);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Entrée invalide.");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                }
            }

            Console.Clear();
            if (_extensionsToEncrypt.Count == 0)
            {
                Console.WriteLine("Aucune extension sélectionnée.");
            }
            else
            {
                Console.WriteLine("Extensions qui seront cryptées :");
                foreach (string ext in _extensionsToEncrypt)
                    Console.WriteLine("  - " + ext);
            }

            Console.WriteLine("\nAppuyez sur une touche pour continuer...");
            Console.ReadKey();
        }
    }
}
