using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Application
{
    internal interface IJobRepository
    {
        List<BackupJob> LoadAll();
        void SaveAll(List<BackupJob> jobs);
    }
}
