using System.Collections.Generic;
namespace EasySave.View

{

    public interface ILocalizationService
    {
        string CurrentLanguage { get; set; }
        string T(string key);
    }

    /// <summary>
    /// Implementation of the localization service.
    /// Manages translations for French and English languages.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations;

        public LocalizationService()
        {
            CurrentLanguage = "fr";

            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "fr", new Dictionary<string, string>
                    {
                        { "InvalidChoice", "Choix invalide." },
                        { "Error", "Erreur :" },

                        { "Menu_ListJobs", "Lister les sauvegardes" },
                        { "Menu_CreateJob", "Créer une sauvegarde" },
                        { "Menu_DeleteJob", "Supprimer une sauvegarde" },
                        { "Menu_RunSelected", "Exécuter des sauvegardes (sélection)" },
                        { "Menu_RunAll", "Exécuter toutes les sauvegardes" },
                        { "Menu_ChangeLanguage", "Changer la langue" },
                        { "Menu_Exit", "Quitter" },

                        { "Prompt_Choice", "Votre choix" },
                        { "Prompt_Name", "Nom de la sauvegarde" },
                        { "Prompt_Source", "Répertoire source" },
                        { "Prompt_Target", "Répertoire cible" },
                        { "Prompt_Type", "Type (1=Full, 2=Diff)" },
                        { "Prompt_Id", "ID du job" },
                        { "Prompt_RunSelected", "Entrez la sélection à exécuter" },
                        { "Hint_RunSelected", "Exemples: 1-3   ou   1;3" },

                        { "NoJobs", "Aucune sauvegarde disponible." },
                        { "JobsHeader", "Liste des sauvegardes :" },
                        { "JobCreated", "Sauvegarde créée." },
                        { "JobDeleted", "Sauvegarde supprimée." },
                        { "InvalidId", "ID invalide." },
                        { "NoValidSelection", "Aucun ID valide dans la sélection." },
                        { "RunDone", "Exécution terminée." },
                        { "LanguageChanged", "Langue modifiée." }
                    }
                },
                {
                    "en", new Dictionary<string, string>
                    {
                        { "InvalidChoice", "Invalid choice." },
                        { "Error", "Error:" },

                        { "Menu_ListJobs", "List backup jobs" },
                        { "Menu_CreateJob", "Create a backup job" },
                        { "Menu_DeleteJob", "Delete a backup job" },
                        { "Menu_RunSelected", "Run selected jobs" },
                        { "Menu_RunAll", "Run all jobs" },
                        { "Menu_ChangeLanguage", "Change language" },
                        { "Menu_Exit", "Exit" },

                        { "Prompt_Choice", "Your choice" },
                        { "Prompt_Name", "Backup name" },
                        { "Prompt_Source", "Source directory" },
                        { "Prompt_Target", "Target directory" },
                        { "Prompt_Type", "Type (1=Full, 2=Diff)" },
                        { "Prompt_Id", "Job ID" },
                        { "Prompt_RunSelected", "Enter selection to run" },
                        { "Hint_RunSelected", "Examples: 1-3   or   1;3" },

                        { "NoJobs", "No backup job available." },
                        { "JobsHeader", "Backup jobs list:" },
                        { "JobCreated", "Job created." },
                        { "JobDeleted", "Job deleted." },
                        { "InvalidId", "Invalid ID." },
                        { "NoValidSelection", "No valid ID in selection." },
                        { "RunDone", "Execution finished." },
                        { "LanguageChanged", "Language updated." }
                    }
                }
            };
        }

        public string CurrentLanguage { get; set; }

        public string T(string key)
        {
            if (_translations.TryGetValue(CurrentLanguage, out Dictionary<string, string>? langDict))
            {
                if (langDict.TryGetValue(key, out string? value))
                {
                    return value;
                }
            }

            // fallback: returns key if missing
            return key;
        }
    }
}
