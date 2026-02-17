using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Domain
{
    public enum Language
    {
        Anglais,
        Francais
    }

    public class AppSettings
    {
        // Propriétés
        public Language Language { get; private set; }
        public string LogFormat { get; set; } = "json/xml";
        public List<string> ExtensionsToEncrypt { get; set; } = new List<string>();
        public string BusinessSoftwarePath { get; set; }
        public string CryptoSoftPath { get; set; }

        // Constructeur
        public AppSettings(Language language)
        {
            SetLanguage(language);
        }

        // Méthode pour définir la langue (uniquement Anglais ou Français)
        public void SetLanguage(Language language)
        {
            if (!Enum.IsDefined(typeof(Language), language))
            {
                throw new ArgumentException("Langue non supportée.");
            }

            Language = language;
        }

      
    }
}
