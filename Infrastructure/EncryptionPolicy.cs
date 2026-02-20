namespace EasySave.Infrastructure;

public sealed class EncryptionPolicy
{
    private readonly HashSet<string> _extensions;

    public EncryptionPolicy(IEnumerable<string> extensions)
    {
        IEnumerable<string> cleaned = extensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().StartsWith(".") ? e.Trim() : "." + e.Trim());

        _extensions = new HashSet<string>(cleaned, StringComparer.OrdinalIgnoreCase);
    }

    public bool ShouldEncrypt(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        return _extensions.Contains(ext);
    }
}

