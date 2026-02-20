using System.Text.Json;
using EasySave.Application;
using EasySave.Domain;

namespace EasySave.Infrastructure;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _path;
    private readonly JsonSerializerOptions _opt;

    public JsonSettingsRepository(string path)
    {
        _path = path;

        string? dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _opt = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            AppSettings defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        string json = File.ReadAllText(_path);
        AppSettings? parsed = JsonSerializer.Deserialize<AppSettings>(json, _opt);
        return parsed ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, _opt);
        File.WriteAllText(_path, json);
    }
}
