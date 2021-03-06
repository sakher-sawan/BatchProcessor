﻿/**********************************************************************************
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
using DataStore.AWS.DynamoDB;
using System;
using WorkItemStorage.Interface;

namespace WorkItemStorage.AWS.DynamoDB
{
    [BatchComponent("AWSDynamoDBWorkItemStore")]
    public class DynamoDBWorkItemStore : IWorkItemStore
    {
        private DynamoDBDataStore dataStore;
        public DynamoDBWorkItemStore()
        {
            dataStore = new DynamoDBDataStore("WorkItemsStoreName");
        }

        public void Delete(WorkItemData workItem)
        {
            dataStore.Delete(workItem.Key);
        }

        public WorkItemData Get(string key)
        {
            var data = dataStore.Get(key);
            return new WorkItemData() { Key = key, Data = data };
        }

        public void Store(WorkItemData workItem)
        {
            dataStore.Store(workItem.Key, workItem.Data);
        }
    }
}
