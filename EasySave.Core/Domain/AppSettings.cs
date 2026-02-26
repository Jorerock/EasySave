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
        public List<string> PriorityExtensions { get; set; } = new();// ex: [".pdf",".docx"]
        public int LargeFileThresholdKo { get; set; } = 1024;
        public string LogMode { get; set; } = "local"; // local | central | both
        public string CentralLogUrl { get; set; } = "http://localhost:5080"; // docker service
        public string CryptoSoftPath { get; set; } = "";

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
