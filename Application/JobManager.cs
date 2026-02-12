using EasySave.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace EasySave.Application
{
    public class JobManager
    {
        private  IJobRepository _repo;
        private  int MaxJobs = 5;

        public JobManager(IJobRepository repo)
        {
            if (repo == null)
            {
                throw new ArgumentNullException(nameof(repo));
            }

            _repo = repo;
        }

        public List<BackupJob> GetAll()
        {
            return _repo.LoadAll();
        }

        public void Add(BackupJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            List<BackupJob> jobs = _repo.LoadAll();

            if (jobs.Count >= MaxJobs)
            {
                throw new InvalidOperationException(
                    "Nombre maximum de travaux  atteint."
                );
            }

            bool idAlreadyExists = jobs.Any(existingJob => existingJob.Id == job.Id);
            if (idAlreadyExists)
            {
                throw new InvalidOperationException(
                    " existe déjà."
                );
            }

            jobs.Add(job);
            _repo.SaveAll(jobs);
        }

        public void Remove(int id)
        {
            List<BackupJob> jobs = _repo.LoadAll();

            BackupJob? jobToRemove =
                jobs.FirstOrDefault(existingJob => existingJob.Id == id);

            if (jobToRemove == null)
            {
                throw new InvalidOperationException(
                    "Le travail à supprimer est introuvable."
                );
            }

            jobs.Remove(jobToRemove);
            _repo.SaveAll(jobs);
        }
    }
}
