using EasySave.Domain;
using EasySave.Application;


namespace EasySave.Application
{
    public class BackupOrchestrator
    {
        private readonly IJobRepository _repo;
        private readonly IBackupEngine _engine;

        public BackupOrchestrator(IJobRepository repo, IBackupEngine engine)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// Execute a job with is id
  

        public void RunOne(int id)
        {
            var jobs = _repo.LoadAll();
            var job = jobs.FirstOrDefault(j => j.Id == id);

            if (job == null)
            {
                throw new InvalidOperationException($"Le job avec l'ID {id} n'existe pas.");
            }

            _engine.Run(job);
        }

        /// Execute multiple jobs
        public void RunMany(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                throw new ArgumentException("La liste des IDs ne peut pas être vide.", nameof(ids));
            }

            var jobs = _repo.LoadAll();

            foreach (var id in ids)
            {
                var job = jobs.FirstOrDefault(j => j.Id == id);

                if (job == null)
                {
                    throw new InvalidOperationException($"Le job avec l'ID {id} n'existe pas.");
                }

                _engine.Run(job);
            }
        }

        /// <summary>
        /// Exécute tous les jobs de sauvegarde disponibles (séquentiel)
        /// </summary>
        public void RunAll()
        {
            var jobs = _repo.LoadAll();

            if (jobs == null || jobs.Count == 0)
            {
                throw new InvalidOperationException("Aucun job de sauvegarde n'est disponible.");
            }

            foreach (var job in jobs)
            {
                _engine.Run(job);
            }
        }
    }
}