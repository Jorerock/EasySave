using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Entries
{
    public sealed class StateEntry : BackupEntryBase
    {
        public JobRunState State { get; set; }

        public int TotalFiles { get; set; }

        public long TotalSizeBytes { get; set; }

        public int FilesRemaining { get; set; }

        public long SizeRemainingBytes { get; set; }

        public int ProgressPct { get; set; }

        public string CurrentSourceUNC { get; set; } = string.Empty;

        public string CurrentTargetUNC { get; set; } = string.Empty;
    }
}
