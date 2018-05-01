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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BatchProcessor.Model;

namespace BatchProcessUnitTest
{
    [TestClass]
    public class WorkQueueTests
    {
        [TestMethod]
        public void Send_SQSWorkItem()
        {
            addItem("Test");
        }

        private void addItem(string content)
        {
            var item = new WorkItemInfo();
            item.SetParam("TestProperty", "Test");
            item.WorkerType = "TEST";
            getClient().SendWorkItem(item);
        }

        [TestMethod]
        public void SendAndGet_SQSWorkItem()
        {
            addItem("Test");
            WorkItemInfo item = getNextItem();
            Assert.IsNotNull(item);
            Assert.IsNotNull(item.ExtraParams);
            Assert.AreEqual(item.GetParamOrDefault("TestProperty"), "Test");
        }

        private WorkItemInfo getNextItem()
        {
            var item = getClient().GetNextWorkItem("TEST");
            return item;
        }

        [TestMethod]
        public void SendAndGetAndDelete_SQSWorkItem()
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
            return new BatchProcessor.AWS.SQS.WorkQueue.SQSWorkQueue();
            //return new BatchProcessor.Azure.ServiceBusQueue.WorkQueue.AzureServiceBusQueueWorkQueue();
        }
    }
}
