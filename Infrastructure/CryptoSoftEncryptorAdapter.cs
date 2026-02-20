using System.Diagnostics;

namespace EasySave.Infrastructure;

public sealed class CryptoSoftEncryptorAdapter : IEncryptor
{
    private readonly string _cryptoSoftExePath;

    public CryptoSoftEncryptorAdapter(string cryptoSoftExePath)
    {
        _cryptoSoftExePath = cryptoSoftExePath ?? "";
    }

    public long EncryptFile(string inputPath, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(_cryptoSoftExePath) || !File.Exists(_cryptoSoftExePath))
            return -2;

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = _cryptoSoftExePath,
                Arguments = $"-in \"{inputPath}\" -out \"{outputPath}\"", // adapte à ton CryptoSoft
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Stopwatch sw = Stopwatch.StartNew();
            using Process? p = Process.Start(psi);
            if (p == null) return -3;

            p.WaitForExit();
            sw.Stop();

            if (p.ExitCode != 0) return -Math.Abs(p.ExitCode);
            return sw.ElapsedMilliseconds;
        }
        catch
        {
            return -1;
        }
    }
}
