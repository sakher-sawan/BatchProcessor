using BatchProcessor.Model;
using BatchProcessor.Config;
using DataStore.AWS.DynamoDB;
using ProtoBuf;
using System;
using System.IO;

namespace ResultStore.AWS.DynamoDB
{
    [BatchComponent("AWSDynamoDBResultStore")]
    public class DynamoDBResultStore : IResultStore
    {
        private DynamoDBDataStore dataStore;
        public DynamoDBResultStore()
        {
            dataStore = new DynamoDBDataStore(ConfigOptions.ResultsStoreName);
        }
        public void Delete(ResultInfo result)
        {
            dataStore.Delete(result.Key);
        }

        public ResultInfo Get(string key)
        {
            var data = dataStore.Get(key);
            return Serializer.Deserialize<ResultInfo>(new MemoryStream(data));
        }

        public bool ResultExists(string key)
        {
            return dataStore.ResultExists(key);
        }

        public void Store(ResultInfo result)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                Serializer.Serialize(mem, result);
                var data = mem.ToArray();
                dataStore.Store(result.Key, data);
            }
        }
    }
}
