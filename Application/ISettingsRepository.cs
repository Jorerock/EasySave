using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Application
{
    public interface ISettingsRepository
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
