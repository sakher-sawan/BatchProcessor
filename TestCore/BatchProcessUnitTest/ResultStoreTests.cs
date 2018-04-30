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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace BatchProcessUnitTest
{
    [TestClass]
    public class ResultStoreTests
    {
        [TestMethod]
        public void Store_5MB_Result()
        {
            storeItem();
        }

        private Guid storeItem()
        {
            // 5 MB of data
            byte[] data = new byte[5 * 1024 * 1024];
            data[0] = 1;
            data[data.Length - 1] = 1;
            Guid id = Guid.NewGuid();
            var result = new ResultInfo()
            {
                Data = data,
                Key = id.ToString()
            };
            result.SetParam("TestParam", "TestValue");
            getResultStore().Store(result);
            return id;
        }

        [TestMethod]
        public void StoreAndGet_Result()
        {
            var id = storeItem();
            var item = getItem(id.ToString());
            Assert.AreEqual(item.Data.Length, 5 * 1024 * 1024);
            Assert.AreEqual(item.Data[0], 1);
            Assert.AreEqual(item.Data[item.Data.Length - 1], 1);
            Assert.AreEqual(item.GetParamOrDefault("TestParam"), "TestValue");
        }

        [TestMethod]
        public void StoreGetAndDelete_Result()
        {
            var id = storeItem();
            var item = getItem(id.ToString());
            deleteItem(item.Key);
        }

        private ResultInfo getItem(string key)
        {
            return getResultStore().Get(key);
        }

        private void deleteItem(string key)
        {
            getResultStore().Delete(new ResultInfo() { Key = key });
        }

        private IResultStore getResultStore()
        {
            return new ResultStore.AWS.S3.S3ResultStore();
        }
    }
}
