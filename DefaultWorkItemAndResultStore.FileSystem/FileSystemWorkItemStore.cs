using BatchProcessor.Model;
using BatchProcessorConfig;

namespace BatchProcessor.FileSystemStore
{
    [BatchComponent("FileSystemWorkItemStore", IsDefault = true)]
    public class FileSystemWorkItemStore : IWorkItemStore
    {
        FileSystemStore dataStore = new FileSystemStore(
            ConfigOptions.WorkItemsStoreName);
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
