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

using Amazon.SQS;
using BatchProcessor.Logging;
using BatchProcessor.Model;
using BatchProcessor.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Concurrent;

namespace BatchProcessor.AWS.SQS.WorkQueue
{
    // IMPORTANT NOTE:
    // AWS Access details should be stored in the following path:
    //  C:\users\awsuser\.aws\credentials.
    //    Each profile has the following format:
    //      [{profile_name}]
    //      aws_access_key_id = {accessKey}
    //      aws_secret_access_key = {secretKey}

    [BatchComponent("AWSSQSWorkQueue")]
    public class SQSWorkQueue : IWorkQueue
    {
        #region IWorkQueue Public Members
        public void DeleteWorkItem(WorkItemInfo workItem)
        {
            string queueSuffix = workItem.WorkerType;
            string finalQueueName;
            AmazonSQSClient sqsClient = constructSqsClient(ConfigurationManager.AppSettings[AWSSQSConfigOptions.AWSQueueURL], queueSuffix, out finalQueueName);
            var resTask = sqsClient.DeleteMessageAsync(finalQueueName, workItem.GetParamOrDefault("ReceiptHandle"));
            resTask.Wait();
            if (resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("The item was not correctly deleted, error code=" + resTask.Result.HttpStatusCode, resTask.Exception);
            }
        }

        public WorkItemInfo GetNextWorkItem(string workerType)
        {
            string queueSuffix = workerType;
            string finalQueueName;
            AmazonSQSClient sqsClient = constructSqsClient(ConfigurationManager.AppSettings[AWSSQSConfigOptions.AWSQueueURL], queueSuffix, out finalQueueName);
            var resTask = sqsClient.ReceiveMessageAsync(finalQueueName);
            resTask.Wait();
            if (resTask.Result.Messages.Count == 0)
            {
                return null;
            }
            string jsonMessage = resTask.Result.Messages[0].Body;
            JObject parsedObject = JObject.Parse(jsonMessage);
            var res = parsedObject.ToObject<WorkItemInfo>();
            res.SetParam("MessageId", resTask.Result.Messages[0].MessageId);
            res.SetParam("ReceiptHandle", resTask.Result.Messages[0].ReceiptHandle);
            return res;
        }

        public WorkItemInfo SendWorkItem(WorkItemInfo workItem)
        {
            string queueSuffix = workItem.WorkerType;
            string finalQueueName;
            AmazonSQSClient sqsClient = constructSqsClient(ConfigurationManager.AppSettings[AWSSQSConfigOptions.AWSQueueURL], queueSuffix, out finalQueueName);
            JObject jObj = JObject.FromObject(workItem);
            var resTask = sqsClient.SendMessageAsync(finalQueueName, jObj.ToString());
            resTask.Wait();
            workItem.SetParam("MessageId", resTask.Result.MessageId);
            return workItem;
        }


        public void ReleaseWorkItem(WorkItemInfo workItem)
        {
            string queueSuffix = workItem.WorkerType;
            string finalQueueName;
            AmazonSQSClient sqsClient = constructSqsClient(ConfigurationManager.AppSettings[AWSSQSConfigOptions.AWSQueueURL], queueSuffix, out finalQueueName);
            var resTask = sqsClient.ChangeMessageVisibilityAsync(finalQueueName, workItem.GetParamOrDefault("ReceiptHandle"), 1);
            resTask.Wait();
            if (resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("The item was not correctly deleted, error code=" + resTask.Result.HttpStatusCode, resTask.Exception);
            }
        }

        #endregion
        #region Private Members
        private static ConcurrentDictionary<string, Tuple<AmazonSQSClient, string>> sqsClients = new ConcurrentDictionary<string, Tuple<AmazonSQSClient, string>>();
        private static AmazonSQSClient constructSqsClient(string queueUrl, string queueSuffix, out string queueUrlFinal)
        {
            Tuple<AmazonSQSClient, string> res;
            string dictionaryKey = queueUrl + queueSuffix;
            if (sqsClients.ContainsKey(dictionaryKey))
            {
                res = sqsClients[dictionaryKey];
                queueUrlFinal = res.Item2;
                return res.Item1;
            }
            AmazonSQSConfig sqsConfig = new AmazonSQSConfig();
            if (!string.IsNullOrWhiteSpace(queueSuffix))
            {
                queueSuffix = "-" + queueSuffix;
            }
            queueUrlFinal = queueUrl.Trim('/', ' ') + queueSuffix;
            string queueFullName = queueUrl.Trim('/', ' ').Split('/').Last() + queueSuffix;
            sqsConfig.ServiceURL = ConfigurationManager.AppSettings[AWSSQSConfigOptions.AWSServiceURL];
            var sqsClient = new AmazonSQSClient(sqsConfig);
            var task = sqsClient.CreateQueueAsync(new Amazon.SQS.Model.CreateQueueRequest()
            {
                QueueName = queueFullName,
                Attributes = new System.Collections.Generic.Dictionary<string, string>()
                {
                    { QueueAttributeName.VisibilityTimeout ,  TimeSpan.FromMinutes(5).TotalSeconds.ToString() },
                    { QueueAttributeName.MessageRetentionPeriod, TimeSpan.FromDays(4).TotalSeconds.ToString() },
                    { QueueAttributeName.MaximumMessageSize, (64*1024).ToString() },
                    { QueueAttributeName.DelaySeconds, "0" },
                    { QueueAttributeName.ReceiveMessageWaitTimeSeconds, "20" }
                }
            });
            try
            {
                task.Wait();
                queueUrlFinal = task.Result.QueueUrl;
            }
            catch (Exception ex)
            {
                //TODO: Check AggregateException InnerExceptions.
                if (!(ex is AggregateException) && !(ex is Amazon.SQS.Model.QueueNameExistsException))
                {
                    Logger.Error(ex);
                }
            }
            res = new Tuple<AmazonSQSClient, string>(sqsClient, queueUrlFinal);
            sqsClients.AddOrUpdate(dictionaryKey, res, (a, b) => b);
            return sqsClient;
        }
        #endregion
    }
}
