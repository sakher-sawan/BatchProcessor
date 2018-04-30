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
using BatchProcessor.Config;
using DataStore.AWS.S3;
using ProtoBuf;
using System;
using System.IO;

namespace ResultStore.AWS.S3
{
    [BatchComponent("AWSS3ResultStore")]
    public class S3ResultStore : IResultStore
    {
        private S3DataStore dataStore;
        public S3ResultStore()
        {
            dataStore = new S3DataStore(ConfigOptions.ResultsStoreName);
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
