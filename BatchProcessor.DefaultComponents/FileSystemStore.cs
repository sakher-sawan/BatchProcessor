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

using BatchProcessor.Logging;
using BatchProcessor.Config;
using System;
using System.IO;

namespace BatchProcessor.DefaultComponents
{
    internal class FileSystemStore
    {
        private string storePath;
        public FileSystemStore(Enum storeNameKey)
        {
            storePath = ConfigurationManager.AppSettings[storeNameKey];
        }
        public void Delete(string key)
        {
            try
            {
                File.Delete(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public bool ResultExists(string key)
        {
            try
            {
                return File.Exists(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public byte[] Get(string key)
        {
            try
            {
                return File.ReadAllBytes(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public void Store(string key, byte[] data)
        {
            try
            {
                File.WriteAllBytes(Path.Combine(storePath, key), data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }
    }
}
