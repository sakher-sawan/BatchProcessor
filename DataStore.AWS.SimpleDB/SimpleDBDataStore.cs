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

using Amazon.SimpleDB;
using BatchProcessor.Config;
using System;
using System.IO;

namespace BatchProcessor.AWS.SimpleDB.DataStore
{
    public class SimpleDBDataStore
    {
        private string bucketName;
        public SimpleDBDataStore(Enum bucketNameConfigKey)
        {
            bucketName = ConfigurationManager.AppSettings[bucketNameConfigKey];
        }
        public void Delete(string key)
        {
            throw new NotImplementedException();
        }

        public bool ResultExists(string key)
        {
            throw new NotImplementedException();
        }

        public byte[] Get(string key)
        {
            throw new NotImplementedException();
        }

        public void Store(string key, byte[] data)
        {
            throw new NotImplementedException();
        }

        private static Amazon.SimpleDB.AmazonSimpleDBClient getSimpleDBClient()
        {
            return new Amazon.SimpleDB.AmazonSimpleDBClient(Amazon.RegionEndpoint.EUWest1);
        }
    }
}
