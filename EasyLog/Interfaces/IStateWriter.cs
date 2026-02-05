using EasyLog.Entries;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Interfaces
{
    public interface IStateWriter
    {
        void WriteState(StateEntry state);
    }
}
