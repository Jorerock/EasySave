using EasySave.Core.Domain;

namespace EasySave.Core.Application
{
    public interface ISettingsRepository
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}