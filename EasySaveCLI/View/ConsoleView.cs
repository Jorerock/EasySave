using EasySave.Core.Domain;
using EasySave.ViewModel;
using System;
using System.Collections.Generic;

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
                string? input = Console.ReadLine();

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
                            ChangeLanguageFlow();
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
                Console.WriteLine(
                    $"[{job.Id}] {job.Name} | {job.Type} | {job.SourceDirectory} -> {job.TargetDirectory}"
                );
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

            // Options de cryptage
            Console.WriteLine("\n" + _i18n.T("EncryptionOptions") + ":");
            Console.Write(_i18n.T("Prompt_EnableEncryption") + " (O/N): ");
            string encryptChoice = Console.ReadLine()?.Trim().ToUpper() ?? "N";
            bool enableEncryption = encryptChoice == "O" || encryptChoice == "Y" || encryptChoice == "YES" || encryptChoice == "OUI";

            string encryptionKey = null;
            List<string> extensionsToEncrypt = new List<string>();

            if (enableEncryption)
            {
                // Demander la clé de cryptage
                Console.Write(_i18n.T("Prompt_EncryptionKey") + " ");
                encryptionKey = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(encryptionKey))
                {
                    Console.WriteLine(_i18n.T("Warning_NoKey"));
                    encryptionKey = "DefaultKey_EasySave_2024"; // Clé par défaut
                    Console.WriteLine(_i18n.T("Info_UsingDefaultKey"));
                }

                // Demander les extensions à crypter
                Console.WriteLine("\n" + _i18n.T("Prompt_ExtensionsToEncrypt"));
                Console.WriteLine(_i18n.T("Info_ExtensionsExample")); // Ex: .docx .pdf .txt .xlsx
                Console.WriteLine(_i18n.T("Info_ExtensionsEmpty")); // (Entrée vide = tous les fichiers)
                Console.Write("> ");
                string extensionsInput = Console.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(extensionsInput))
                {
                    // Parser les extensions
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

            // Resume
            Console.WriteLine("\n=== " + _i18n.T("JobSummary") + " ===");
            Console.WriteLine($"{_i18n.T("Name")}: {name}");
            Console.WriteLine($"{_i18n.T("Source")}: {src}");
            Console.WriteLine($"{_i18n.T("Target")}: {dst}");
            Console.WriteLine($"{_i18n.T("Type")}: {type}");
            Console.WriteLine($"{_i18n.T("Encryption")}: {(enableEncryption ? _i18n.T("Yes") : _i18n.T("No"))}");
            if (enableEncryption)
            {
                Console.WriteLine($"{_i18n.T("Extensions")}: {(extensionsToEncrypt.Any() ? string.Join(", ", extensionsToEncrypt) : _i18n.T("All"))}");
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
            Console.WriteLine(_i18n.T("Hint_RunSelected")); // ex: "1-3" ou "1;3"
            Console.Write("> ");
            string selection = Console.ReadLine() ?? string.Empty;

            CommandParser parser = new CommandParser(1, int.MaxValue);
            List<int> ids = parser.ParseJobSelection(selection);

            if (ids.Count == 0)
            {
                Console.WriteLine(_i18n.T("NoValidSelection"));
                return;
            }

            // Fix: RunJob expects a single int. Iterate over parsed ids and call RunJob for each id.
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

        private void ChangeLanguageFlow()
        {
            Console.WriteLine("1) FR");
            Console.WriteLine("2) EN");
            Console.Write("> ");
            string input = Console.ReadLine() ?? "1";

            string lang = input.Trim() == "2" ? "en" : "fr";
            _i18n.CurrentLanguage = lang;

            Console.WriteLine(_i18n.T("LanguageChanged"));
        }
    }
}
