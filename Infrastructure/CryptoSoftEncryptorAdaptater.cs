using CryptoSoft;
using EasyLog.Entries;
using EasyLog.Interfaces;

namespace EasySave.Infrastructure
{
    internal class CryptoSoftEncryptorAdapter
    {
        private readonly string _key;
        private readonly ILogWriter _logWriter;

        public CryptoSoftEncryptorAdapter(string key, ILogWriter logWriter)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
        }

        /// Crypt the file and return is time in ms, or -1 if an error occurs
        public int EncryptFile(string inputFilePath, string backupName)
        {
            try
            {
                if (!File.Exists(inputFilePath))
                    throw new FileNotFoundException($"Fichier introuvable: {inputFilePath}");

                var manager = new FileManager(inputFilePath, _key);
                int elapsedMs = manager.TransformFile();

                // crypt log
                //_logWriter.WriteDailyLog(new LogEntry
                //{
                //    Timestamp = DateTime.Now,
                //    BackupName = backupName,
                //    SourcePathUNC = inputFilePath,
                //    TargetPathUNC = inputFilePath, // Le fichier est crypté sur place
                //    FileSizeBytes = new FileInfo(inputFilePath).Length,
                //    TransferTimeMs = elapsedMs,
                //    OperationType = "Encryption"
                //});

                return elapsedMs;
            }
            catch (Exception ex)
            {
                //_logWriter.WriteError(backupName, inputFilePath, $"Erreur cryptage: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Vérifie si une extension de fichier doit être cryptée
        /// </summary>
        public static bool ShouldEncrypt(string filePath, List<string> extensionsToEncrypt)
        {
            // Si la liste est null ou vide, on crypte TOUS les fichiers
            if (extensionsToEncrypt == null || !extensionsToEncrypt.Any())
                return true;

            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extensionsToEncrypt.Any(ext =>
                ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}