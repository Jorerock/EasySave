using EasySave.Core.Domain;
using System;

namespace EasySave.Core.Application
{
    public sealed class SettingsManager
    {
        private readonly ISettingsRepository _repo;
        private AppSettings _currentSettings;

        public SettingsManager(ISettingsRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

            // Charge les settings au démarrage, ou crée des settings par défaut
            _currentSettings = _repo.Load() ?? new AppSettings();
        }

        /// <summary>
        /// Récupère les settings actuels
        /// </summary>
        public AppSettings Get()
        {
            return _currentSettings;
        }

        /// <summary>
        /// Met à jour et sauvegarde les settings
        /// </summary>
        public void ApplySettings(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _currentSettings = settings;
            _repo.Save(settings);
        }
    }
}