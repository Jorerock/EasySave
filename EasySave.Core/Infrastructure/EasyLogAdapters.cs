//using System;
//using System.IO;
//using System.Text.Json;
//using EasyLog.Interfaces;
//using EasyLog.Entries;

//namespace EasySave.Core.Infrastructure
//{
//    // Implémentation minimale de ILogWriter attendue par FileSystemBackupEngine
//    internal sealed class SimpleFileLogWriter : ILogWriter
//    {
//        private readonly string _dir;

//        public SimpleFileLogWriter(string dir)
//        {
//            if (string.IsNullOrWhiteSpace(dir)) throw new ArgumentNullException(nameof(dir));
//            _dir = dir;
//            Directory.CreateDirectory(_dir);
//        }

//        // Méthode utilisée dans FileSystemBackupEngine : WriteDailyLog(LogEntry)
//        public void WriteDailyLog(LogEntry entry)
//        {
//            if (entry == null) return;

//            string filename = Path.Combine(_dir, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log");
//            string line = $"{entry.Timestamp:o} | {entry.BackupName} | {entry.SourcePathUNC} -> {entry.TargetPathUNC} | {entry.FileSizeBytes} B | {entry.TransferTimeMs} ms{Environment.NewLine}";
//            File.AppendAllText(filename, line);
//        }
//    }

//    // Implémentation minimale de IStateWriter attendue par FileSystemBackupEngine
//    internal sealed class SimpleFileStateWriter : IStateWriter
//    {
//        private readonly string _dir;

//        public SimpleFileStateWriter(string dir)
//        {
//            if (string.IsNullOrWhiteSpace(dir)) throw new ArgumentNullException(nameof(dir));
//            _dir = dir;
//            Directory.CreateDirectory(_dir);
//        }

//        // Méthode utilisée dans FileSystemBackupEngine : WriteState(StateEntry)
//        public void WriteState(StateEntry entry)
//        {
//            if (entry == null) return;

//            string safeName = MakeSafeFilename(entry.BackupName);
//            string filename = Path.Combine(_dir, safeName + ".state.json");
//            var options = new JsonSerializerOptions { WriteIndented = true };
//            string json = JsonSerializer.Serialize(entry, options);
//            File.WriteAllText(filename, json);
//        }

//        private static string MakeSafeFilename(string name)
//        {
//            if (string.IsNullOrWhiteSpace(name)) return "unnamed";
//            foreach (var c in Path.GetInvalidFileNameChars())
//            {
//                name = name.Replace(c, '_');
//            }
//            return name;
//        }
//    }
//}