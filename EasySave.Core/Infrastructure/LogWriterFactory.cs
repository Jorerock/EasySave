using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Domain;
using System.IO;

namespace EasySave.Core.Infrastructure
{
    public static class LogWriterFactory
    {
        public static ILogWriter Create(AppSettings settings, string logDir)
        {
            Directory.CreateDirectory(logDir);

            // Writer local selon format
            ILogWriter localWriter =
                (settings.LogFormat?.ToLowerInvariant() == "xml")
                ? new XmlLogWriter(logDir)
                : new JsonLogWriter(logDir);

            var mode = (settings.LogMode ?? "local").ToLowerInvariant();

            return mode switch
            {
                "central" => new HttpLogWriter(settings.CentralLogUrl),
                "both" => new CompositeLogWriter(localWriter, new HttpLogWriter(settings.CentralLogUrl)),
                _ => localWriter
            };
        }
    }
}