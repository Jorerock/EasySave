using CryptoSoft;

namespace EasySave.Infrastructure
{
    internal class CryptoSoftEncryptorAdaptater
    {
        private readonly string _key;

        public CryptoSoftEncryptorAdaptater(string key)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public static int EncryptFile(string inputFilePath)
        {
            try
            {
                string key = "Je_Suis_une_cle_en_dure_dans_le_code";
                CryptoSoft.FileManager manager = new FileManager(inputFilePath, key);
                //elapsed should be used to log the time taken for encryption,
                //TODO: integrate with EasyLog to log this information
                int elapsed = manager.TransformFile();
                return elapsed;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error) 
                //Todo: integrate with EasyLog to log this information
                Console.WriteLine($"Error CryptoSoft dll : {ex.Message}");
                return -1;
            }
        }
    }
}
