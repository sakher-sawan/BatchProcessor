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

using BatchProcessor.Logging;
using BatchProcessor.Model;
using BatchProcessor.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Management.ServiceBus;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System.Collections.Concurrent;
using Microsoft.Azure.ServiceBus.Core;

namespace BatchProcessor.Azure.ServiceBusQueue.WorkQueue
{
    [BatchComponent("AzureServiceBusQueueWorkQueue")]
    public class AzureServiceBusQueueWorkQueue : IWorkQueue
    {
        #region IWorkQueue Public Members
        public void DeleteWorkItem(WorkItemInfo workItem)
        {
            var rec = constructAzureServiceBusReceiver(workItem.WorkerType);
            var resTask = rec.CompleteAsync(workItem.GetParamOrDefault("LockToken"));
            resTask.Wait();
        }

        public WorkItemInfo GetNextWorkItem(string workerType)
        {
            var rec = constructAzureServiceBusReceiver(workerType);
            var resTask = rec.ReceiveAsync();
            resTask.Wait();
            if (resTask.Result == null || resTask.Result.Body == null)
            {
                return null;
            }
            string jsonMessage = System.Text.Encoding.UTF8.GetString(resTask.Result.Body);
            JObject parsedObject = JObject.Parse(jsonMessage);
            var res = parsedObject.ToObject<WorkItemInfo>();
            res.SetParam("MessageId", resTask.Result.MessageId);
            res.SetParam("LockToken", resTask.Result.SystemProperties.LockToken);
            //var resTask2 = rec.RenewLockAsync(resTask.Result);
            //resTask2.Wait();
            return res;
        }

        public WorkItemInfo SendWorkItem(WorkItemInfo workItem)
        {
            var queueClient = constructAzureServiceBusQueue(workItem.WorkerType);
            string jObj = JObject.FromObject(workItem).ToString();
            string messageId = Guid.NewGuid().ToString();
            var resTask = queueClient.SendAsync(new Message(System.Text.Encoding.UTF8.GetBytes(jObj)) { MessageId = messageId });
            resTask.ConfigureAwait(false);
            resTask.Wait();
            workItem.SetParam("MessageId", messageId);
            return workItem;
        }


        public void ReleaseWorkItem(WorkItemInfo workItem)
        {
            var queueClient = constructAzureServiceBusQueue(workItem.WorkerType);
            var resTask = queueClient.AbandonAsync(workItem.GetParamOrDefault("LockToken"));
            resTask.Wait();
        }

        #endregion
        #region Private Members
        private static ConcurrentDictionary<string, Tuple<IQueueClient, IMessageReceiver>> serviceBusQueues
            = new ConcurrentDictionary<string, Tuple<IQueueClient, IMessageReceiver>>();
        private IQueueClient constructAzureServiceBusQueue(string workerType)
        {
            return createQueueClients(workerType).Item1;
        }

        private IMessageReceiver constructAzureServiceBusReceiver(string workerType)
        {
            return createQueueClients(workerType).Item2;
        }

        private Tuple<IQueueClient, IMessageReceiver> createQueueClients(string workerType)
        {
            string azureServiceBusQueueNamePrefix = Config.ConfigurationManager
                            .AppSettings[AzureServiceBusQueueConfigOptions.AzureServiceBusQueueNamePrefix];
            string azureServiceBusNamespace = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureServiceBusNamespace];
            string azureServiceBusConnectionString = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureServiceBusConnectionString];
            string azureServiceBusQueueResourceGroupName = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureServiceBusQueueResourceGroupName];
            string azureTenantId = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureTenantId];
            string azureSubscriptionId = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureSubscriptionId];
            string azureClientId = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureClientId];
            string azureClientSecret = Config.ConfigurationManager
                .AppSettings[AzureServiceBusQueueConfigOptions.AzureClientSecret];
            string queueName = azureServiceBusQueueNamePrefix + "-" + workerType;
            Tuple<IQueueClient, IMessageReceiver> res;
            if (!serviceBusQueues.ContainsKey(workerType))
            {
                var context = new AuthenticationContext("https://login.microsoftonline.com/" + azureTenantId);

                var resultTask = context.AcquireTokenAsync(
                    "https://management.core.windows.net/",
                    new ClientCredential(azureClientId, azureClientSecret)
                );
                resultTask.Wait();
                var result = resultTask.Result;

                var creds = new TokenCredentials(result.AccessToken, "Bearer");

                IServiceBusManagementClient managementClient = new ServiceBusManagementClient(creds, new System.Net.Http.DelegatingHandler[0])
                {
                    SubscriptionId = azureSubscriptionId
                };
                var task = managementClient.Queues.CreateOrUpdateAsync(azureServiceBusQueueResourceGroupName,
                    azureServiceBusNamespace,
                    queueName,
                    new Microsoft.Azure.Management.ServiceBus.Models.SBQueue()
                    {
                        DefaultMessageTimeToLive = TimeSpan.FromHours(1),
                        LockDuration = TimeSpan.FromMinutes(5),
                        DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10),
                        MaxDeliveryCount = 10,
                    });
                task.Wait();
                var queueDetails = task.Result;
                //Queue Client
                IQueueClient queueClient = new QueueClient(azureServiceBusConnectionString
                    , queueName, ReceiveMode.PeekLock);
                IMessageReceiver receiver = new MessageReceiver(azureServiceBusConnectionString
                    , queueName, ReceiveMode.PeekLock);
                res = new Tuple<IQueueClient, IMessageReceiver>(queueClient, receiver);
            }
            else
            {
                res = serviceBusQueues[workerType];
            }

            serviceBusQueues.AddOrUpdate(workerType, res, (a, b) => b);
            return res;
        }
        #endregion
    }
}
