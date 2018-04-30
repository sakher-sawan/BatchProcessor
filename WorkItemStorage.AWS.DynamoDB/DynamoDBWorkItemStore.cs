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
