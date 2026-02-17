using EasySave.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Core.Application
{
    public sealed class JobManager
    {
        private readonly IJobRepository _repo;
        //private const int MaxJobs = 5;

        public JobManager(IJobRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public List<BackupJob> GetAll()
        {
            return _repo.LoadAll();
        }

        public void Add(BackupJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            List<BackupJob> jobs = _repo.LoadAll();
            //if (jobs.Count >= MaxJobs)
            //{
            //    throw new InvalidOperationException("Nombre maximum de travaux atteint.");
            //}

            // Auto-assign ID si non fourni
            if (job.Id <= 0)
            {
                int nextId = jobs.Count == 0 ? 1 : jobs.Max(j => j.Id) + 1;
                job.Id = nextId;
            }

            while (jobs.Any(existingJob => existingJob.Id == job.Id))
            {
                job.Id++;
            }

            jobs.Add(job);
            _repo.SaveAll(jobs);
        }

        public void Remove(int id)
        {
            List<BackupJob> jobs = _repo.LoadAll();
            BackupJob? jobToRemove = jobs.FirstOrDefault(existingJob => existingJob.Id == id);

            if (jobToRemove == null)
            {
                throw new InvalidOperationException("Le travail à supprimer est introuvable.");
            }

            jobs.Remove(jobToRemove);
            _repo.SaveAll(jobs);
        }
    }
}
