using EasySave.Application;
using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.ViewModel
{
    public sealed class MainViewModel
    {
        private readonly JobManager _jobs;
        private readonly BackupOrchestrator _orchestrator;

        public MainViewModel(JobManager jobs, BackupOrchestrator orchestrator)
        {
            _jobs = jobs;
            _orchestrator = orchestrator;
        }

        public List<BackupJob> ListJobs()
        {
            return _jobs.GetAll();
        }

        public void CreateJob(string name, string src, string dst, BackupType type)
        {
            BackupJob job = new BackupJob
            {
                Name = name,
                SourceDirectory = src,
                TargetDirectory = dst,
                Type = type
            };

            _jobs.Add(job);
        }

        public void DeleteJob(int id)
        {
            _jobs.Remove(id);
        }

        public void RunJobs(List<int> ids)
        {
            _orchestrator.RunMany(ids);
        }

        public void RunAll()
        {
            _orchestrator.RunAllSequential();
        }
    }
}
