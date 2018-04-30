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


namespace System
{
    public static class DataSlicer
    {
        public static Dictionary<string, byte[]> Slice(this byte[] data, int maxSize, string key)
        {
            Dictionary<string, byte[]> slices = new Dictionary<string, byte[]>();
            if (data == null)
            {
                return slices;
            }
            slices.Add(key, data.Take(maxSize).ToArray());
            for (int i = 1; i < (data.Length / maxSize) + 1; i++)
            {
                var currentArray = data.Skip(i * (int)maxSize).Take(maxSize).ToArray();
                if (currentArray.Length > 0)
                {
                    slices.Add(key + "[" + i + "]", currentArray);
                }
            }
            return slices;
        }
    }
}
