using CryptoSoft;
using EasyLog.Entries;
using EasyLog.Interfaces;
using EasySave.Core.Domain;

namespace EasySave.Core.Infrastructure
{
    internal class CryptoSoftEncryptorAdapter
    {
        private readonly string _key;
        private readonly ILogWriter _logWriter;
        private readonly AppSettings _settings;

        public CryptoSoftEncryptorAdapter(string key, ILogWriter logWriter, AppSettings settings)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
                return elapsedMs;
            }
            catch (Exception ex)
            {
                //_logWriter.WriteError(backupName, inputFilePath, $"Erreur cryptage: {ex.Message}");
                Console.WriteLine(ex.ToString());
                return -1;
            }
        }

        /// <summary>
        /// Vérifie si une extension de fichier doit être cryptée
        /// </summary>
        public bool ShouldEncrypt(string filePath)
        {
            var extensions = _settings.ExtensionsToEncrypt;
            

            if (extensions == null || !extensions.Any())
                return true;

            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();

            return extensions.Any(ext =>
                ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

    }
}