/**********************************************************************************
 *   Copyright 2017-2018 Mohamed Sakher Sawan                                     *
 *                                                                                *
 *   Licensed under the Apache License, Version 2.0 (the "License");              *
 *   you may not use this file except in compliance with the License.             *
 *   You may obtain a copy of the License at                                      *
 *                                                                                *
 *       http://www.apache.org/licenses/LICENSE-2.0                               *
 *                                                                                *
 *   Unless required by applicable law or agreed to in writing, software          *
 *   distributed under the License is distributed on an "AS IS" BASIS,            *
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.     *
 *   See the License for the specific language governing permissions and          *
 *   limitations under the License.                                               *
***********************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using BatchProcessor.Model;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.IO;
using System.Collections.Concurrent;
using BatchProcessor.Logging;
using System.Runtime.Loader;

namespace BatchProcessor.Library
{
    public class BatchProcessor
    {
        #region Public Members
        public System.Threading.CancellationToken LoopCancellationToken { get; set; }

        public int RetryCount { get; set; } = 3;
        /// <summary>
        /// If the CPU usage average for the last 5 minutes is not within 5% of this value
        /// Worker count will be rebalanced (more workers to be added or removed).
        /// </summary>
        public int CPUUsageTaskBalancingThreshold { get; set; } = 75;

        /// <summary>
        /// If the memory usage average for the last 5 minutes is not within 5% of this value
        /// Worker count will be rebalanced (more workers to be added or removed).
        /// </summary>
        public int FreeMemoryTaskBalancingThreshold { get; set; } = 80;

        public int ConcurrentProcesses { get; set; } =
#if DEBUG // To allow changing process count to lower or higher when debugging.
        Environment.ProcessorCount;
#else
        Environment.ProcessorCount;
#endif




        public BatchProcessor(IWorkQueue workQueue,
            IResultStore resultStore,
            IWorkItemStore workItemStore,
            IWorkerCollection workers)
        {
            Debug.Assert(resultStore != null, "resultStore can't be null");
            Debug.Assert(workItemStore != null, "workItemStore can't be null");
            Debug.Assert(workQueue != null, "queue can't be null");
            this.workers = workers;

            // TODO, CRITICAL: Linux is crashing on memory issue, limiting to 1 concurrent worker.
            bool isWindows, isLinux;
            perfUtil.GetPlatform(out isLinux, out isWindows);
            if (isLinux) this.ConcurrentProcesses = 1;

            ResultStore = resultStore;
            WorkItemStore = workItemStore;
            WorkQueue = workQueue;
            currentWorkItems = new BlockingCollection<WorkItemInfo>(1);
        }

        public event EventHandler<Exception> Error;
        public event EventHandler<string> Update;

        public void StartProcessingLoop()
        {
            lock (startProcessingLockObj)
            {
                if (processingStarted)
                {
                    return;
                }
                Random rnd = new Random(Environment.TickCount);
                Task producerTask = new Task(producerTaskAction, LoopCancellationToken);
                producerTask.Start();

                Task consumerTask = new Task(consumerTaskAction, LoopCancellationToken);
                consumerTask.Start();
                processingStarted = true;
                Task.WaitAll(producerTask, consumerTask);
            }
        }


        public void StartProcessingLoopAsync()
        {
            if (processingStarted)
            {
                return;
            }
            Task t = new Task(() => StartProcessingLoop());
            t.Start();
        }


        public static BatchProcessor Create()
        {
            var serviceCollection = ComponentFactory.BatchComponentFactory.LoadBatchServicesFromConfiguration();
            serviceCollection.AddTransient<BatchProcessor, BatchProcessor>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider.GetService<BatchProcessor>();
        }
        #endregion

        #region Private Members
        private object startProcessingLockObj = new object();
        internal IResultStore ResultStore { get; set; }
        private bool processingStarted = false;
        internal IWorkItemStore WorkItemStore { get; set; }
        internal IWorkQueue WorkQueue { get; set; }
        private IWorkerCollection workers;

        private BlockingCollection<WorkItemInfo> currentWorkItems;
        private PerformanceCountersXPlatform perfUtil = new PerformanceCountersXPlatform();
        private DateTime lastBalanceDate = DateTime.Now;

        private int currentWorkerIndex = 0;
        private int numberOfWorkerTasks = 0;
        private int actualNumberOfRunningWorkers = 0;
        private object workerTaskBalancingLockObject = new object();
        private List<ConsumerToken> workerTasks = new List<ConsumerToken>();


        private void producerTaskAction()
        {
            List<Task> tasks = new List<Task>();
            foreach (var worker in workers)
            {
                Task workerProducerTask = new Task(new Action<object>(workerProducerTaskAction)
                    , worker
                    , LoopCancellationToken);
                workerProducerTask.Start();
                tasks.Add(workerProducerTask);
            }
            Task.WaitAll(tasks.ToArray());
        }

        private void consumerTaskAction()
        {
            for (int i = 0; i < ConcurrentProcesses; i++)
            {
                addOneWorker();
            }
        }
        protected virtual void OnError(Exception ex)
        {
            if (Error != null)
            {
                Error(this, ex);
            }
        }

        protected virtual void OnUpdate(string update)
        {
            if (Update != null)
            {
                Update(this, update);
            }
        }

        private void addOneWorker()
        {
            lock (workerTaskBalancingLockObject)
            {
                int index = Interlocked.Increment(ref numberOfWorkerTasks);
                ConsumerToken token = new ConsumerToken()
                {
                    Index = index,
                    StopProcessing = false
                };
                Task workerTask = new Task(workerAction, token, LoopCancellationToken);
                token.Task = workerTask;
                workerTask.Start();
                workerTasks.Add(token);
            }
        }

        private void removeOneWorker()
        {
            if (numberOfWorkerTasks <= 1) return;
            lock (workerTaskBalancingLockObject)
            {
                int index = Interlocked.Decrement(ref numberOfWorkerTasks);
                var taskToRemove = workerTasks.Last();
                workerTasks.Remove(taskToRemove);
                taskToRemove.StopProcessing = true;
            }
        }

        private void rebalanceWorkers()
        {
            if ((DateTime.Now - lastBalanceDate).TotalSeconds < 60)
            {
                return;
            }
            lastBalanceDate = DateTime.Now;
            double currentMemUsage = perfUtil.GetAverageMemoryUsageFor5Minutes();
            double currentCpuUtil = perfUtil.GetAverageCPUUsageFor5Minutes();
            double cpuUtilDif = CPUUsageTaskBalancingThreshold - currentCpuUtil;
            double memUsageDif = FreeMemoryTaskBalancingThreshold - currentMemUsage;
            if (Math.Abs(cpuUtilDif) > 5 || Math.Abs(memUsageDif) > 5)
            {
                if (memUsageDif < 0)
                {
                    removeOneWorker();
                    OnUpdate("Worker removed, mem usage is " + currentMemUsage);
                }
                else if (cpuUtilDif < 0)
                {
                    removeOneWorker();
                    OnUpdate("Worker removed, CPU Util is " + currentCpuUtil);
                }
                else
                {
                    bool isWindows, isLinux;
                    perfUtil.GetPlatform(out isLinux, out isWindows);
                    if (!isLinux)
                    {
                        addOneWorker();
                        OnUpdate("Worker added, CPU Util is " + currentCpuUtil + "  mem usage is " + currentMemUsage);
                    }
                }
                OnUpdate("----- Worker count " + workerTasks.Count + " actual worker count " + actualNumberOfRunningWorkers);
            }
        }

        private void workerAction(object tokenObject)
        {
            Interlocked.Increment(ref actualNumberOfRunningWorkers);
            ConsumerToken token = tokenObject as ConsumerToken;
            while (!token.StopProcessing)
            {
                try
                {
                    Interlocked.Increment(ref this.currentWorkerIndex);
                    WorkItemInfo workItem;
                    OnUpdate("Waiting for an item from the queue " + currentWorkerIndex);
                    workItem = currentWorkItems.Take(LoopCancellationToken);
                    OnUpdate("Item was found " + currentWorkerIndex);
                    if (!workers.Any(c => c.WorkerType == workItem.WorkerType))
                    {
                        OnUpdate("Worker type " + workItem.WorkerType + " is not available in this node, releasing item back to the queue " + currentWorkerIndex);
                        WorkQueue.ReleaseWorkItem(workItem);
                        OnUpdate("Item released back to the queue " + currentWorkerIndex);
                        return;
                    }
                    var worker = workers.Single(c => c.WorkerType == workItem.WorkerType);
                    OnUpdate("Worker type: " + worker.WorkerType + " " + currentWorkerIndex);
                    WorkItemData workItemData = null;
                    if (workItem.GetParamOrDefault("NoData", "").ToLower() != "true")
                    {
                        OnUpdate("Getting data: " + worker.WorkerType + " " + currentWorkerIndex);
                        workItemData = WorkItemStore.Get(workItem.Key + "-" + workItem.WorkerType);
                        OnUpdate("Data received, size in bytes "
                            + (workItemData.Data != null ? workItemData.Data.Length : 0)
                            + " " + currentWorkerIndex);
                    }
                    OnUpdate("Started processing " + currentWorkerIndex);
                    ResultInfo result = processAndHandleError(currentWorkerIndex, workItem, worker, workItemData);
                    handleResult(currentWorkerIndex, workItem, workItemData, result);
                    rebalanceWorkers();
                }
                catch (Exception ex)
                {
                    OnError(ex);
                } /**/
            }
            Interlocked.Decrement(ref actualNumberOfRunningWorkers);
        }


        private void workerProducerTaskAction(object workerObject)
        {
            while (true)
            {
                try
                {
                    IWorker worker = workerObject as IWorker;
                    WorkItemInfo item = WorkQueue.GetNextWorkItem(worker.WorkerType);
                    if (item != null)
                    {
                        currentWorkItems.Add(item);
                    }
                    else
                    {
                        OnUpdate("Item was not found for worker: " + worker.WorkerType);
                    }
                }
                catch (Exception ex)
                {
                    Error(this, ex);
                }
            }

        }
        private ResultInfo processAndHandleError(int i, WorkItemInfo workItem, IWorker worker, WorkItemData workItemData)
        {
            ResultInfo result = null;
            try
            {
                result = worker.Process(workItem, workItemData);
            }
            catch (Exception ex)
            {
                OnError(ex);
                deleteItemAndData(workItem, workItemData);
                int tried = int.Parse(workItem.GetParamOrDefault("Tried", "0"));
                if (tried < RetryCount)
                {
                    tried++;
                    workItem.SetParam("Tried", tried.ToString());
                    OnUpdate("Processing failed, retrying for the " + tried + " time " + i);
                    WorkQueue.SendWorkItem(workItem);
                }
                else
                {
                    OnUpdate("Processing failed, reached max number of trials " + tried + ", item abandoned " + i);
                }
            }

            return result;
        }

        private void deleteItemAndData(WorkItemInfo workItem, WorkItemData workItemData)
        {
            WorkQueue.DeleteWorkItem(workItem);
            if (workItemData != null)
            {
                WorkItemStore.Delete(workItemData);
            }
        }

        private void handleResult(int i, WorkItemInfo workItem, WorkItemData workItemData, ResultInfo result)
        {
            if (result != null)
            {
                if (result.Postpone)
                {
                    OnUpdate("Processing postponed " + i);
                    WorkQueue.ReleaseWorkItem(workItem);
                }
                else
                {
                    OnUpdate("Finished processing, storing result " + i);
                    deleteItemAndData(workItem, workItemData);
                    result.Key = workItem.Key + "-" + workItem.WorkerType;
                    OnUpdate("Finished processing, storing result " + i);
                    ResultStore.Store(result);
                    OnUpdate("Finished result is stored " + i);
                }
            }
            else
            {
                deleteItemAndData(workItem, workItemData);
            }
        }
        #endregion
    }

    //internal class AssemblyLoader : AssemblyLoadContext
    //{
    //    // Not exactly sure about this
    //    protected override Assembly Load(AssemblyName assemblyName)
    //    {
    //        var deps = DependencyContext.Default;
    //        var res = deps.CompileLibraries.Where(d => d.Name.Contains(assemblyName.Name)).ToList();
    //        var assembly = Assembly.Load(new AssemblyName(res.First().Name));
    //        return assembly;
    //    }
    //}


}
