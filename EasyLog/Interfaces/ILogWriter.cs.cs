using EasyLog.Entries;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Interfaces
{
    public interface ILogWriter
    {
        void WriteDailyLog(LogEntry entry);
    }
}
