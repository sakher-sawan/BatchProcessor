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

using BatchProcessor.ComponentFactory;
using BatchProcessor.Config;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BatchProcessor.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var batchProcessor = BatchProcessor.Library.BatchProcessor.Create();

            batchProcessor.ConcurrentProcesses = (int)(Environment.ProcessorCount * 2.5);

            batchProcessor.Error += BatchProcessor_Error;
            batchProcessor.Update += BatchProcessor_Update;

            batchProcessor.StartProcessingLoop();
        }

        private static void BatchProcessor_Update(object sender, string e)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + " - UPDATE: " + e);
        }

        private static void BatchProcessor_Error(object sender, Exception e)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + " - ERROR: " + e);
        }
    }
}