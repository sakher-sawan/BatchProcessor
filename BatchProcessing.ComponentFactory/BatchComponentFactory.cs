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

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using BatchProcessor.Model;
using BatchProcessor.Logging;
using System.IO;
using System.Threading.Tasks;
using BatchProcessor.Config;

namespace BatchProcessor.ComponentFactory
{
    public class BatchComponentFactory
    {
        public static ServiceCollection LoadBatchServicesFromConfiguration()
        {
            var services = new ServiceCollection();

            ConcurrentBag<Assembly> assemblies;
            assemblies = loadAllAssemblies();

            Dictionary<string, List<Type>> batchComponentsCategories = getBarchComponentsCategories(assemblies);

            string workQueueConfig = (BatchProcessor.Config.ConfigurationManager.AppSettings[ConfigOptions.WorkQueue] ?? "").ToLower();
            string resultStoreConfig = (BatchProcessor.Config.ConfigurationManager.AppSettings[ConfigOptions.ResultStore] ?? "").ToLower();
            string workItemStoreConfig = (BatchProcessor.Config.ConfigurationManager.AppSettings[ConfigOptions.WorkItemStore] ?? "").ToLower();

            //TODO: Account for more than one returned.
            // Configuration doesn't need to be the complete name, but just part of it, for example: if compmenent name is "FileSystemResultStore", config can be just: "FileSystem" to locate the right type.
            // Configuration is not case-sensitive.
            Type workQueueType = batchComponentsCategories["WorkQueue"].FirstOrDefault(c => c.GetTypeInfo().GetCustomAttribute<BatchComponentAttribute>().ComponenetName.ToLower().Contains(workQueueConfig));
            Type resultStoreType = batchComponentsCategories["ResultStore"].FirstOrDefault(c => c.GetTypeInfo().GetCustomAttribute<BatchComponentAttribute>().ComponenetName.ToLower().Contains(resultStoreConfig));
            Type workItemStoreType = batchComponentsCategories["WorkItemStore"].FirstOrDefault(c => c.GetTypeInfo().GetCustomAttribute<BatchComponentAttribute>().ComponenetName.ToLower().Contains(workItemStoreConfig));


            if (workQueueType != null)
                services.AddTransient((c) => { return Activator.CreateInstance(workQueueType) as IWorkQueue; });

            if (resultStoreType != null)
                services.AddTransient((c) => { return Activator.CreateInstance(resultStoreType) as IResultStore; });

            if (workItemStoreType != null)
                services.AddTransient((c) => { return Activator.CreateInstance(workItemStoreType) as IWorkItemStore; });

            foreach (var item in batchComponentsCategories["Worker"])
            {
                services.AddTransient((c) => { return Activator.CreateInstance(item) as IWorker; });
            }


            // Register workers.
            var serviceProvider = services.BuildServiceProvider();

            SimpleWorkerCollection workerCollection = new SimpleWorkerCollection();
            var workers = serviceProvider.GetServices<IWorker>();
            foreach (var worker in workers)
            {
                workerCollection.AddWorker(worker);
            }
            services.AddSingleton<IWorkerCollection>(workerCollection);

            return services;
        }

        private static Dictionary<string, List<Type>> getBarchComponentsCategories(ConcurrentBag<Assembly> assemblies)
        {
            var batchComponentsCategories = new Dictionary<string, List<Type>>();

            batchComponentsCategories.Add("WorkQueue", new List<Type>());
            batchComponentsCategories.Add("ResultStore", new List<Type>());
            batchComponentsCategories.Add("WorkItemStore", new List<Type>());
            batchComponentsCategories.Add("Worker", new List<Type>());
            Type[] allBatchComponents = assemblies.SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes().Where(type =>
                    {
                        var res = false;
                        try
                        {
                            res = type.GetTypeInfo().GetCustomAttributes<BatchComponentAttribute>().Count() > 0;
                        }
                        catch (Exception ex)
                        {
                            Logging.Logger.Error(ex);
                        }
                        return res;
                    });
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error(ex);
                }
                return new List<Type>();
            }
            ).ToArray();

            batchComponentsCategories["WorkQueue"]
                .AddRange(allBatchComponents.Where(type => typeof(IWorkQueue).IsAssignableFrom(type)));

            batchComponentsCategories["ResultStore"]
                .AddRange(allBatchComponents.Where(type => typeof(IResultStore).IsAssignableFrom(type)));

            batchComponentsCategories["WorkItemStore"]
                .AddRange(allBatchComponents.Where(type => typeof(IWorkItemStore).IsAssignableFrom(type)));

            batchComponentsCategories["Worker"]
                .AddRange(allBatchComponents.Where(type => typeof(IWorker).IsAssignableFrom(type)));
            return batchComponentsCategories;
        }

        private static ConcurrentBag<Assembly> loadAllAssemblies()
        {
            string codeBase = typeof(BatchComponentFactory).GetTypeInfo().Assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string assemblyPath = Path.GetDirectoryName(path);
            string[] assemblyFiles = Directory.GetFiles(assemblyPath, "*.dll", SearchOption.AllDirectories);
            assemblyFiles = assemblyFiles.Concat(Directory.GetFiles(assemblyPath, "*.exe", SearchOption.AllDirectories)).ToArray();
            var assemblies = new ConcurrentBag<Assembly>();
            //foreach (var c in dllFiles)
            Parallel.ForEach(assemblyFiles, c =>
            {
                Assembly a = null;
                try
                {
                    a = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(c)));
                    //#if NETSTANDARD1_6
                    //                    // Load .NET Core / Standard DLL
                    //                       a = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(c)));
                    //#elif NET47
                    //                    // To load .NET Framework DLLs (full .NET and not .NET Core).
                    //                    a = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(c)));
                    //                    //a = AssemblyLoadContext.Default.LoadFromAssemblyPath(c);
                    //#endif
                }
                catch (Exception ex)
                {

                    Logging.Logger.Error(ex);
                }
                if (a != null)
                {
                    assemblies.Add(a);
                }
            }
            );
            return assemblies;
        }
    }
}
