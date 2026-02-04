namespace EasySave.View
{

    /// <summary>
    /// Implementation of the localization service.
    /// Manages translations for French and English languages.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private Dictionary<string, string> _translations;
        private string _currentLanguage;

        // Dictionary containing all translations for each language
        private readonly Dictionary<string, Dictionary<string, string>> _languages;

        public string CurrentLanguage => _currentLanguage;

        public LocalizationService()
        {
            _languages = new Dictionary<string, Dictionary<string, string>>();
            InitializeLanguages();
            _currentLanguage = "fr"; // Default language
            _translations = _languages[_currentLanguage];
        }

        /// <summary>
        /// Gets the translated text for a given key.
        /// </summary>
        public string GetText(string key)
        {
            if (_translations.ContainsKey(key))
                return _translations[key];

            // Return the key itself if translation not found
            return $"[{key}]";
        }

        /// <summary>
        /// Changes the current language.
        /// </summary>
        public bool ChangeLanguage(string languageCode)
        {
            languageCode = languageCode.ToLower();

            if (_languages.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                _translations = _languages[languageCode];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes all available languages and their translations.
        /// </summary>
        private void InitializeLanguages()
        {
            // English translations
            _languages["en"] = new Dictionary<string, string>
        {
            // Menu
            { "menu_title", "=== EasySave - Backup Manager ===" },
            { "menu_create", "1. Create a backup job" },
            { "menu_delete", "2. Delete a backup job" },
            { "menu_execute_one", "3. Execute a backup job" },
            { "menu_execute_all", "4. Execute all backup jobs" },
            { "menu_display", "5. Display backup jobs" },
            { "menu_language", "6. Change language" },
            { "menu_quit", "0. Quit" },

            // Job creation
            { "job_name", "Job name: " },
            { "job_source", "Source directory: " },
            { "job_target", "Target directory: " },
            { "job_type", "Backup type (1=Full, 2=Differential): " },
            { "job_id_delete", "Enter the job ID to delete: " },
            { "job_id_execute", "Enter the job ID to execute: " },
            { "job_no_jobs", "No backup jobs available." },

            // Backup types
            { "backup_full", "Full" },
            { "backup_diff", "Differential" },

            // Language
            { "lang_choice", "Choose language (en/fr): " },

            // Messages
            { "copying", "Copying in progress..." },
            { "error_invalid_input", "Error: Invalid input." },
            { "error_job_not_found", "Error: Job not found." },
            { "error_invalid_path", "Error: Invalid path." },
            { "success_job_created", "Job created successfully." },
            { "success_job_deleted", "Job deleted successfully." },
            { "success_job_executed", "Job executed successfully." },
            { "success_language_changed", "Language changed successfully." }
        };

            // French translations
            _languages["fr"] = new Dictionary<string, string>
        {
            // Menu
            { "menu_title", "=== EasySave - Gestionnaire de Sauvegardes ===" },
            { "menu_create", "1. Créer un travail de sauvegarde" },
            { "menu_delete", "2. Supprimer un travail de sauvegarde" },
            { "menu_execute_one", "3. Exécuter un travail de sauvegarde" },
            { "menu_execute_all", "4. Exécuter tous les travaux de sauvegarde" },
            { "menu_display", "5. Afficher les travaux de sauvegarde" },
            { "menu_language", "6. Changer de langue" },
            { "menu_quit", "0. Quitter" },

            // Job creation
            { "job_name", "Nom du travail : " },
            { "job_source", "Répertoire source : " },
            { "job_target", "Répertoire cible : " },
            { "job_type", "Type de sauvegarde (1=Complète, 2=Différentielle) : " },
            { "job_id_delete", "Entrez l'ID du travail à supprimer : " },
            { "job_id_execute", "Entrez l'ID du travail à exécuter : " },
            { "job_no_jobs", "Aucun travail de sauvegarde disponible." },

            // Backup types
            { "backup_full", "Complète" },
            { "backup_diff", "Différentielle" },

            // Language
            { "lang_choice", "Choisissez la langue (en/fr) : " },

            // Messages
            { "copying", "Copie en cours..." },
            { "error_invalid_input", "Erreur : Saisie invalide." },
            { "error_job_not_found", "Erreur : Travail introuvable." },
            { "error_invalid_path", "Erreur : Chemin invalide." },
            { "success_job_created", "Travail créé avec succès." },
            { "success_job_deleted", "Travail supprimé avec succès." },
            { "success_job_executed", "Travail exécuté avec succès." },
            { "success_language_changed", "Langue modifiée avec succès." }
        };
        }

        public IEnumerable<string> GetAvailableLanguages()
        {
            return _languages.Keys;
        }
    }
   

    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        string GetText(string key);
        bool ChangeLanguage(string languageCode);
    }

}
