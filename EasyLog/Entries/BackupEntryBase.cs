using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Entries
{
    public abstract class BackupEntryBase
    {
        public DateTime Timestamp { get; set; }

        public string BackupName { get; set; } = string.Empty;
    }
}
