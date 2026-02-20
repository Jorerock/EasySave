using System;
using System.Collections.Generic;

namespace EasySave.Core.Domain
{
    public enum AppLanguage
    {
        Anglais,
        Francais
    }

    public class AppSettings
    {
        public AppLanguage Language { get; set; }

        public string LogFormat { get; set; } = "json";
        public List<string> ExtensionsToEncrypt { get; set; } = new List<string>();
        public string BusinessSoftwarePath { get; set; } = "";

        
        public AppSettings()
        {
            Language = AppLanguage.Anglais;
            LogFormat = "json";
            ExtensionsToEncrypt = new List<string>();
            BusinessSoftwarePath = "";
        }

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
