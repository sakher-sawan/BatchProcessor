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
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ProtoBuf;
using System.Dynamic;

namespace BatchProcessor.Model
{
    [ProtoContract]
    [Serializable]
    [ProtoInclude(100, typeof(WorkItemInfo))]
    [ProtoInclude(200, typeof(ResultInfo))]
    public abstract class ExtObject
    {
        [ProtoMember(1)]
        public List<ParameterInfo> ExtraParams { get; set; } = new List<ParameterInfo>();
        public ExtObject()
        {

        }
        public void SetParam(string name, string value)
        {
            var item = ExtraParams.FirstOrDefault(c => c.Name == name);
            if (item == null)
            {
                item = new ParameterInfo()
                {
                    Name = name,
                    Value = value
                };
                ExtraParams.Add(item);

            }
            else
            {
                item.Value = value;
            }
        }

        public string GetParamOrDefault(string name, string def = null)
        {
            if (ExtraParams.Any(c => c.Name == name))
            {
                return ExtraParams.First(c => c.Name == name).Value;
            }
            return def;
        }

        public dynamic ToDynamic()
        {
            var ex = new ExpandoObject();
            var exCollection = (ICollection<KeyValuePair<string, object>>)ex;

            foreach (var param in ExtraParams)
            {
                exCollection.Add(new KeyValuePair<string, object>(param.Name, param.Value));
            }

            return ex as dynamic;

        }
    }

    [ProtoContract]
    [Serializable]
    public class ParameterInfo
    {
        public ParameterInfo()
        {

        }
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public string Value { get; set; }
    }
}
