using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasySave.Domain;

namespace EasySave.Application
{
    public class BackupOrchestrator
    {
        private readonly IJobRepository _repo;
        private readonly IBackupEngine _engine;

        public BackupOrchestrator(IJobRepository repo, IBackupEngine engine)
        {
            _repo = repo;
            _engine = engine;
        }

        // Modifié pour accepter un callback de progression
        public void RunOne(BackupJob job, CancellationToken ct, IProgress<int>? progress = null)
        {
            if (job == null) return;
            _engine.Run(job, ct, progress);
        }

        public void RunAllSequential(List<BackupJob> jobs, CancellationToken ct)
        {
            foreach (var job in jobs)
            {
                if (ct.IsCancellationRequested) break;
                // On pourrait créer un progress spécifique pour chaque job ici
                _engine.Run(job, ct, null);
            }
        }
    }
}