using CryptoSoft;

namespace EasySave.Infrastructure
{
    internal class CryptoSoftEncryptorAdaptater
    {
        internal required string _CryptoSoftExePath;

        public void CryptoSoftEncryptorAdapter(string CryptoSoftExePath)
        {
            _CryptoSoftExePath = CryptoSoftExePath ?? throw new ArgumentNullException(nameof(CryptoSoftExePath));
        }

        public void EncryptFile(string inputFilePath, string outputFilePath)
        {
            string key = "si j'oublie de retirer la cle du code celestin je suis dans la mouise";
            CryptoSoft.FileManager manager = new FileManager(inputFilePath, key);
            int elapsed = manager.TransformFile();

        }
    }
}
