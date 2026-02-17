namespace EasySave.Core.Domain
{
    public sealed class BackupJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public DateTime LastRunUtc { get; set; } = DateTime.UtcNow;
        public bool EnableEncryption { get; set; }
        public List<string> ExtensionsToEncrypt { get; set; } = new List<string>();
        public string? EncryptionKey { get; set; }

    }
}
