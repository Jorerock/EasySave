using System.Threading;

namespace EasySave.Core.Application
{

    // Allows FileSystemBackupEngine to report the progression to the ViewModel 
    public interface IProgressReporter
    {
        // Called before each file to report the current file
        void ReportFile(string sourceFilePath, int filesRemaining, int totalFiles);

        /// Called after each file to update the percentage.
        void ReportProgress(int progressPct, int filesRemaining);

        /// CancellationToken: cancel = Stop requested.
        CancellationToken CancellationToken { get; }

        /// Call WaitOne() after each file to manage the Pause.
        void WaitIfPaused();
    }
}