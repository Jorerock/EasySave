namespace EasySave.Infrastructure;

public interface IEncryptor
{
    long EncryptFile(string inputPath, string outputPath);
}
