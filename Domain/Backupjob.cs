using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Domain
{
    internal class Backupjob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public string TargetDirectory { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public string LastBackupDate { get; set; } = string.Empty;
    }
  
}
