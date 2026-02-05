using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Entries
{
    public sealed class LogEntry : BackupEntryBase
    {
        public string SourcePathUNC { get; set; } = string.Empty;

        public string TargetPathUNC { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Transfer time in ms. Must be negative if error.
        /// </summary>
        public long TransferTimeMs { get; set; }
    }
}
