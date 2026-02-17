using EasySave.Core.Domain;

using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Core.Application
{
    public sealed class BackupOrchestrator
    {
        private readonly IJobRepository _repo;
        private readonly IBackupEngine _engine;

        public BackupOrchestrator(IJobRepository repo, IBackupEngine engine)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public void RunOne(int id)
        {
            List<BackupJob> jobs = _repo.LoadAll();
            BackupJob? job = jobs.FirstOrDefault(j => j.Id == id);

            if (job == null)
            {
                throw new InvalidOperationException($"Le job avec l'ID {id} n'existe pas.");
            }

            _engine.Run(job);
        }

        public void RunMany(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                throw new ArgumentException("La liste des IDs ne peut pas être vide.", nameof(ids));
            }

            List<BackupJob> jobs = _repo.LoadAll();

            foreach (int id in ids)
            {
                BackupJob? job = jobs.FirstOrDefault(j => j.Id == id);
                if (job == null)
                {
                    throw new InvalidOperationException($"Le job avec l'ID {id} n'existe pas.");
                }
                _engine.Run(job);
            }
        }

        public void RunAll()
        {
            List<BackupJob> jobs = _repo.LoadAll();
            if (jobs == null || jobs.Count == 0)
            {
                throw new InvalidOperationException("Aucun job de sauvegarde n'est disponible.");
            }

            foreach (BackupJob job in jobs)
            {
                _engine.Run(job);
            }
        }

        public void RunAllSequential()
        {
            RunAll();
        }
    }
}
