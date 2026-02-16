using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EasySave.Infrastructure
{
    public class ProcessBusinessSoftwareDetector
    {

        private readonly string _processOrPath; 

        public bool IsBlocked()
        {
            // Logique : vérifier si l'un des processus définis dans les paramètres est actif
            // Exemple simplifié :
            return Process.GetProcesses().Any(p => p.ProcessName.Contains("LogicielMetier"));
        }
    }
}
