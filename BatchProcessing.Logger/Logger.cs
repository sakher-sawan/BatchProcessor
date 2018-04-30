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

using BatchProcessor.Config;
using Serilog;
using Serilog.Sinks.File;
using Serilog.Sinks.RollingFile;
using System;
using System.IO;
using System.Reflection;


namespace BatchProcessor.Logging
{
    public class Logger
    {
        private static Serilog.Core.Logger log;
        static Logger()
        {
            string logBasePath = BatchProcessor.Config.ConfigurationManager.AppSettings[ConfigOptions.LogPath] ?? "";
            log = new LoggerConfiguration()
                .WriteTo.RollingFile(Path.Combine(logBasePath, "log-{Date}.txt"))
                .CreateLogger();
            log.Information("Logger initialized");

        }
        public static void Error(Exception ex, string message = "")
        {
            log.Error(ex, message);
        }

        public static void Info(string str)
        {
            log.Error(str);
        }
    }
}
