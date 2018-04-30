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

using System;
using BatchProcessor.Model;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using BatchProcessor.Client;

namespace Worker.ImageVerificationCheckBase
{
    public class SimpleWorker : IWorker
    {
        public string WorkerType { get { return "SimpleWorker"; } }

        public ResultInfo Process(WorkItemInfo workItem, WorkItemData data)
        {
            // Do the actual work!
            var res = new ResultInfo();
            if (workItem != null)
            {
                Console.WriteLine("Work Item:");
                Console.WriteLine(Dump(workItem));
                res.Key = workItem.Key;
            }

            if (data != null)
            {
                Console.WriteLine("data:");
                Console.WriteLine(Dump(data));
            }
            return res;
        }

        public static string Dump(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }


    }
}
