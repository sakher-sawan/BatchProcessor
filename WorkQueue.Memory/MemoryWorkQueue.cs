using BatchProcessor.Model;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BatchProcessor.MemoryWorkQueue
{
    [BatchComponent("MemoryWorkQueue", IsDefault = true)]
    public class MemoryWorkQueue : IWorkQueue
    {
        private static ConcurrentDictionary<string, ConcurrentQueue<WorkItemInfo>> internalQueues = new ConcurrentDictionary<string, ConcurrentQueue<WorkItemInfo>>();
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<DateTime, WorkItemInfo>>> waitingItems = new ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<DateTime, WorkItemInfo>>>();
        private object internalQueueLockObject = new object();
        private object waitingItemsLockObject = new object();


        public void DeleteWorkItem(WorkItemInfo workItem)
        {
            if (!waitingItems.ContainsKey(workItem.Key))
            {
                return;
            }

            getWaitingItems(workItem.WorkerType)
                .TryRemove(workItem.Key
                , out Tuple<DateTime, WorkItemInfo> temp);

            refreshWaitingItems();
        }

        public WorkItemInfo GetNextWorkItem(string workerType)
        {
            if (getQueue(workerType).TryDequeue(out WorkItemInfo workItem))
                return null;

            getWaitingItems(workerType).TryAdd(
                workItem.Key, new Tuple<DateTime
                    , WorkItemInfo>(DateTime.Now, workItem
                    ));

            refreshWaitingItems();
            return workItem;
        }

        public void ReleaseWorkItem(WorkItemInfo workItem)
        {
            if (!waitingItems.ContainsKey(workItem.Key))
            {
                return;
            }
            Tuple<DateTime, WorkItemInfo> itemToRelease;
            getWaitingItems(workItem.WorkerType).TryRemove(workItem.Key, out itemToRelease);
            if (itemToRelease != null)
            {
                getQueue(workItem.WorkerType).Enqueue(itemToRelease.Item2);
            }
            refreshWaitingItems();
        }

        public WorkItemInfo SendWorkItem(WorkItemInfo workItem)
        {
            getQueue(workItem.WorkerType).Enqueue(workItem);
            return workItem;
        }


        private ConcurrentQueue<WorkItemInfo> getQueue(string workerType)
        {
            lock (internalQueueLockObject)
            {
                if (!internalQueues.ContainsKey(workerType))
                {
                    ConcurrentQueue<WorkItemInfo> newQueue = new ConcurrentQueue<WorkItemInfo>();
                    internalQueues.AddOrUpdate(workerType, newQueue, (a, b) => b);
                }
                return internalQueues[workerType];
            }
        }

        private ConcurrentDictionary<string, Tuple<DateTime, WorkItemInfo>> getWaitingItems(string workerType)
        {
            lock (waitingItemsLockObject)
            {
                if (!waitingItems.ContainsKey(workerType))
                {
                    ConcurrentDictionary<string, Tuple<DateTime, WorkItemInfo>> newWorkingItems = new ConcurrentDictionary<string, Tuple<DateTime, WorkItemInfo>>();
                    waitingItems.AddOrUpdate(workerType, newWorkingItems, (a, b) => b);
                }
                return waitingItems[workerType];
            }
        }

        private void refreshWaitingItems()
        {
            var waitingItemsList = waitingItems.ToArray();
            foreach (var item in waitingItemsList)
            {
                var waitingItemForOneWorkerType = item.Value.ToArray();
                foreach (var workItem in waitingItemForOneWorkerType)
                {
                    if ((DateTime.Now - workItem.Value.Item1).TotalSeconds > 60 * 5)
                    {
                        getWaitingItems(workItem.Value.Item2.WorkerType).TryRemove(workItem.Key, out Tuple<DateTime, WorkItemInfo> wii);
                        getQueue(workItem.Value.Item2.WorkerType).Enqueue(workItem.Value.Item2);
                    }
                }
            }

        }
    }
}
