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

using ProtoBuf;
using System;
using System.Collections.Generic;

namespace BatchProcessor.Model
{
    [ProtoContract]
    [Serializable]
    public class ResultInfo: ExtObject
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public byte[] Data { get; set; }

        [ProtoMember(3)]
        public bool Postpone { get; set; }
    }
}