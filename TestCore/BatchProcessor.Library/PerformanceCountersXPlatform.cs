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
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Concurrent;

namespace BatchProcessor.Library
{
    public class PerformanceCountersXPlatform
    {
        private System.Threading.Timer timer;
        private FixedSizedQueue<double> lastLoadReadings = new FixedSizedQueue<double>(600);
        private FixedSizedQueue<double> lastMemoryReadings = new FixedSizedQueue<double>(600);
        private double fiveMinAverage;
        public PerformanceCountersXPlatform()
        {
            timer = new System.Threading.Timer(performanceTimerTimer, null, 0, 2000);

        }
        private void performanceTimerTimer(object state)
        {
            double currentLoad = GetCurrentCpuUsageXPlatform();
            lastLoadReadings.Enqueue(currentLoad);
            bool isLinux, isWindows;
            GetPlatform(out isLinux, out isWindows);
            if (isWindows)
            {
                if (lastLoadReadings.Count > 0)
                {
                    fiveMinAverage = lastLoadReadings.Average();
                }
            }
            else if (isLinux)
            {
                fiveMinAverage = currentLoad;
            }
            lastMemoryReadings.Enqueue(GetUsedMemoryPercentageXPlatform());
        }

        public double GetAverageCPUUsageFor5Minutes()
        {
            return fiveMinAverage;
        }

        public double GetAverageMemoryUsageFor5Minutes()
        {
            if (lastMemoryReadings.Count == 0)
            {
                lastMemoryReadings.Enqueue(GetUsedMemoryPercentageXPlatform());
            }
            return lastMemoryReadings.Average();
        }

        public double GetCurrentCpuUsageXPlatform()
        {
            bool isLinux, isWindows;
            GetPlatform(out isLinux, out isWindows);
            string processName = isWindows ? "cmd.exe" : "/usr/bin/uptime";
            string args = isWindows ? "/c wmic cpu get loadpercentage" : @"";

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            double percentage = 0;
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();

                if (isWindows)
                {
                    if (double.TryParse(line, out percentage))
                    {
                        break;
                    }
                }
                else if (isLinux)
                {
                    //line = " 22:52:21 up  6:09,  0 users,  load average: 0.52, 0.58, 0.59";
                    string regex = @".*load average: *([0-9.]*) *,";
                    //Console.WriteLine(line);
                    Match m = Regex.Match(line, regex);
                    if (m.Success)
                    {
                        percentage = (double.Parse(m.Groups[1].Value)) * 100;
                        break;
                    }
                }
            }
            return percentage;
        }



        public double GetUsedMemoryPercentageXPlatform()
        {
            bool isLinux, isWindows;
            GetPlatform(out isLinux, out isWindows);
            string processName = isWindows ? "cmd.exe" : "/usr/bin/free";
            string args = isWindows ? "/c wmic ComputerSystem get TotalPhysicalMemory && wmic OS get FreePhysicalMemory" : @"-m";

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            bool numRead = false;
            long freeMem = 0;
            long totalMem = 0;
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();

                if (isWindows)
                {
                    long tmp;
                    if (long.TryParse(line, out tmp))
                    {
                        if (numRead)
                        {
                            freeMem = tmp;
                            break;
                        }
                        else
                        {
                            numRead = true;
                            totalMem = tmp;
                        }
                    }
                }
                else if (isLinux)
                {
                    //line = "Mem:         16282      10913       5369         17         33        184";
                    string regex = @"Mem:\s*([0-9]+)\s+[0-9]+\s+([0-9]+)";
                    //Console.WriteLine(line);
                    Match m = Regex.Match(line, regex);
                    if (m.Success)
                    {
                        totalMem = long.Parse(m.Groups[1].Value);
                        freeMem = long.Parse(m.Groups[2].Value);
                        break;
                    }
                }
            }
            return (1 - (freeMem / (totalMem / 1000.0))) * 100;
        }

        internal void GetPlatform(out bool isLinux, out bool isWindows)
        {
            isLinux = false;
            isWindows = false;

#if NETSTANDARD1_6
            isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#elif NET47
        isWindows = 
        (System.Environment.OSVersion.Platform == System.PlatformID.Win32NT)
        || (System.Environment.OSVersion.Platform == System.PlatformID.Win32S)
        || (System.Environment.OSVersion.Platform == System.PlatformID.Win32Windows)
        || (System.Environment.OSVersion.Platform == System.PlatformID.WinCE);
        isLinux = System.Environment.OSVersion.Platform == System.PlatformID.Unix;
#endif
        }
    }

    /// <summary>
    /// Source: https://stackoverflow.com/questions/5923552/c-sharp-collection-with-automatic-item-removal?lq=1
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly int maxQueueSize;
        private readonly object syncRoot = new object();

        public FixedSizedQueue(int maxQueueSize)
        {
            this.maxQueueSize = maxQueueSize;
        }

        public new void Enqueue(T item)
        {
            lock (syncRoot)
            {
                base.Enqueue(item);
                if (Count > maxQueueSize)
                {
                    T temp;
                    TryDequeue(out temp);
                }
            }
        }
    }

}
