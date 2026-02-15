using EasySave.Application;
using EasySave.Domain;
using System.Collections.Generic;

namespace EasySave.ViewModel
{
    public sealed class MainViewModel
    {
        private readonly JobManager _jobs;
        private readonly BackupOrchestrator _orchestrator;

        // Clés de message (interprétées par la View)
        public string MessageKey { get; private set; }
        public string ErrorKey { get; private set; }

        internal MainViewModel(JobManager jobs, BackupOrchestrator orchestrator)
        {
            _jobs = jobs;
            _orchestrator = orchestrator;
        }

        private void ClearMessages()
        {
            MessageKey = null;
            ErrorKey = null;
        }

        // ── Lecture ─────────────────────────────

        internal List<BackupJob> ListJobs()
        {
            return _jobs.GetAll();
        }

        // ── Actions ─────────────────────────────

        public void CreateJob(string name, string src, string dst, BackupType type, bool EnableEncrypt, string? Key, List<string> Extensions)
        {
            ClearMessages();

            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst))
            {
                ErrorKey = "error_invalid_path";
                return;
            }

            try
            {
                BackupJob job = new BackupJob
                {
                    Name = name,
                    SourceDirectory = src,
                    TargetDirectory = dst,
                    EnableEncryption = EnableEncrypt,
                    Type            = type,
                    EncryptionKey = Key,
                    ExtensionsToEncrypt = Extensions
                };

                _jobs.Add(job);
                MessageKey = "success_job_created";
            }
            catch
            {
                ErrorKey = "error_invalid_input";
            }
        }

        public void DeleteJob(int id)
        {
            ClearMessages();

            try
            {
                _jobs.Remove(id);
                MessageKey = "success_job_deleted";
            }
            catch
            {
                ErrorKey = "error_job_not_found";
            }
        }

        public void RunJob(int id)
        {
            ClearMessages();

            try
            {
                _orchestrator.RunOne(id);
                MessageKey = "success_job_executed";
            }
            catch
            {
                ErrorKey = "error_job_not_found";
            }
        }

        public void RunAll()
        {
            ClearMessages();

            try
            {
                _orchestrator.RunAllSequential();
                MessageKey = "success_job_executed";
            }
            catch
            {
                ErrorKey = "error_invalid_input";
            }
        }
    }
}
