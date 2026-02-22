using EasyLog.Interfaces;
using EasySave.Core.Application;
using EasySave.Core.Domain;

namespace EasySave.Core.Infrastructure
{
    /// <summary>
    /// Fabrique des instances de FileSystemBackupEngine liées à un IProgressReporter.
    /// Une instance de moteur est créée par job pour permettre l'exécution parallèle.
    /// </summary>
    public class FileSystemBackupEngineFactory : IBackupEngineFactory
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;
        private readonly AppSettings _settings;
        private readonly IBusinessSoftwareDetector _detector;

        public FileSystemBackupEngineFactory(
            ILogWriter logWriter,
            IStateWriter stateWriter,
            AppSettings settings,
            IBusinessSoftwareDetector detector)
        {
            _logWriter = logWriter;
            _stateWriter = stateWriter;
            _settings = settings;
            _detector = detector;
        }

        /// <summary>
        /// Crée un nouveau moteur. Le reporter est passé au moment de Run(),
        /// donc la factory se contente de fournir une instance fraîche.
        /// </summary>
        public IBackupEngineWithProgress Create(IProgressReporter reporter)
        {
            // FileSystemBackupEngine implémente IBackupEngineWithProgress
            return new FileSystemBackupEngine(_logWriter, _stateWriter, _settings, _detector);
        }
    }
}