using System;
using System.Collections.Concurrent;
using System.Threading;

namespace EasySave.Core.Application
{
    public sealed class PriorityTransferCoordinator
    {
        private readonly ConcurrentDictionary<string, int> _priorityRemainingByJob;
        private int _globalPriorityRemaining;
        private readonly ManualResetEventSlim _noPriorityPendingEvent;

        public PriorityTransferCoordinator()
        {
            _priorityRemainingByJob = new ConcurrentDictionary<string, int>();
            _noPriorityPendingEvent = new ManualResetEventSlim(true);
            _globalPriorityRemaining = 0;
        }

        public void RegisterJob(string jobName, int priorityCount)
        {
            if (priorityCount < 0)
            {
                priorityCount = 0;
            }

            _priorityRemainingByJob.AddOrUpdate(
                jobName,
                priorityCount,
                delegate (string key, int oldValue)
                {
                    return priorityCount;
                });

            RecomputeGlobal();
        }

        public void MarkPriorityDone(string jobName)
        {
            _priorityRemainingByJob.AddOrUpdate(
                jobName,
                0,
                delegate (string key, int oldValue)
                {
                    int newValue = oldValue - 1;
                    if (newValue < 0)
                    {
                        newValue = 0;
                    }
                    return newValue;
                });

            RecomputeGlobal();
        }

        public void UnregisterJob(string jobName)
        {
            int removedValue;
            _priorityRemainingByJob.TryRemove(jobName, out removedValue);
            RecomputeGlobal();
        }

        public void WaitIfPrioritiesExist(CancellationToken token)
        {
            _noPriorityPendingEvent.Wait(token);
        }

        private void RecomputeGlobal()
        {
            int sum = 0;

            foreach (KeyValuePair<string, int> pair in _priorityRemainingByJob)
            {
                sum = sum + pair.Value;
            }

            _globalPriorityRemaining = sum;

            if (sum <= 0)
            {
                _noPriorityPendingEvent.Set();
            }
            else
            {
                _noPriorityPendingEvent.Reset();
            }
        }
    }
}