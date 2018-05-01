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

using BatchProcessor.Azure.ServiceBusQueue.WorkQueue;
using BatchProcessor.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchProcessUnitTest
{
    [TestClass]
    public class Azure_ServiceBusQueue_Tests
    {
        private static void setDefaultParams()
        {
            BatchProcessor.Config.ConfigurationManager.AppSettings
                               .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureClientId, "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureClientSecret, "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX=");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureServiceBusConnectionString, "Endpoint=sb://XXXXXXXX.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY=");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureServiceBusNamespace, "XXXXXXXX");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureServiceBusQueueNamePrefix, "XXXXXXXX");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureServiceBusQueueResourceGroupName, "XXXXXXXX");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureSubscriptionId, "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy");
            BatchProcessor.Config.ConfigurationManager.AppSettings
                .SetGlobalDefault(AzureServiceBusQueueConfigOptions.AzureTenantId, "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz");
        }

        [TestMethod]
        public void Azure_Send_SQSWorkItem()
        {
            addItem("Test");
        }

        private void addItem(string content)
        {
            var item = new WorkItemInfo();
            item.WorkerType = "TEST1";
            item.SetParam("TestProperty", "Test");
            getClient().SendWorkItem(item);
        }

        [TestMethod]
        public void Azure_SendAndGet_SQSWorkItem()
        {
            addItem("Test");
            WorkItemInfo item = getNextItem();
            Assert.IsNotNull(item);
            Assert.IsNotNull(item.ExtraParams);
            Assert.AreEqual(item.GetParamOrDefault("TestProperty"), "Test");
        }

        private WorkItemInfo getNextItem()
        {
            var item = getClient().GetNextWorkItem("TEST1");
            return item;
        }

        [TestMethod]
        public void Azure_SendAndGetAndDelete_WorkItem()
        {
            addItem("Test");
            WorkItemInfo item = getNextItem();
            deleteItem(item);
        }

        private void deleteItem(WorkItemInfo item)
        {
            getClient().DeleteWorkItem(item);
        }

        private IWorkQueue getClient()
        {
            // Change this to test other implementations if needed.
            //return new BatchProcessor.AWS.SQS.WorkQueue.SQSWorkQueue();
            setDefaultParams();
            return new BatchProcessor.Azure.ServiceBusQueue.WorkQueue.AzureServiceBusQueueWorkQueue();
        }
    }
}
