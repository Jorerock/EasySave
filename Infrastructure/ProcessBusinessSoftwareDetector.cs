using System.Diagnostics;

namespace EasySave.Infrastructure;

public sealed class ProcessBusinessSoftwareDetector : IBusinessSoftwareDetector
{
    private readonly string _processName;

    public ProcessBusinessSoftwareDetector(string processName)
    {
        _processName = (processName ?? "").Trim();
    }

    public bool IsBlocked()
    {
        if (string.IsNullOrWhiteSpace(_processName)) return false;

        string name = _processName;

        // Vérification de la longueur pour éviter le crash sur Substring
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
        {
            name = name.Substring(0, name.Length - 4);
        }

        Process[] processes = Process.GetProcessesByName(name);
        return processes.Length > 0;
    }
}
