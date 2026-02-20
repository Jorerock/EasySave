using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Application
{
    public sealed class SettingsManager
    {
        private readonly ISettingsRepository _repo;

        public SettingsManager(ISettingsRepository repo)
        {
            _repo = repo;
        }

        public AppSettings Get()
        {
            return _repo.Load();
        }

        public void Save(AppSettings settings)
        {
            _repo.Save(settings);
        }
    }
}
