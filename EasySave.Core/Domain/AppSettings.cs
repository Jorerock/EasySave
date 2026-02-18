using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Domain
{
    public enum AppLanguage 
    { 
        Anglais, 
        Francais 
    }

    public class AppSettings
    {
        // Propriétés
        public AppLanguage Language { get; private set; }
        public string LogFormat { get; set; } = "json/xml";
        public List<string> ExtensionsToEncrypt { get; set; } = new List<string>(); // List of file extensions to encrypt (empty means encrypt all)
        public string BusinessSoftwarePath { get; set; } // Software witch will block the backup when it is running

        // Constructor
        // AppSettings.cs
        public AppSettings()  // ← Constructeur sans paramètres ajouté
        {
            Language = AppLanguage.Anglais;
            LogFormat = "json";
            ExtensionsToEncrypt = new List<string>();
            BusinessSoftwarePath = "";
        }

        // Méthode pour définir la langue (uniquement Anglais ou Français)
        public void SetLanguage(AppLanguage language)
        {
            if (!Enum.IsDefined(typeof(AppLanguage), language))
            {
                throw new ArgumentException("Langue non supportée.");
            }

            Language = language;
        }

      
    }
}
