using EasyLog.Interfaces;
using EasyLog.Writers;
using EasySave.Core.Domain;
using System;

using System.IO;

namespace EasySave.Core.Infrastructure
{
    public static class LogWriterFactory
    {
        public static ILogWriter Create(AppSettings settings, string logDir)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Directory.CreateDirectory(logDir);

            // 1) Writer local selon format
            ILogWriter localWriter =
                (settings.LogFormat?.ToLowerInvariant() == "xml")
                ? new XmlLogWriter(logDir)
                : new JsonLogWriter(logDir);

            // 2) Mode
            var mode = (settings.LogMode ?? "local").ToLowerInvariant();

            // 3) URL central (fallback)
            var url = (settings.CentralLogUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(url))
                url = "http://localhost:5080";

            return mode switch
            {
                "central" => new HttpLogWriter(url),
                "both" => new CompositeLogWriter(localWriter, new HttpLogWriter(url)),
                _ => localWriter
            };
        }
    }
}