using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Entries
{
    public enum JobRunState
    {
        Inactive = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }
}
