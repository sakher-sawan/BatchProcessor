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

using BatchProcessor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace BatchProcessor.Client
{
    public class BatchProcessorClient
    {
        internal IResultStore ResultStore { get; set; }
        internal IWorkItemStore WorkItemStore { get; set; }
        internal IWorkQueue Queue { get; set; }

        public BatchProcessorClient(IResultStore resultStore
            , IWorkItemStore workItemStore
            , IWorkQueue queue)
        {
            Debug.Assert(resultStore != null, "resultStore can't be null");
            Debug.Assert(workItemStore != null, "workItemStore can't be null");
            Debug.Assert(queue != null, "queue can't be null");

            ResultStore = resultStore;
            WorkItemStore = workItemStore;
            Queue = queue;
        }

        public void ScheduleWork(string workItemKey, string workerType, byte[] workerData, List<KeyValuePair<string, string>> extraParams)
        {
            List<ParameterInfo> extrParams = extraParams.Select(c => new ParameterInfo() { Name = c.Key, Value = c.Value }).ToList();
            if (workerData != null)
            {
                WorkItemStore.Store(new WorkItemData()
                {
                    Key = workItemKey + "-" + workerType,
                    Data = workerData
                });
            }
            else
            {
                extrParams.Add(new ParameterInfo() { Name = "NoData", Value = "true" });
            }

#if DEBUG
            //for (int i = 0; i < 10; i++)
            //{
#endif
            Queue.SendWorkItem(new BatchProcessor.Model.WorkItemInfo()
            {
                WorkerType = workerType,
                Key = workItemKey,
                ExtraParams = extrParams
            });
#if DEBUG
            //}
#endif
        }

        public bool FinishedProcessing(string key)
        {
            return ResultStore.ResultExists(key);
        }

        public ResultInfo GetResult(string key)
        {
            return ResultStore.Get(key);
        }

        public void StoreResult(string key, byte[] value, List<KeyValuePair<string, string>> extraParams = null)
        {
            List<ParameterInfo> extraParamsToAdd = extraParams == null
                ? null
                : extraParams.Select(c => new ParameterInfo()
                {
                    Name = c.Key,
                    Value = c.Value
                }).ToList();
            ResultStore.Store(new ResultInfo() { Key = key, Data = value, ExtraParams = extraParamsToAdd });
        }

        public void RegisterOnWorkFinished(string workItemKey, Action<string> action)
        {

        }

        /// <summary>
        /// Loads suitable components from the configuration.
        /// </summary>
        /// <returns></returns>
        public static BatchProcessorClient CreateClient()
        {
            var serviceCollection = BatchProcessor.ComponentFactory.BatchComponentFactory.LoadBatchServicesFromConfiguration();
            serviceCollection.AddTransient<BatchProcessorClient, BatchProcessorClient>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider.GetService<BatchProcessorClient>();
        }

    }
}
