using EasyLog.Interfaces;
using EasyLog.Writers;

namespace EasySave.Infrastructure;

public static class LogWriterFactory
{
    public static ILogWriter Create(string format, string logDir)
    {
        string normalized = (format ?? "").Trim().ToLowerInvariant();
        if (normalized == "xml") return new XmlLogWriter(logDir);
        return new JsonLogWriter(logDir);
    }
}
