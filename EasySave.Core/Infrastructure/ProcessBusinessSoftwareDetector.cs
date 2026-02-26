using EasySave.Core.Domain;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EasySave.Core.Infrastructure
{
    public class ProcessBusinessSoftwareDetector : IBusinessSoftwareDetector
    {
        private readonly AppSettings _settings;

        public ProcessBusinessSoftwareDetector(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

      



 public bool IsBlocked()
{
    // Tu peux stocker dans settings soit un "nom de process" (ex: "calc"),
    // soit un chemin (ex: "C:\Windows\System32\calc.exe").
    var value = _settings.BusinessSoftwarePath;

    if (string.IsNullOrWhiteSpace(value))
        return false;

    // Si l'utilisateur a mis un chemin complet, on récupère le nom du process
    // ex: calc.exe => calc
    string processName = value;

    try
    {
        if (value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar))
        {
            processName = Path.GetFileNameWithoutExtension(value);
        }
        else
        {
            processName = Path.GetFileNameWithoutExtension(value); // gère "calc.exe" ou "calc"
        }
    }
    catch
    {
        processName = value;
    }

    processName = processName.Trim();

    if (string.IsNullOrWhiteSpace(processName))
        return false;

    // Détection "exacte" du process
    return Process.GetProcessesByName(processName).Any();
}


}
}