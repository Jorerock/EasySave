using EasyLog.Interfaces;
using EasySave.Core.Application;
using EasySave.Core.Domain;
using System;

namespace EasySave.Core.Infrastructure
{
    public class FileSystemBackupEngineFactory : IBackupEngineFactory
    {
        private readonly ILogWriter _logWriter;
        private readonly IStateWriter _stateWriter;
        private readonly AppSettings _settings;
        private readonly IBusinessSoftwareDetector _detector;
        private readonly PriorityTransferCoordinator _priorityCoordinator;

        public FileSystemBackupEngineFactory(
            ILogWriter logWriter,
            IStateWriter stateWriter,
            AppSettings settings,
            IBusinessSoftwareDetector detector,
            PriorityTransferCoordinator priorityCoordinator)
        {
            _logWriter = logWriter ?? throw new ArgumentNullException(nameof(logWriter));
            _stateWriter = stateWriter ?? throw new ArgumentNullException(nameof(stateWriter));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _detector = detector ?? throw new ArgumentNullException(nameof(detector));
            _priorityCoordinator = priorityCoordinator ?? throw new ArgumentNullException(nameof(priorityCoordinator));
        }

        public IBackupEngineWithProgress Create(IProgressReporter reporter)
        {
            return new FileSystemBackupEngine(_logWriter, _stateWriter, _settings, _detector, _priorityCoordinator);
        }
    }
}