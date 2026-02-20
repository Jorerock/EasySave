using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EasySave.Core.Infrastructure
{
    public class ProcessBusinessSoftwareDetector : IBusinessSoftwareDetector
    {
        private readonly string _processPath; // Ou une liste issue des AppSettings

        public bool IsBlocked()
        {
            // Logique : vérifier si l'un des processus définis dans les paramètres est actif
            return Process.GetProcesses().Any(p => p.ProcessName.Contains("LogicielMetier"));
        }
    }
}
