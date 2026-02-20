namespace EasySave.Infrastructure;

public static class AppPaths
{
    public static string BaseDir
    {
        get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProSoft", "EasySave"); }
    }

    public static string JobsPath
    {
        get { return Path.Combine(BaseDir, "jobs.json"); }
    }

    public static string SettingsPath
    {
        get { return Path.Combine(BaseDir, "settings.json"); }
    }

    public static string LogsDir
    {
        get { return Path.Combine(BaseDir, "logs"); }
    }

    public static string StatePath
    {
        get { return Path.Combine(BaseDir, "state.json"); }
    }
}
