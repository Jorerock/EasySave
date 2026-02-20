using EasySave.Core.Domain;
using System.Collections.Generic;

namespace EasySave.Core.ViewModels
{
    /// <summary>
    /// Interface commune pour le ViewModel principal (CLI et WPF)
    /// </summary>
    public interface IMainViewModel
    {
        // ── Jobs Management ─────────────────────────────────────────────────
        List<BackupJob> ListJobs();
        void CreateJob(string name, string src, string dst, BackupType type, bool enableEncrypt, string encryptionKey, List<string> extensions);
        void AddJob(BackupJob job);  // ← AJOUTÉ : Pour WPF (ajouter un job déjà créé)
        void DeleteJob(int id);

        // ── Backup Execution ────────────────────────────────────────────────
        void RunJob(int id);
        void RunAll();

        // ── Settings Management ─────────────────────────────────────────────
        AppSettings GetCurrentSettings();
        void ApplySettings(AppSettings newSettings);

        // ── Status/Messages ─────────────────────────────────────────────────
        string MessageKey { get; }
        string ErrorKey { get; }
        string StatusMessage { get; }  // ← AJOUTÉ : Pour WPF
    }
}