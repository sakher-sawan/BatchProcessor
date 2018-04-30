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

using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using BatchProcessor.Config;
using System;
using System.IO;
using System.Collections.Generic;

namespace BatchProcessor.AWS.DynamoDB.DataStore
{
    public class DynamoDBDataStore
    {
        private string tableName;
        private readonly int maxItemSizeInBytes = 390 * 1024;//Max size is 4KB, accounting for item names overhead.
        public DynamoDBDataStore(Enum tableNameConfigKey)
        {
            tableName = ConfigurationManager.AppSettings[tableNameConfigKey];
        }
        public void Delete(string key)
        {
            List<byte> res = new List<byte>();
            byte[] currentBlock = getInternal(key);
            int i = 1;
            string currentKey = key;
            while (ResultExists(currentKey))
            {
                deleteInternal(key);
                currentKey = key + "[" + i + "]";
                i++;
            }
        }

        private void deleteInternal(string key)
        {
            Amazon.DynamoDBv2.AmazonDynamoDBClient client = getDynamoDBClient();

            var resTask = client.DeleteItemAsync(tableName, new Dictionary<string, AttributeValue>()
            {
                { "Id", new AttributeValue(){ S = key } }
            });
            resTask.Wait();
            if (resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK
                && resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception("Delete failed, error code: " + resTask.Result.HttpStatusCode, resTask.Exception);
            }
        }

        public bool ResultExists(string key)
        {
            var resTask = getDynamoDBClient().QueryAsync(new QueryRequest()
            {
                AttributesToGet = new List<string>() { "Id" },
                TableName = tableName,
                KeyConditions = new Dictionary<string, Condition>()
                    {
                        {
                            "Id",
                            new Condition()
                                {
                                  ComparisonOperator = "EQ",
                                  AttributeValueList = new List<AttributeValue>()
                                      {
                                       new AttributeValue()
                                           {
                                            S = key
                                           }
                                      }
                                }
                        }
                    }
            });

            resTask.Wait();
            return resTask.Result.Count != 0;
        }

        public byte[] Get(string key)
        {
            List<byte> res = new List<byte>();
            byte[] currentBlock = getInternal(key);
            int i = 1;
            while (currentBlock != null)
            {
                res.AddRange(currentBlock);
                currentBlock = getInternal(key + "[" + i + "]");
                i++;
            }
            return res.ToArray();
        }

        private byte[] getInternal(string key)
        {
            var resTask = getDynamoDBClient().QueryAsync(new QueryRequest()
            {
                AttributesToGet = new List<string>() { "Id", "Item" },
                TableName = tableName,
                KeyConditions = new Dictionary<string, Condition>()
                    {
                        {
                            "Id",
                            new Condition()
                                {
                                  ComparisonOperator = "EQ",
                                  AttributeValueList = new List<AttributeValue>()
                                      {
                                       new AttributeValue()
                                           {
                                            S = key
                                           }
                                      }
                                }
                        }
                    }
            });
            resTask.Wait();
            if (resTask.Result.Count > 0)
            {
                return resTask.Result.Items[0]["Item"].B.ToArray();
            }
            return null;
        }

        public void Store(string key, byte[] data)
        {
            if (data.Length > maxItemSizeInBytes)
            {
                Dictionary<string, byte[]> slices = data.Slice(maxItemSizeInBytes, key);
                foreach (var slice in slices)
                {
                    Store(slice.Key, slice.Value);
                }
                return;
            }
            Amazon.DynamoDBv2.AmazonDynamoDBClient client = getDynamoDBClient();
            var resTask = client.PutItemAsync(new PutItemRequest()
            {
                TableName = tableName,
                Item = new Dictionary<string, AttributeValue>()
                   {
                       {"Id", new AttributeValue(){ S =  key} },
                       {"Item", new AttributeValue(){ B =  new MemoryStream(data)} }
                   }
            });
            resTask.Wait();
            if (resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Store failed, error code: " + resTask.Result.HttpStatusCode, resTask.Exception);
            }
        }

        private static Amazon.DynamoDBv2.AmazonDynamoDBClient getDynamoDBClient()
        {
            return new Amazon.DynamoDBv2.AmazonDynamoDBClient(Amazon.RegionEndpoint.EUWest1);
        }
    }
}
