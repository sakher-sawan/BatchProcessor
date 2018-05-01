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

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BatchProcessor.Config
{
    public class ConfigurationManager
    {
        public static ConfigurationManager AppSettings = new ConfigurationManager();
        public static ConfigOptions ConfigOptions = new ConfigOptions();

#if NETSTANDARD1_6
        private static string homePath = (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
#elif NET47
        private static string homePath =  Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
#endif




        private static ConcurrentDictionary<string, string> globalSettings = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> localSettings = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> defaultSettings = new ConcurrentDictionary<string, string>();
        private static readonly string localConfigFilePath = Path.Combine(".batch", "config.json");
        private static readonly string globalConfigFilePath = Path.Combine(homePath, ".batch", "config.json");
        private static DateTime lastLocalConfigFileWriteDate;
        private static DateTime lastGlobalConfigFileWriteDate;
        private static string defaultConfigContent = @"
{
    ""AWSQueueURL"":""https://sqs.eu-west-1.amazonaws.com/919916729808/TestQueue"",
    ""AWSServiceURL"":""http://sqs.eu-west-1.amazonaws.com"",
    ""ResultsStoreName"":""batch-results"",
    ""WorkItemsStoreName"":""workitems-data"",
    ""ResultStore"":""S3"",
    ""WorkItemStore"":""S3"",
    ""WorkQueue"":""SQS"",
    ""LogPath"":""" + Path.Combine(homePath, ".batch", "log").Replace(@"\", @"\\") + @"""
}
";

        static ConfigurationManager()
        {
            var defaultSettingsTemp =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(defaultConfigContent);
            defaultSettings = new ConcurrentDictionary<string, string>(defaultSettingsTemp.ToList());
        }

        private static void loadSettings()
        {
            loadSettings(localConfigFilePath, ref localSettings, ref lastLocalConfigFileWriteDate);
            loadSettings(globalConfigFilePath, ref globalSettings, ref lastGlobalConfigFileWriteDate);
        }

        private static void loadSettings(string configFilePath, ref ConcurrentDictionary<string, string> settings, ref DateTime lastWriteTime)
        {
            if (!File.Exists(configFilePath))
            {
                return;
            }
            var currentLastWriteTime = File.GetLastWriteTime(configFilePath);
            if (currentLastWriteTime != lastWriteTime)
            {
                lastWriteTime = currentLastWriteTime;
                var settingsTemp = JsonConvert.DeserializeObject<Dictionary<string, string>>
                    (
                        File.ReadAllText(configFilePath)
                    );
                settings = new ConcurrentDictionary<string, string>(settingsTemp.ToList());
            }
        }

        public string this[Enum enumIndex]
        {
            get
            {
                string index = enumIndex.ToString();
                loadSettings();
                if (localSettings.ContainsKey(index)) return localSettings[index];
                else if (globalSettings.ContainsKey(index)) return globalSettings[index];
                else if (defaultSettings.ContainsKey(index)) return defaultSettings[index];
                else return null;
            }
        }

        public void SetGlobalDefault(Enum index, string value)
        {
            defaultSettings.AddOrUpdate(index.ToString(), value, (i, v) => v);
        }
    }
}
